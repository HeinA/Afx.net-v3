using Afx.Data;
using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace TestConsoleApplication
{
  [Export(ConnectionName, typeof(IConnectionProvider))]
  public class LocalConnectionProvider : IConnectionProvider
  {
    public const string ConnectionName = "Local";

    public const string MdfFile = @"Afx v3\local.mdf";
    public const string LdfFile = @"Afx v3\local.ldf";

    public IDbConnection GetConnection()
    {
      string mdfFilename = string.Format(@"{0}\{1}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), MdfFile);
      string connectionString = string.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={0};Integrated Security=True;", mdfFilename);
      return new SqlConnection(connectionString);
    }

    public bool VerifyConnection()
    {
      using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Suppress))
      {
        string mdfFilename = string.Format(@"{0}\{1}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), MdfFile);
        string ldfFilename = string.Format(@"{0}\{1}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), LdfFile);
        new FileInfo(mdfFilename).Directory.Create();
        if (!File.Exists(mdfFilename))
        {
          using (SqlConnection con = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True;"))
          {
            con.Open();

            string sql = string.Format(@"CREATE DATABASE [Afx] ON PRIMARY (NAME=Afx_Data, FILENAME = '{0}') LOG ON (NAME=Afx_log, FILENAME = '{1}')", mdfFilename, ldfFilename);
            SqlCommand command = new SqlCommand(sql, con);
            command.ExecuteNonQuery();
            con.Close();
          }
        }
        return true;
      }
    }
  }
}
