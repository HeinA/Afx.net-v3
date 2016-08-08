using Afx.Data;
using Afx.Data.MsSql;
using System.Collections.Generic;
using System.Linq;

namespace TestConsoleApplication
{
  [System.ComponentModel.Composition.Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(ObjectDataConverter))]
  public class PurchaseOrderDataConverter : MsSqlObjectDataConverter<Test.Business.PurchaseOrder>
  {
    protected override void WriteObject(Test.Business.PurchaseOrder target, ObjectDataRow source, ObjectDataRowCollection context)
    {
      GetObjectDataConverter<Test.Business.Document>().WriteObject(source, context);

      if (source.DataRow["CustomerName"] != System.DBNull.Value) target.CustomerName = (string)source.DataRow["CustomerName"];
      if (source.DataRow["IsComplete"] != System.DBNull.Value) target.IsComplete = (bool)source.DataRow["IsComplete"];
      if (source.DataRow["SourceAccount"] != System.DBNull.Value) target.SourceAccount = (Test.Business.LedgerAccount)DataCache.GetObject((System.Guid)source.DataRow["SourceAccount"]);

      System.Collections.Generic.List<Test.Business.PurchaseOrderItem> list = new System.Collections.Generic.List<Test.Business.PurchaseOrderItem>();
      foreach (var item in context.GetOwnedObjects(target.Id).Where(r => typeof(Test.Business.PurchaseOrderItem).IsAssignableFrom(r.Type)))
      {
        if (item.Instance == null) GetObjectDataConverter(item.Type).WriteObject(item, context);
        list.Add((Test.Business.PurchaseOrderItem)item.Instance);
      }
      System.Collections.IDictionary dict = target.Items;
      foreach (var item in list.OrderBy(i => i.Reference.Name)) dict.Add(item.Reference, item);
    }

    protected override DatabaseWriteType WriteDatabase(Test.Business.PurchaseOrder source)
    {
      DatabaseWriteType writeType = GetObjectDataConverter<Test.Business.Document>().WriteDatabase(source);
      switch (writeType)
      {
        case DatabaseWriteType.Insert:
          using (var cmd = GetCommand("INSERT INTO [Test].[PurchaseOrder] ([id], [CustomerName], [IsComplete], [SourceAccount]) VALUES (@id, @p1, @p2, @p3)"))
          {
            cmd.Parameters.AddWithValue("@id", source.Id);
            cmd.Parameters.AddWithValue("@p1", source.CustomerName);
            cmd.Parameters.AddWithValue("@p2", source.IsComplete);
            cmd.Parameters.AddWithValue("@p3", source.SourceAccount ?? (object)System.DBNull.Value);
            cmd.ExecuteNonQuery();
          }
          break;

        case DatabaseWriteType.Update:
          using (var cmd = GetCommand("UPDATE [Test].[PurchaseOrder] SET [CustomerName]=@p1, [IsComplete]=@p2, [SourceAccount]=@p3 WHERE [id]=@id"))
          {
            cmd.Parameters.AddWithValue("@p1", source.CustomerName);
            cmd.Parameters.AddWithValue("@p2", source.IsComplete);
            cmd.Parameters.AddWithValue("@p3", source.SourceAccount ?? (object)System.DBNull.Value);
            cmd.Parameters.AddWithValue("@id", source.Id);
            cmd.ExecuteNonQuery();
          }
          break;
      }

      foreach (var item in source.Items)
      {
        var associative = source.Items[item];
        GetObjectDataConverter(associative.GetType()).WriteDatabase(associative);
      }
      foreach (var item in source.Items.DeletedItems)
      {
        GetObjectDataConverter(item).DeleteDatabase(item);
      }

      return writeType;
    }
  }
}
