using Afx.Data;
using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Business;

namespace TestConsoleApplication
{
  [Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(ObjectDataConverter))]
  public class LedgerAccountDataConverter : MsSqlObjectDataConverter<LedgerAccount>
  {
    protected override void WriteObject(LedgerAccount target, ObjectDataRow source, ObjectDataRowCollection context)
    {
      if (source.DataRow["Name"] != DBNull.Value) target.Name = (string)source.DataRow["Name"];

      foreach (var row in context.GetOwnedObjects(target.Id).Where(r => typeof(LedgerAccount).IsAssignableFrom(r.Type)).OrderBy(r => r.DataRow["Name"]))
      {
        if (row.Instance == null) GetObjectDataConverter(row.Type).WriteObject(row, context);
        target.Accounts.Add((LedgerAccount)row.Instance);
      }
    }

    protected override DatabaseWriteType WriteDatabase(LedgerAccount source)
    {
      DatabaseWriteType writeType = GetWriteType(source);
      switch (writeType)
      {
        case DatabaseWriteType.Insert:
          using (var cmd = GetCommand("INSERT INTO [Test].[LedgerAccount] ([id], [RegisteredType], [Owner], [Name]) VALUES (@id, @rt, @owner, @p1)"))
          {
            cmd.Parameters.AddWithValue("@id", source.Id); 
            cmd.Parameters.AddWithValue("@rt", DataScope.CurrentScope.GetRegisteredTypeId(source));
            cmd.Parameters.AddWithValue("@owner", source.Owner == null ? (object)DBNull.Value : source.Owner.Id);
            cmd.Parameters.AddWithValue("@p1", source.Name);
            cmd.ExecuteNonQuery();
          }
          break;

        case DatabaseWriteType.Update:
          using (var cmd = GetCommand("UPDATE [Test].[LedgerAccount] SET [Owner]=@owner, [Name]=@p1 WHERE [id]=@id"))
          {
            cmd.Parameters.AddWithValue("@owner", source.Owner == null ? (object)DBNull.Value : source.Owner.Id);
            cmd.Parameters.AddWithValue("@p1", source.Name);
            cmd.Parameters.AddWithValue("@id", source.Id);
            cmd.ExecuteNonQuery();
          }
          break;
      }

      foreach (var item in source.Accounts)
      {
        GetObjectDataConverter(item).WriteDatabase(item);
      }
      foreach (var item in source.Accounts.DeletedItems)
      {
        GetObjectDataConverter(item).DeleteDatabase(item);
      }

      return writeType;
    }
  }
}
