using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public static class PropertyInfoExtender
  {
    public static string AfxDbName(this PropertyInfo pi)
    {
      return string.Format("[{0}].[{1}].[{2}]", MsSqlDataBuilder.GetSchema(pi.DeclaringType), pi.DeclaringType.Name, pi.Name);
    }

    public static string AfxDbName(this PropertyInfo pi, Type definingType)
    {
      return string.Format("[{0}].[{1}].[{2}]", MsSqlDataBuilder.GetSchema(definingType == null ? pi.DeclaringType : definingType), definingType == null ? pi.DeclaringType.Name : definingType.Name, pi.Name);
    }
  }
}
