using Afx.Collections;
using Afx.Data;
using Afx.Data.MsSql;
using System;
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
  public class InventoryItemRepository : MsSqlObjectRepository<InventoryItem>
  {
    public override string Columns
    {
      get { return "[Test].[InventoryItem].[id], [Test].[InventoryItem].[RegisteredType], [Test].[InventoryItem].[Name]"; }
    }

    public override string TableName
    {
      get { return "[Test].[InventoryItem]"; }
    }

    public override void FillObject(InventoryItem target, DataRow dr)
    {
      if (dr["Name"] != DBNull.Value) target.Name = (string)dr["Name"];
    }

    protected override void SaveObjectCore(InventoryItem target, SaveContext context)
    {
      if (context.IsNew)
      {
        string sql = "INSERT INTO [Test].[InventoryItem] ([id], [RegisteredType], [Name]) SELECT @id, [RT].[id], @n FROM [Afx].[RegisteredType] [RT] WHERE [RT].[FullName]=@fn";
        log.Debug(sql);

        using (SqlCommand cmd = GetCommand(sql))
        {
          cmd.Parameters.AddWithValue("@id", target.Id);
          cmd.Parameters.AddWithValue("@fn", target.GetType().AfxTypeName());
          cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(target.Name) ? (object)DBNull.Value : target.Name);
          cmd.ExecuteNonQuery();
        }
      }
      else
      {
        string sql = "UPDATE [Test].[InventoryItem] SET [Name]=@n WHERE [id]=@id";
        log.Debug(sql);

        using (SqlCommand cmd = GetCommand(sql))
        {
          cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(target.Name) ? (object)DBNull.Value : target.Name);
          cmd.Parameters.AddWithValue("@id", target.Id);
          cmd.ExecuteNonQuery();
        }
      }
    }
  }
}
