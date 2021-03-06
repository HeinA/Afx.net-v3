﻿using Afx.Collections;
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
  [Export(typeof(ObjectRepository<PurchaseOrder>))]
  public class PurchaseOrderRepository : MsSqlObjectRepository<PurchaseOrder>
  {
    public override string Columns
    {
      get { return "[Test].[PurchaseOrder].[IsComplete], [Test].[PurchaseOrder].[CustomerName]"; }
    }

    public override string TableJoin
    {
      get { return "[Test].[PurchaseOrder]"; }
    }

    public override void FillObject(PurchaseOrder target, LoadContext context, DataRow dr)
    {
      //SqlRepositoryFor<Document>().FillObject(target, context, dr);
            
      if (dr["IsComplete"] != DBNull.Value) target.IsComplete = (bool)dr["IsComplete"];
      if (dr["CustomerName"] != DBNull.Value) target.CustomerName = (string)dr["CustomerName"];

      IDictionary items = (IDictionary)target.Items;
      foreach (var obj in RepositoryFor<PurchaseOrderItem>().LoadObjects(target.Id))
      {
        items.Add(obj.Reference, obj);
      }
    }

    protected override void SaveObjectCore(PurchaseOrder target, SaveContext context)
    {
      RepositoryInterfaceFor<Document>().SaveObjectCore(target, context);

      if (context.ShouldProcess(target))
      {
        if (IsNew(target.Id))
        {
          string sql = "INSERT INTO [Test].[PurchaseOrder] ([id], [IsComplete], [CustomerName]) VALUES (@id, @ic, @cn)";
          Log.Debug(sql);

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
          Log.Debug(sql);

          using (SqlCommand cmd = GetCommand(sql))
          {
            cmd.Parameters.AddWithValue("@ic", target.IsComplete);
            cmd.Parameters.AddWithValue("@cn", string.IsNullOrWhiteSpace(target.CustomerName) ? (object)DBNull.Value : target.CustomerName);
            cmd.Parameters.AddWithValue("@id", target.Id);
            cmd.ExecuteNonQuery();
          }
        }
      }

      foreach (var item in target.Items)
      {
        PurchaseOrderItem ao = target.Items[item];
        RepositoryFor<PurchaseOrderItem>().SaveObject(ao, context);
      }
      if (!context.Merge)
      {
        foreach (PurchaseOrderItem obj in target.Items.DeletedItems)
        {
          RepositoryFor<PurchaseOrderItem>().DeleteObject(obj);
        }
      }
    }
  }
}
