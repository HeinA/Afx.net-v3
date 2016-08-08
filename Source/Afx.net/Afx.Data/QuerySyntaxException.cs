using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class QuerySyntaxException : DataException
  {
    public QuerySyntaxException()
    {
    }

    public QuerySyntaxException(string message)
      : base(message)
    {
    }

    public QuerySyntaxException(string message, Exception innerException)
      : base (message, innerException)
    {
    }
  }
}
