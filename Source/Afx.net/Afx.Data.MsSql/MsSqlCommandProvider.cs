using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  [Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(ICommandProvider))]
  public class MsSqlCommandProvider : ICommandProvider
  {
    #region GetCommand()

    public IDbCommand GetCommand()
    {
      return new SqlCommand();
    }

    #endregion
  }
}
