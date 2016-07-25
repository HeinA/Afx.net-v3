using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Afx.Data;
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.Data;
using Afx.Collections;

namespace TestConsoleApplication
{
  [Export(typeof(ObjectRepository<PurchaseOrderItem>))]
  public class PurchaseOrderItemRepository : MsSqlObjectRepository<PurchaseOrderItem>
  {
    public override string Columns
    {
      get { return "[Test].[PurchaseOrderItem].[id], [Afx].[RegisteredType].[FullName] as AssemblyFullName, [Test].[PurchaseOrderItem].[Owner], [Test].[PurchaseOrderItem].[Reference]"; }
    }

    public override string TableJoin
    {
      get { return "[Test].[PurchaseOrderItem] INNER JOIN [Afx].[RegisteredType] ON [Test].[PurchaseOrderItem].[RegisteredType]=[Afx].[RegisteredType].[id]"; }
    }

    public override void FillObject(PurchaseOrderItem target, LoadContext context, DataRow dr)
    {
      if (dr["Reference"] != DBNull.Value) target.Reference = RepositoryFor<InventoryItem>().LoadObject((Guid)dr["Reference"]);
    }

    protected override void SaveObjectCore(PurchaseOrderItem target, SaveContext context)
    {
      RepositoryFor<InventoryItem>().SaveObject(target.Reference, context);

      bool isNew = true;
      if (context.ShouldProcess(target))
      {
        isNew = IsNew(target.Id);
        if (isNew)
        {
          string sql = "INSERT INTO [Test].[PurchaseOrderItem] ([id], [RegisteredType], [Owner], [Reference]) SELECT @id, [RT].[id], @o, @r FROM [Afx].[RegisteredType] [RT] WHERE [RT].[FullName]=@fn";
          Log.Debug(sql);

          using (SqlCommand cmd = GetCommand(sql))
          {
            cmd.Parameters.AddWithValue("@id", target.Id);
            cmd.Parameters.AddWithValue("@fn", target.GetType().AfxTypeName());
            cmd.Parameters.AddWithValue("@o", target.Owner.Id);
            cmd.Parameters.AddWithValue("@r", target.Reference.Id);
            cmd.ExecuteNonQuery();
          }
        }
        //else
        //{
        //  string sql = "UPDATE [Test].[PurchaseOrderItem] SET [Owner]=@o, [Reference]=@r WHERE [id]=@id";
        //  log.Debug(sql);

        //  using (SqlCommand cmd = GetCommand(sql))
        //  {
        //    cmd.Parameters.AddWithValue("@o", target.Owner.Id);
        //    cmd.Parameters.AddWithValue("@r", target.Reference.Id);
        //    cmd.Parameters.AddWithValue("@id", target.Id);
        //    cmd.ExecuteNonQuery();
        //  }
        //}
      }
    }
  }
}
