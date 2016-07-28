using Afx.Data;
using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  [Export(ConnectionName, typeof(IConnectionProvider))]
  public class LocalConnectionProvider : IConnectionProvider
  {
    public const string ConnectionName = "Local";

    public IDbConnection GetConnection()
    {
      return new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString);
    }
  }
}
