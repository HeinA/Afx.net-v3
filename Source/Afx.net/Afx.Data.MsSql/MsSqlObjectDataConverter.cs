using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public abstract class MsSqlObjectDataConverter<T> : ObjectDataConverter<T>
    where T : class, IAfxObject
  {
    #region GetCommand()

    protected SqlCommand GetCommand()
    {
      return GetCommand(string.Empty);
    }

    protected SqlCommand GetCommand(string text)
    {
      return (SqlCommand)ConnectionScope.CurrentScope.GetCommand(text);
    }

    #endregion

    #region DeleteDatabase()

    public override void DeleteDatabase(IAfxObject source)
    {
      Type implementationBaseType = typeof(T).AfxImplementationBaseType();
      using (var cmd = GetCommand(string.Format("DELETE FROM {0} WHERE [id]=@id", implementationBaseType.AfxDbName())))
      {
        cmd.Parameters.AddWithValue("@id", source.Id);
        cmd.ExecuteNonQuery();
      }
    }

    #endregion


    #region GetWriteType()

    protected DatabaseWriteType GetWriteType(IAfxObject source)
    {
      if (!source.IsDirty) return DatabaseWriteType.None;
      using (var cmd = GetCommand(string.Format("SELECT COUNT(1) FROM {0} WHERE [id]=@id", source.GetType().AfxDbName())))
      {
        cmd.Parameters.AddWithValue("@id", source.Id);
        return cmd.ExecuteScalar().Equals(0) ? DatabaseWriteType.Insert : DatabaseWriteType.Update;
      }
    }

    #endregion

    #region GetInstance()

    protected IAfxObject GetInstance(object id, ObjectDataRowCollection context)
    {
      if (id == null || id == DBNull.Value) return null;
      var odr = context.FirstOrDefault(r => r.Id.Equals(id));
      if (odr != null)
      {
        if (odr.Instance == null) GetObjectDataConverter(odr.Type).WriteObject(odr, context);
        return odr.Instance;
      }
      return DataScope.GetObject((Guid)id);
    }

    #endregion
  }
}
