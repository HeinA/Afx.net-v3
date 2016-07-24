using Afx.Data;
using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  [Export(typeof(IObjectRepository))]
  public class LedgerAccountRepository : MsSqlObjectRepository<LedgerAccount>
  {
    public override string Columns
    {
      get { return "[Test].[LedgerAccount].[id], [Test].[LedgerAccount].[RegisteredType], [Test].[LedgerAccount].[Owner], [Test].[LedgerAccount].[Name]"; }
    }

    public override string TableName
    {
      get { return "[Test].[LedgerAccount]"; }
    }

    public override void FillObject(LedgerAccount target, DataRow dr)
    {
      if (dr["Name"] != DBNull.Value) target.Name = (string)dr["Name"];
      foreach (var a in LoadObjects(target.Id)) target.Accounts.Add(a);
    }

    protected override void SaveObjectCore(LedgerAccount target, SaveContext context)
    {
      if (context.IsNew)
      {
        string sql = "INSERT INTO [Test].[LedgerAccount] ([id], [RegisteredType], [Owner], [Name]) SELECT @id, [RT].[id], @o, @n FROM [Afx].[RegisteredType] [RT] WHERE [RT].[FullName]=@fn";
        log.Debug(sql);

        using (SqlCommand cmd = GetCommand(sql))
        {
          cmd.Parameters.AddWithValue("@id", target.Id);
          cmd.Parameters.AddWithValue("@fn", target.GetType().AfxTypeName());
          cmd.Parameters.AddWithValue("@o", target.Owner == null ? (object)DBNull.Value : target.Owner.Id);
          cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(target.Name) ? (object)DBNull.Value : target.Name);
          cmd.ExecuteNonQuery();
        }
      }
      else
      {
        string sql = "UPDATE [Test].[LedgerAccount] SET [Name]=@n WHERE [id]=@id";
        log.Debug(sql);

        using (SqlCommand cmd = GetCommand(sql))
        {
          cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(target.Name) ? (object)DBNull.Value : target.Name);
          cmd.Parameters.AddWithValue("@id", target.Id);
          cmd.ExecuteNonQuery();
        }
      }

      DeleteContext deleteContext = new DeleteContext(target.Id);
      foreach (var item in target.Accounts)
      {
        deleteContext.ActiveTargets.Add(item.Id);
        SaveObject(item, context.Merge);
      }
      if (!context.Merge && !context.IsNew) DeleteObjectsCore(deleteContext);
    }
  }
}
