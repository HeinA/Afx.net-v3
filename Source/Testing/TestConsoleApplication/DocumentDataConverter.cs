using Afx.Data.MsSql;
using Afx.Data;

namespace TestConsoleApplication
{
  [System.ComponentModel.Composition.Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(ObjectDataConverter))]
  public class DocumentDataConverter : MsSqlObjectDataConverter<Test.Business.Document>
  {
    protected override void WriteObject(Test.Business.Document target, ObjectDataRow source, ObjectDataRowCollection context)
    {
      if (source.DataRow["DocumentDate"] != System.DBNull.Value) target.DocumentDate = (System.DateTime)source.DataRow["DocumentDate"];
      if (source.DataRow["DocumentNumber"] != System.DBNull.Value) target.DocumentNumber = (string)source.DataRow["DocumentNumber"];
    }

    protected override DatabaseWriteType WriteDatabase(Test.Business.Document source)
    {
      DatabaseWriteType writeType = GetWriteType(source);
      switch (writeType)
      {
        case DatabaseWriteType.Insert:
          using (var cmd = GetCommand("INSERT INTO [Test].[Document] ([id], [RegisteredType], [DocumentDate], [DocumentNumber]) VALUES (@id, @p1, @p2, @p3)"))
          {
            cmd.Parameters.AddWithValue("@id", source.Id);
            cmd.Parameters.AddWithValue("@p1", DataScope.CurrentScope.GetRegisteredTypeId(source));
            cmd.Parameters.AddWithValue("@p2", source.DocumentDate);
            cmd.Parameters.AddWithValue("@p3", source.DocumentNumber);
            cmd.ExecuteNonQuery();
          }
          break;

        case DatabaseWriteType.Update:
          using (var cmd = GetCommand("UPDATE [Test].[Document] SET [DocumentDate]=@p1, [DocumentNumber]=@p2 WHERE [id]=@id"))
          {
            cmd.Parameters.AddWithValue("@p1", source.DocumentDate);
            cmd.Parameters.AddWithValue("@p2", source.DocumentNumber);
            cmd.Parameters.AddWithValue("@id", source.Id);
            cmd.ExecuteNonQuery();
          }
          break;
      }
      return writeType;
    }
  }
}
