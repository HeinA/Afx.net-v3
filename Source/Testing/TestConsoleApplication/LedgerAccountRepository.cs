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
  [Export(typeof(ObjectRepository<LedgerAccount>))]
  public class LedgerAccountRepository : MsSqlObjectRepository<LedgerAccount>
  {
    public override string Columns
    {
      get { return "[Test].[LedgerAccount].[id], [Afx].[RegisteredType].[FullName] as AssemblyFullName, [Test].[LedgerAccount].[Owner], [Test].[LedgerAccount].[Name]"; }
    }

    public override string TableJoin
    {
      get { return "[Test].[LedgerAccount] INNER JOIN [Afx].[RegisteredType] ON [Test].[LedgerAccount].[RegisteredType]=[Afx].[RegisteredType].[id]"; }
    }

    public override void FillObject(LedgerAccount target, LoadContext context, DataRow dr)
    {
      if (dr["Name"] != DBNull.Value) target.Name = (string)dr["Name"];

      if (dr["Owner"] != DBNull.Value)
      {
        Guid oid = (Guid)dr["Owner"];
        LedgerAccount owner = (LedgerAccount)context.GetObject(oid);
        owner.Accounts.Add(target);
      }
      context.RegisterObject(target);
    }

    protected override void SaveObjectCore(LedgerAccount target, SaveContext context)
    {
      bool isNew = true;
      if (context.ShouldProcess(target))
      {
        isNew = IsNew(target.Id);
        if (isNew)
        {
          string sql = "INSERT INTO [Test].[LedgerAccount] ([id], [RegisteredType], [Owner], [Name]) SELECT @id, [RT].[id], @o, @n FROM [Afx].[RegisteredType] [RT] WHERE [RT].[FullName]=@fn";
          Log.Debug(sql);

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
          string sql = "UPDATE [Test].[LedgerAccount] SET [Owner]=@o, [Name]=@n WHERE [id]=@id";
          Log.Debug(sql);

          using (SqlCommand cmd = GetCommand(sql))
          {
            cmd.Parameters.AddWithValue("@o", target.Owner == null ? (object)DBNull.Value : target.Owner.Id);
            cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(target.Name) ? (object)DBNull.Value : target.Name);
            cmd.Parameters.AddWithValue("@id", target.Id);
            cmd.ExecuteNonQuery();
          }
        }
      }

      foreach (var item in target.Accounts)
      {
        SaveObject(item, context);
      }
      if (!context.Merge)
      {
        foreach (LedgerAccount obj in target.Accounts.DeletedItems)
        {
          DeleteObject(obj);
        }
      }
    }
  }
}
