using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public static class TypeExtender
  {
    public static string AfxDbName(this Type type, string propertyName)
    {
      return string.Format("[{0}].[{1}].[{2}]", MsSqlBuilder.GetSchema(type), type.Name, propertyName);
    }

    public static string AfxDbName(this Type type)
    {
      return string.Format("[{0}].[{1}]", MsSqlBuilder.GetSchema(type), type.Name);
    }
  }
}
