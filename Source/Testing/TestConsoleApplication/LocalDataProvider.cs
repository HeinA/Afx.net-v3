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
  public static class LocalDataProvider
  {
    public const string ConnectionName = "Local";

    [Export(ConnectionName, typeof(IDbConnection))]
    static SqlConnection LocalConnection
    {
      get { return new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString); }
    }

    [Export(ConnectionName, typeof(IDataBuilder))]
    static MsSqlDataBuilder MsSqlDataBuilder
    {
      get { return new MsSqlDataBuilder(); }
    }

    [Export(ConnectionName, typeof(IObjectRepositoryBuilder))]
    static MsSqlRepositoryBuilder MsSqlRepositoryBuilder
    {
      get { return new MsSqlRepositoryBuilder(); }
    }
  }
}
