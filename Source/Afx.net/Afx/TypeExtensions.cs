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

    public static Type GetGenericSubClass(this Type type, Type targetType)
    {
      return GetGenericSubClass(type.GetTypeInfo(), targetType.GetTypeInfo())?.AsType();
    }

    public static TypeInfo GetGenericSubClass(this TypeInfo type, TypeInfo targetType)
    {
      while (type != null && type != typeof(object).GetTypeInfo())
      {
        var cur = type.IsGenericType ? type.GetGenericTypeDefinition().GetTypeInfo() : type;
        if (targetType == cur)
        {
          return type;
        }
        type = type.BaseType.GetTypeInfo();
      }
      return null;
    }
  }
}
