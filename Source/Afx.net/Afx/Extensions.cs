using Afx.Collections;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx
{
  public static class Extensions
  {
    #region String

    public static string FormatWith(this string format, params object[] args)
    {
      if (format == null) throw new ArgumentNullException("format");
      return string.Format(format, args);
    }

    public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
    {
      if (format == null) throw new ArgumentNullException("format");
      return string.Format(provider, format, args);
    }

    #endregion

    #region Type

    #region GetGenericSubClass()

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
        if (type.BaseType == null) return null;
        type = type.BaseType.GetTypeInfo();
      }
      return null;
    }

    #endregion

    #region AfIsxBaseType()

    public static bool AfxIsBaseType(this TypeInfo type)
    {
      return type.GetCustomAttribute<AfxBaseTypeAttribute>() != null;
    }
    public static bool AfxIsBaseType(this Type type)
    {
      return AfxIsBaseType(type.GetTypeInfo());
    }

    #endregion

    #region IsAfxBasedType()

    public static bool AfxIsAfxType(this TypeInfo type)
    {
      return type.IsSubclassOf(typeof(AfxObject)) || type.GetGenericSubClass(typeof(ObjectCollection<>).GetTypeInfo()) != null;
    }

    public static bool AfxIsAfxType(this Type type)
    {
      return AfxIsAfxType(type.GetTypeInfo());
    }

    #endregion

    #region AfxImplementationBaseType()

    public static Type AfxImplementationBaseType(this Type type)
    {
      TypeInfo ti = type.GetTypeInfo();
      if (ti.BaseType.Equals(typeof(object))) return null;
      if (ti.BaseType.GetTypeInfo().GetCustomAttribute<AfxBaseTypeAttribute>() != null) return type;
      return ti.BaseType.AfxImplementationBaseType();
    }

    #endregion

    #region AfxTypeName()

    public static string AfxTypeName(this Type type)
    {
      return AfxTypeName(type.GetTypeInfo());
    }

    public static string AfxTypeName(this TypeInfo type)
    {
      return string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
    }

    #endregion

    #region AfxAggregateType()

    public static Type AfxAggregateType(this Type type)
    {
      return AfxAggregateType(type.GetTypeInfo()).AsType();
    }

    public static TypeInfo AfxAggregateType(this TypeInfo type)
    {
      if (type.GetCustomAttribute<AggregateAttribute>(true) != null) return type;
      var ownedType = type.GetGenericSubClass(typeof(AfxObject<>).GetTypeInfo());
      if (ownedType == null) ownedType = type.GetGenericSubClass(typeof(AssociativeObject<,>).GetTypeInfo());
      if (ownedType != null)
      {
        var ownerType = ownedType.GenericTypeArguments[0].GetTypeInfo();
        return ownerType.AfxAggregateType();
      }
      return null;
    }

    #endregion

    #region AfxAggregateReferenceType()

    public static bool AfxIsAggregateReferenceType(this Type type)
    {
      return AfxIsAggregateReferenceType(type.GetTypeInfo());
    }

    public static bool AfxIsAggregateReferenceType(this TypeInfo type)
    {
      return type.GetCustomAttribute<AggregateReferenceAttribute>(true) != null;
    }

    #endregion

    #endregion
  }
}
