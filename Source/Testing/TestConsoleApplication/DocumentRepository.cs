using Afx.Data;
using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Afx;
using System.Data;
using Afx.Collections;

namespace TestConsoleApplication
{
  [Export(typeof(ObjectRepository<Document>))]
  public class DocumentRepository : MsSqlObjectRepository<Document>
  {
    public override string Columns
    {
      get { return "[Test].[Document].[id], [RegisteredType].[FullName] as AssemblyFullName, [Test].[Document].[DocumentNumber], [Test].[Document].[DocumentDate]"; }
    }

    public override string TableJoin
    {
      get { return "[Test].[Document] INNER JOIN [Afx].[RegisteredType] ON [Test].[Document].[RegisteredType]=[RegisteredType].[id]"; }
    }

    public override void FillObject(Document target, LoadContext context, DataRow dr)
    {
      if (dr["DocumentNumber"] != DBNull.Value) target.DocumentNumber = (string)dr["DocumentNumber"];
      if (dr["DocumentDate"] != DBNull.Value) target.DocumentDate = (DateTime)dr["DocumentDate"];
    }

    protected override void SaveObjectCore(Document target, SaveContext context)
    {
      bool isNew = true;
      if (context.ShouldProcess(target))
      {
        isNew = IsNew(target.Id);
        if (isNew)
        {
          string sql = "INSERT INTO [Test].[Document] ([id], [RegisteredType], [DocumentNumber], [DocumentDate]) SELECT @id, [RT].[id], @dn, @dd FROM [Afx].[RegisteredType] [RT] WHERE [RT].[FullName]=@fn";
          Log.Debug(sql);

          using (SqlCommand cmd = GetCommand(sql))
          {
            cmd.Parameters.AddWithValue("@id", target.Id);
            cmd.Parameters.AddWithValue("@fn", target.GetType().AfxTypeName());
            cmd.Parameters.AddWithValue("@dn", string.IsNullOrWhiteSpace(target.DocumentNumber) ? (object)DBNull.Value : target.DocumentNumber);
            cmd.Parameters.AddWithValue("@dd", target.DocumentDate);
            cmd.ExecuteNonQuery();
          }
        }
        else
        {
          string sql = "UPDATE [Test].[Document] SET [DocumentNumber]=@dn, [DocumentDate]=@dd WHERE [id]=@id";
          Log.Debug(sql);

          using (SqlCommand cmd = GetCommand(sql))
          {
            cmd.Parameters.AddWithValue("@dn", string.IsNullOrWhiteSpace(target.DocumentNumber) ? (object)DBNull.Value : target.DocumentNumber);
            cmd.Parameters.AddWithValue("@dd", target.DocumentDate);
            cmd.Parameters.AddWithValue("@id", target.Id);
            cmd.ExecuteNonQuery();
          }
        }
      } 
    }
  }
}
