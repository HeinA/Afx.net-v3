using Afx.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public abstract class MsSqlObjectRepository<T> : ObjectRepository<T>
    where T : class, IAfxObject
  {
    protected static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public abstract string TableName { get; }
    public abstract string Columns { get; }
    public abstract void FillObject(T target, DataRow dr);

    public virtual IEnumerable<string> GetJoins()
    {
      yield break;
    }

    protected string JoinOn(string tableName)
    {
      return string.Format("{1} ON {1}.[id]={0}.[id]", TableName, tableName);
    }

    protected override LoadContext GetInstance(Guid id)
    {
      string sql = string.Format("SELECT [OT].[ix], [RT].[FullName] FROM [Afx].[RegisteredType] [RT] INNER JOIN {0} [OT] ON [OT].[RegisteredType]=[RT].[id] WHERE [OT].[id]=@id", typeof(T).AfxDbName());
      log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", id);
        DataSet ds = ExecuteDataSet(cmd);
        if (ds.Tables[0].Rows.Count == 0) throw new InvalidOperationException("Type not registered");

        var context = new LoadContext();
        context.LoadTargets.Add(new ObjectTarget(id, (int)ds.Tables[0].Rows[0]["ix"], (string)ds.Tables[0].Rows[0]["FullName"]));
        return context;
      }
    }

    protected override LoadContext GetInstances(Guid owner)
    {
      Type ownerType = null;
      Type afxType = typeof(T).GetGenericSubClass(typeof(AssociativeObject<,>));
      if (afxType == null) afxType = typeof(T).GetGenericSubClass(typeof(AfxObject<>));
      if (afxType != null) ownerType = afxType.GetGenericArguments()[0];

      string sql = null;
      if (ownerType == null) sql = string.Format("SELECT [OT].[id], [OT].[ix], [RT].[FullName] FROM [Afx].[RegisteredType] [RT] INNER JOIN {0} [OT] ON [OT].[RegisteredType]=[RT].[id]", typeof(T).AfxDbName());
      else
      {
        if (owner == Guid.Empty) sql = string.Format("SELECT [OT].[id], [OT].[ix], [RT].[FullName] FROM [Afx].[RegisteredType] [RT] INNER JOIN {0} [OT] ON [OT].[RegisteredType]=[RT].[id] WHERE [OT].[Owner] IS NULL", typeof(T).AfxDbName());
        else sql = string.Format("SELECT [OT].[id], [OT].[ix], [RT].[FullName] FROM [Afx].[RegisteredType] [RT] INNER JOIN {0} [OT] ON [OT].[RegisteredType]=[RT].[id] WHERE [OT].[Owner]=@id", typeof(T).AfxDbName());
      }
      log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", owner);
        var context = new LoadContext(owner);
        foreach (DataRow dr in ExecuteDataSet(cmd).Tables[0].Rows)
        {
          string typeName = (string)dr["FullName"];
          Guid id = (Guid)dr["id"];
          int ix = (int)dr["ix"];
          if (string.IsNullOrWhiteSpace(typeName)) throw new InvalidOperationException("Type not registered");
          context.LoadTargets.Add(new ObjectTarget(id, ix, typeName));
        }
        return context;
      }
    }

    protected override bool IsNew(Guid id)
    {
      string sql = string.Format("SELECT CAST(COUNT(1) as bit) FROM {0} WHERE id=@id", typeof(T).AfxDbName());
      log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", id);
        bool isNew = !(bool)cmd.ExecuteScalar();
        return isNew;
      }
    }

    protected override T LoadObjectCore(LoadContext context)
    {
      T target = (T)Activator.CreateInstance(context.LoadTargets[0].AssemblyType, context.LoadTargets[0].Id);
      string joins = string.Join(" INNER JOIN ", GetJoins());
      bool join = !string.IsNullOrWhiteSpace(joins);
      string sql = string.Format("SELECT {0} FROM {1}{2}{3} WHERE {1}.[ix]=@ix", Columns, TableName, join ? " INNER JOIN " : string.Empty, join ? joins : string.Empty);
      log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@ix", context.LoadTargets[0].Ix);
        DataSet ds = ExecuteDataSet(cmd);
        if (ds.Tables[0].Rows.Count != 1) throw new InvalidOperationException(Afx.Data.Properties.Resources.LoadOneRow);
        DataRow dr = ds.Tables[0].Rows[0];
        FillObject(target, dr);
      }
      return target;
    }

    protected override T[] LoadObjectsCore(LoadContext context)
    {
      List<T> objects = new List<T>();
      if (context.LoadTargets.Count == 0) return objects.ToArray();

      string selection = string.Join(" OR ", context.LoadTargets.Select(t => string.Format("{1}.[id]='{0}'", t.Id, TableName)));
      string joins = string.Join(" INNER JOIN ", GetJoins());
      bool join = !string.IsNullOrWhiteSpace(joins);
      string sql = null;
      if (context.Owner != Guid.Empty) sql = string.Format("SELECT {0} FROM {1}{2}{3} WHERE {1}.[Owner]='{5}' AND ({4})", Columns, TableName, join ? " INNER JOIN " : string.Empty, join ? joins : string.Empty, selection, context.Owner);
      else sql = string.Format("SELECT {0} FROM {1}{2}{3} WHERE {4}", Columns, TableName, join ? " INNER JOIN " : string.Empty, join ? joins : string.Empty, selection);
      log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        foreach (DataRow dr in ExecuteDataSet(cmd).Tables[0].Rows)
        {
          T target = (T)Activator.CreateInstance(context.LoadTargets[0].AssemblyType, dr["id"]);
          FillObject(target, dr);
          objects.Add(target);
        }
      }

      return objects.ToArray(); ;
    }

    Type mOwnerType;
    bool mOwnerTypeChecked = false;
    Type OwnerType
    {
      get
      {
        if (mOwnerType != null) return mOwnerType;
        if (mOwnerTypeChecked) return null;
        Type afxType = typeof(T).GetGenericSubClass(typeof(AssociativeObject<,>));
        if (afxType == null) afxType = typeof(T).GetGenericSubClass(typeof(AfxObject<>));
        if (afxType != null) mOwnerType = afxType.GetGenericArguments()[0];
        mOwnerTypeChecked = true;
        return mOwnerType;
      }
    }
    protected override void DeleteObjectsCore(DeleteContext context)
    {
      string selection = string.Join(" AND ", context.ActiveTargets.Select(t => string.Format("[id]<>'{0}'", t)));

      string sql = null;
      if (OwnerType == null) sql = string.Format("SELECT COUNT(1) FROM {0} {1}", TableName, string.IsNullOrWhiteSpace(selection) ? null : string.Format(" WHERE {0}", selection));
      else
      {
        if (context.Owner == Guid.Empty) sql = string.Format("SELECT COUNT(1) FROM {0} WHERE [Owner] IS NULL{2}", TableName, context.Owner, string.IsNullOrWhiteSpace(selection) ? null : string.Format(" AND {0}", selection));
        else sql = string.Format("SELECT COUNT(1) FROM {0} WHERE [Owner]='{1}'{2}", TableName, context.Owner, string.IsNullOrWhiteSpace(selection) ? null : string.Format(" AND {0}", selection), context.ActiveTargets.Count);
      }
      log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        int i = (int)cmd.ExecuteScalar();
        if (i > 0)
        {
          if (OwnerType == null) sql = string.Format("DELETE FROM {0} {1}", TableName, string.IsNullOrWhiteSpace(selection) ? null : string.Format(" WHERE {0}", selection));
          else
          {
            if (context.Owner == Guid.Empty) sql = string.Format("DELETE FROM {0} WHERE [Owner] IS NULL{2}", TableName, context.Owner, string.IsNullOrWhiteSpace(selection) ? null : string.Format(" AND {0}", selection));
            else sql = string.Format("DELETE FROM {0} WHERE [Owner]='{1}'{2}", TableName, context.Owner, string.IsNullOrWhiteSpace(selection) ? null : string.Format(" AND {0}", selection), context.ActiveTargets.Count);
          }
          log.Debug(sql);

          using (SqlCommand cmd1 = GetCommand(sql))
          {
            cmd.ExecuteNonQuery();
          }
        }
      }
    }

    protected SqlCommand GetCommand()
    {
      return (SqlCommand)ConnectionScope.CurrentScope.GetCommand();
    }

    protected SqlCommand GetCommand(string text)
    {
      return (SqlCommand)ConnectionScope.CurrentScope.GetCommand(text);
    }

    protected MsSqlObjectRepository<T1> GetSqlRepository<T1>()
      where T1 : class, IAfxObject
    {
      return (MsSqlObjectRepository<T1>)ObjectRepository.GetRepository<T1>();
    }

    protected string GetColumns<T1>()
      where T1 : class, IAfxObject
    {
      return GetSqlRepository<T1>().Columns;
    }

    protected string GetTableName<T1>()
      where T1 : class, IAfxObject
    {
      return GetSqlRepository<T1>().TableName;
    }

    protected IEnumerable<string> GetJoins<T1>()
      where T1 : class, IAfxObject
    {
      return GetSqlRepository<T1>().GetJoins();
    }

    protected void FillObject<T1>(T1 target, DataRow dr)
      where T1 : class, IAfxObject
    {
      GetSqlRepository<T1>().FillObject(target, dr);
    }
  }
}
