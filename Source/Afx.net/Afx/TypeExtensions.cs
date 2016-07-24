using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx
{
  public static class TypeExtensions
  {
    public static Type GetAfxImplementationRoot(this Type type)
    {
      TypeInfo ti = type.GetTypeInfo();
      if (ti.BaseType.Equals(typeof(object))) return null;
      if (ti.BaseType.GetTypeInfo().GetCustomAttribute<AfxBaseTypeAttribute>() != null) return type;
      return ti.BaseType.GetAfxImplementationRoot();
    }
  }
}
