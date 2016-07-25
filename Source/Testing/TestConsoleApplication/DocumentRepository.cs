using Afx.Data;

namespace TestConsoleApplication
{
  [System.ComponentModel.Composition.Export(typeof(ObjectRepository<Document>))]
  public class DocumentRepository : Afx.Data.MsSql.MsSqlObjectRepository<Document>
  {
    public override string Columns
    {
      get { return "[Test].[Document].[id], [RegisteredType].[FullName] as AssemblyFullName, [Test].[Document].[DocumentNumber], [Test].[Document].[DocumentDate]"; }
    }

    public override string TableJoin
    {
      get { return "[Test].[Document] INNER JOIN [Afx].[RegisteredType] ON [Test].[Document].[RegisteredType]=[RegisteredType].[id]"; }
    }

    public override void FillObject(Document target, LoadContext context, System.Data.DataRow dr)
    {
      if (dr["DocumentNumber"] != System.DBNull.Value) target.DocumentNumber = (System.String)dr["DocumentNumber"];
      if (dr["DocumentDate"] != System.DBNull.Value) target.DocumentDate = (System.DateTime)dr["DocumentDate"];
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

          using (System.Data.SqlClient.SqlCommand cmd = GetCommand(sql))
          {
            cmd.Parameters.AddWithValue("@id", target.Id);
            cmd.Parameters.AddWithValue("@fn", target.GetType().AfxTypeName());
            cmd.Parameters.AddWithValue("@dn", System.String.IsNullOrWhiteSpace(target.DocumentNumber) ? (object)System.DBNull.Value : target.DocumentNumber);
            cmd.Parameters.AddWithValue("@dd", target.DocumentDate);
            cmd.ExecuteNonQuery();
          }
        }
        else
        {
          string sql = "UPDATE [Test].[Document] SET [DocumentNumber]=@dn, [DocumentDate]=@dd WHERE [id]=@id";
          Log.Debug(sql);

          using (System.Data.SqlClient.SqlCommand cmd = GetCommand(sql))
          {
            cmd.Parameters.AddWithValue("@dn", string.IsNullOrWhiteSpace(target.DocumentNumber) ? (object)System.DBNull.Value : target.DocumentNumber);
            cmd.Parameters.AddWithValue("@dd", target.DocumentDate);
            cmd.Parameters.AddWithValue("@id", target.Id);
            cmd.ExecuteNonQuery();
          }
        }
      } 
    }
  }
}
