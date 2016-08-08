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
  public class InventoryItemDataConverter : MsSqlObjectDataConverter<InventoryItem>
  {
    protected override void WriteObject(InventoryItem target, ObjectDataRow source, ObjectDataRowCollection context)
    {
      if (source.DataRow["Name"] != DBNull.Value) target.Name = (string)source.DataRow["Name"];
    }

    protected override DatabaseWriteType WriteDatabase(InventoryItem source)
    {
      DatabaseWriteType writeType = GetWriteType(source);
      switch (writeType)
      {
        case DatabaseWriteType.Insert:
          using (var cmd = GetCommand("INSERT INTO [Test].[InventoryItem] ([id], [RegisteredType], [Name]) VALUES (@id, @p1, @p2)"))
          {
            cmd.Parameters.AddWithValue("@id", source.Id);
            cmd.Parameters.AddWithValue("@p1", DataScope.CurrentScope.GetRegisteredTypeId(source));
            cmd.Parameters.AddWithValue("@p2", source.Name);
            cmd.ExecuteNonQuery();
          }
          break;

        case DatabaseWriteType.Update:
          using (var cmd = GetCommand("UPDATE [Test].[InventoryItem] SET [Name]=@p1 WHERE [id]=@id"))
          {
            cmd.Parameters.AddWithValue("@p1", source.Name);
            cmd.Parameters.AddWithValue("@id", source.Id);
            cmd.ExecuteNonQuery();
          }
          break;
      }
      return writeType;
    }
  }
}
