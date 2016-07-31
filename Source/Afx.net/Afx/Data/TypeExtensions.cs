using Afx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public static class TypeExtensions
  {
    public static Type AggregateType(this Type type)
    {
      return AggregateType(type.GetTypeInfo()).AsType();
    }

    public static string AfxTypeName(this Type type)
    {
      try
      {
        return AfxTypeName(type.GetTypeInfo());
      }
      catch
      {
        throw;
      }
    }

    public static string AfxTypeName(this TypeInfo type)
    {
      return string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
    }

    public static TypeInfo AggregateType(this TypeInfo type)
    {
      if (type.GetCustomAttribute<AggregateAttribute>(true) != null) return type;
      var ownedType = type.GetGenericSubClass(typeof(AfxObject<>).GetTypeInfo());
      if (ownedType == null) ownedType = type.GetGenericSubClass(typeof(AssociativeObject<,>).GetTypeInfo());
      if (ownedType != null)
      {
        var ownerType = ownedType.GenericTypeArguments[0].GetTypeInfo();
        return ownerType.AggregateType();
      }
      return null;
    }
  }
}
