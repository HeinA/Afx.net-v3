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
  public class PurchaseOrderItemDataConverter : MsSqlObjectDataConverter<PurchaseOrderItem>
  {
    protected override void WriteObject(PurchaseOrderItem target, ObjectDataRow source, ObjectDataRowCollection context)
    {
      var referenceRow = context.FirstOrDefault(r => r.Id.Equals((Guid)source.DataRow["Reference"]));
      if (referenceRow.Instance == null) GetObjectDataConverter(referenceRow.Type).WriteObject(referenceRow, context);
      target.Reference = (InventoryItem)referenceRow.Instance;
    }

    protected override DatabaseWriteType WriteDatabase(PurchaseOrderItem source)
    {
      GetObjectDataConverter<InventoryItem>().WriteDatabase(source.Reference);

      DatabaseWriteType writeType = GetWriteType(source);
      switch (writeType)
      {
        case DatabaseWriteType.Insert:
          using (var cmd = GetCommand("INSERT INTO [Test].[PurchaseOrderItem] ([id], [RegisteredType], [Owner], [Reference]) VALUES (@p1, @p2, @p3, @p4)"))
          {
            cmd.Parameters.AddWithValue("@p1", source.Id);
            cmd.Parameters.AddWithValue("@p2", DataScope.CurrentScope.GetRegisteredTypeId(source));
            cmd.Parameters.AddWithValue("@p3", source.Owner.Id);
            cmd.Parameters.AddWithValue("@p4", source.Reference.Id);
            cmd.ExecuteNonQuery();
          }
          break;
      }
      return writeType;
    }
  }
}
