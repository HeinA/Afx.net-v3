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

    public abstract void FillObject(T target, LoadContext context, DataRow dr);

    #region IsNew()

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

    #endregion

    #region LoadObjectCore()

    protected override T LoadObjectCore(LoadContext context)
    {
      string sql = string.Format("SELECT [OT].*, [RT].[Fullname] AS [AssemblyFullName] FROM {0} [OT] INNER JOIN [Afx].[RegisteredType] [RT] ON [OT].[RegisteredType]=[RT].[id] WHERE [OT].[id]=@id", typeof(T).AfxDbName());
      //log.Debug(sql);

      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", context.Id);
        DataSet ds = ExecuteDataSet(cmd);
        if (ds.Tables[0].Rows.Count != 1) throw new InvalidOperationException(Afx.Data.Properties.Resources.LoadOneRow);
        DataRow dr = ds.Tables[0].Rows[0];
        T target = (T)Activator.CreateInstance(Type.GetType((string)dr["AssemblyFullName"]));
        target.Id = context.Id;
        FillObject(target, context, dr);
        if (target.GetType() != typeof(T)) RepositoryInterfaceFor(target.GetType()).LoadObjectCore(target, context);
        target.IsDirty = false;
        return target;
      }
    }

    protected override void LoadObjectCore(T target, LoadContext context)
    {
      string sql = string.Format("SELECT * FROM {0} WHERE [id]=@id", typeof(T).AfxDbName());
      //log.Debug(sql);

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

      string sql = null;
      if (OwnerType == null) sql = string.Format("SELECT [OT].*, [RT].[FullName] AS [AssemblyFullname] FROM [Afx].[RegisteredType] [RT] INNER JOIN {0} [OT] ON [OT].[RegisteredType]=[RT].[id]", typeof(T).AfxDbName());
      else
      {
        if (OwnerType != typeof(T))
        {
          if (context.Owner == Guid.Empty) sql = string.Format("SELECT [OT].*, [RT].[FullName] AS [AssemblyFullname] FROM [Afx].[RegisteredType] [RT] INNER JOIN {0} [OT] ON [OT].[RegisteredType]=[RT].[id] WHERE [OT].[Owner] IS NULL", typeof(T).AfxDbName());
          else sql = string.Format("SELECT [OT].*, [RT].[FullName] AS [AssemblyFullname] FROM [Afx].[RegisteredType] [RT] INNER JOIN {0} [OT] ON [OT].[RegisteredType]=[RT].[id] WHERE [OT].[Owner]=@id", typeof(T).AfxDbName());
        }
        else
        {
          if (context.Owner == Guid.Empty) sql = sql = string.Format("WITH Hierarchy AS (SELECT *, 0 AS Level FROM {0} OT WHERE [OT].[Owner] IS NULL UNION ALL SELECT OT.*, Level + 1 FROM {0} OT INNER JOIN Hierarchy H ON H.id = OT.Owner) SELECT H.*, RT.FullName as AssemblyFullname FROM Hierarchy H INNER JOIN Afx.RegisteredType RT ON H.RegisteredType=RT.Id ORDER BY [Level]", typeof(T).AfxDbName());
          else sql = string.Format("WITH Hierarchy AS (SELECT *, 0 AS Level FROM {0} OT WHERE [OT].[id]=@id UNION ALL SELECT OT.*, Level + 1 FROM {0} OT INNER JOIN Hierarchy H ON H.id = OT.Owner) SELECT H.*, RT.FullName as AssemblyFullname FROM Hierarchy H INNER JOIN Afx.RegisteredType RT ON H.RegisteredType=RT.Id ORDER BY [Level]", typeof(T).AfxDbName());
        }
      }

      //log.Debug(sql);
      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", context.Owner);
        foreach (DataRow dr in ExecuteDataSet(cmd).Tables[0].Rows)
        {
          T target = (T)Activator.CreateInstance(Type.GetType((string)dr["AssemblyFullname"]));
          target.Id = (Guid)dr["id"];
          FillObject(target, context, dr);
          if (target.GetType() != typeof(T)) RepositoryInterfaceFor(target.GetType()).LoadObjectCore(target, context);

          Guid ownerId = Guid.Empty;
          if (target.Owner != null) ownerId = target.Owner.Id;
          if (ownerId == context.Owner) objects.Add(target);
          target.IsDirty = false;
        }
      }

      return objects.ToArray();
    }

    #endregion

    protected override void DeleteObjectCore(T target)
    {
      string sql = string.Format("DELETE FROM {0} WHERE [id]=@id", typeof(T).AfxDbName());
      using (SqlCommand cmd = GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@id", target.Id);
        cmd.ExecuteNonQuery();
      }
    }


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
