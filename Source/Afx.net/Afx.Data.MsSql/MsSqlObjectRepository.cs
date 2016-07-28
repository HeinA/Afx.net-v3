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
    protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public abstract void FillObject(T target, LoadContext context, DataRow dr);
    public abstract string Columns { get; }
    public abstract string TableJoin { get; }

    #region IsNew()

    protected override bool IsNew(Guid id)
    {
      string sql = string.Format("SELECT COUNT(1) FROM {0} WHERE id=@id", typeof(T).AfxDbName());
      Log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", id);
        int i = (int)cmd.ExecuteScalar();
        bool isNew = i == 0;
        return isNew;
      }
    }

    #endregion

    #region LoadObjectCore()

    protected override T LoadObjectCore(LoadContext context)
    {
      string sql = string.Format("SELECT {0} FROM {1} WHERE {2}.[id]=@id", Columns, TableJoin, typeof(T).AfxDbName());
      Log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", context.Target);
        DataSet ds = ExecuteDataSet(cmd);
        if (ds.Tables[0].Rows.Count != 1) throw new InvalidOperationException(Afx.Data.Properties.Resources.LoadOneRow);
        DataRow dr = ds.Tables[0].Rows[0];
        T target = (T)Activator.CreateInstance(Type.GetType((string)dr["AssemblyFullName"]));
        target.Id = context.Target;
        FillObject(target, context, dr);
        if (target.GetType() != typeof(T)) RepositoryInterfaceFor(target.GetType()).LoadObjectCore(target, context);
        target.IsDirty = false;
        return target;
      }
    }

    protected override void LoadObjectCore(T target, LoadContext context)
    {
      string sql = string.Format("SELECT {0} FROM {1} WHERE {2}.[id]=@id", Columns, TableJoin, typeof(T).AfxDbName());
      Log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", target.Id);
        DataSet ds = ExecuteDataSet(cmd);
        if (ds.Tables[0].Rows.Count != 1) throw new InvalidOperationException(Afx.Data.Properties.Resources.LoadOneRow);
        DataRow dr = ds.Tables[0].Rows[0];
        FillObject(target, context, dr);
      }
    }

    #endregion

    #region LoadObjectsCore()

    protected override T[] LoadObjectsCore(LoadContext context)
    {
      List<T> objects = new List<T>();
      bool useListDetection = false;

      string sql = null;
      if (OwnerType == null) sql = string.Format("SELECT {0} FROM {1}", Columns, TableJoin);
      else
      {
        if (OwnerType != typeof(T))
        {
          if (context.Target == Guid.Empty) sql = string.Format("SELECT {0} FROM {1} WHERE {2}.[Owner] IS NULL", Columns, TableJoin, typeof(T).AfxDbName());
          else sql = string.Format("SELECT {0} FROM {1} WHERE {2}.[Owner]=@id", Columns, TableJoin, typeof(T).AfxDbName());
        }
        else
        {
          useListDetection = true;
          if (context.Target == Guid.Empty) sql = string.Format("WITH Hierarchy AS (SELECT {0}, 0 AS [Level] FROM {1} WHERE {2}.[Owner] IS NULL UNION ALL SELECT {0}, [Level] + 1 FROM {1} INNER JOIN [Hierarchy] [H] ON [H].[id] = {2}.[Owner]) SELECT * FROM [Hierarchy] ORDER BY [Level]", Columns, TableJoin, typeof(T).AfxDbName());
          else sql = string.Format("WITH Hierarchy AS (SELECT {0}, 0 AS [Level] FROM {1} WHERE {2}.[Owner]=@id UNION ALL SELECT {0}, [Level] + 1 FROM {1} INNER JOIN [Hierarchy] [H] ON [H].[id] = {2}.[Owner]) SELECT * FROM [Hierarchy] ORDER BY [Level]", Columns, TableJoin, typeof(T).AfxDbName());
        }
      }
      Log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", context.Target);
        foreach (DataRow dr in ExecuteDataSet(cmd).Tables[0].Rows)
        {
          T target = (T)Activator.CreateInstance(Type.GetType((string)dr["AssemblyFullname"]));
          target.Id = (Guid)dr["id"];
          FillObject(target, context, dr);
          if (target.GetType() != typeof(T)) RepositoryInterfaceFor(target.GetType()).LoadObjectCore(target, context);

          Guid ownerId = Guid.Empty;
          if (target.Owner != null) ownerId = target.Owner.Id;
          if (!useListDetection || (useListDetection && ownerId == context.Target)) objects.Add(target);
          target.IsDirty = false;
        }
      }

      return objects.ToArray();
    }

    #endregion

    #region DeleteObjectCore()

    protected override void DeleteObjectCore(T target)
    {
      string sql = string.Format("DELETE FROM {0} WHERE [id]=@id", typeof(T).AfxDbName());
      Log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", target.Id);
        cmd.ExecuteNonQuery();
      }
    }

    #endregion

    #region SqlRepositoryFor

    protected MsSqlObjectRepository<T1> SqlRepositoryFor<T1>()
      where T1 : class, IAfxObject
    {
      return (MsSqlObjectRepository<T1>)RepositoryFor<T1>();
    }

    #endregion

    #region GetCommand()

    protected SqlCommand GetCommand()
    {
      return (SqlCommand)ConnectionScope.CurrentScope.GetCommand();
    }

    protected SqlCommand GetCommand(string text)
    {
      return (SqlCommand)ConnectionScope.CurrentScope.GetCommand(text);
    }

    #endregion

    #region OwnerType

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

    #endregion
  }
}
