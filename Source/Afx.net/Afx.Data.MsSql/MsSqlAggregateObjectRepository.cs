using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public abstract class MsSqlAggregateObjectRepository<T> : AggregateObjectRepository<T>
    where T : class, IAfxObject
  {
    protected override void AddParameter(IDbCommand cmd, string name, object value)
    {
      SqlCommand sqlcmd = (SqlCommand)cmd;
      sqlcmd.Parameters.AddWithValue(name, value);
    }
  }
}
