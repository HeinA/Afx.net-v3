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
    protected SqlCommand GetCommand()
    {
      return GetCommand(string.Empty);
    }

    protected SqlCommand GetCommand(string text)
    {
      return (SqlCommand)ConnectionScope.CurrentScope.GetCommand(text);
    }

    protected DatabaseWriteType GetWriteType(IAfxObject source)
    {
      if (!source.IsDirty) return DatabaseWriteType.None;
      using (var cmd = GetCommand(string.Format("SELECT COUNT(1) FROM {0} WHERE [id]=@id", source.GetType().AfxDbName())))
      {
        cmd.Parameters.AddWithValue("@id", source.Id);
        return cmd.ExecuteScalar().Equals(0) ? DatabaseWriteType.Insert : DatabaseWriteType.Update;
      }
    }

    public override void DeleteDatabase(IAfxObject source)
    {
      Type implementationBaseType = typeof(T).AfxImplementationBaseType();
      using (var cmd = GetCommand(string.Format("DELETE FROM {0} WHERE [id]=@id", implementationBaseType.AfxDbName())))
      {
        cmd.Parameters.AddWithValue("@id", source.Id);
        cmd.ExecuteNonQuery();
      }
    }
  }
}
