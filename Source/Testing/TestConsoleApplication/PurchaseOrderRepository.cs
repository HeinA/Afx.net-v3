using Afx.Collections;
using Afx.Data;
using Afx.Data.MsSql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  [Export(typeof(IObjectRepository))]
  public class PurchaseOrderRepository : MsSqlObjectRepository<PurchaseOrder>
  {
    public override string Columns
    {
      get { return string.Join(", ", GetColumns<Document>(), "[Test].[PurchaseOrder].[IsComplete], [Test].[PurchaseOrder].[CustomerName]"); }
    }

    public override string TableName
    {
      get { return "[Test].[PurchaseOrder]"; }
    }

    public override IEnumerable<string> GetJoins()
    {
      return GetJoins<Document>().Concat(new string[] { JoinOn(GetTableName<Document>()) });
    }

    public override void FillObject(PurchaseOrder target, DataRow dr)
    {
      FillObject<Document>(target, dr);

      if (dr["IsComplete"] != DBNull.Value) target.IsComplete = (bool)dr["IsComplete"];
      if (dr["CustomerName"] != DBNull.Value) target.CustomerName = (string)dr["CustomerName"];

      IDictionary items = (IDictionary)target.Items;
      foreach (var obj in GetSqlRepository<PurchaseOrderItem>().LoadObjects(target.Id)) items.Add(obj.Reference, obj);
    }

    protected override void SaveObjectCore(PurchaseOrder target, SaveContext context)
    {
      GetRepositoryInterface<Document>().SaveObjectCore(target, context);

      if (context.IsNew)
      {
        string sql = "INSERT INTO [Test].[PurchaseOrder] ([id], [IsComplete], [CustomerName]) VALUES (@id, @ic, @cn)";
        log.Debug(sql);

        using (SqlCommand cmd = GetCommand(sql))
        {
          cmd.Parameters.AddWithValue("@id", target.Id);
          cmd.Parameters.AddWithValue("@ic", target.IsComplete);
          cmd.Parameters.AddWithValue("@cn", string.IsNullOrWhiteSpace(target.CustomerName) ? (object)DBNull.Value : target.CustomerName);
          cmd.ExecuteNonQuery();
        }
      }
      else
      {
        string sql = "UPDATE [Test].[PurchaseOrder] SET [IsComplete]=@ic, [CustomerName]=@cn WHERE [id]=@id";
        log.Debug(sql);

        using (SqlCommand cmd = GetCommand(sql))
        {
          cmd.Parameters.AddWithValue("@ic", target.IsComplete);
          cmd.Parameters.AddWithValue("@cn", string.IsNullOrWhiteSpace(target.CustomerName) ? (object)DBNull.Value : target.CustomerName);
          cmd.Parameters.AddWithValue("@id", target.Id);
          cmd.ExecuteNonQuery();
        }
      }

      DeleteContext deleteContext = new DeleteContext(target.Id);
      foreach (var item in target.Items)
      {
        PurchaseOrderItem ao = target.Items[item];
        deleteContext.ActiveTargets.Add(ao.Id);
        GetRepository<PurchaseOrderItem>().SaveObject(ao);
      }
      if (!context.Merge) GetRepositoryInterface<PurchaseOrderItem>().DeleteObjectsCore(deleteContext);
    }
  }
}
