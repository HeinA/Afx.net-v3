using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public static class TypeExtender
  {
    public static string AfxDbName(this Type type, string propertyName)
    {
      return string.Format("[{0}].[{1}].[{2}]", MsSqlDataBuilder.GetSchema(type), type.Name, propertyName);
    }

    public static string AfxDbName(this Type type)
    {
      return string.Format("[{0}].[{1}]", MsSqlDataBuilder.GetSchema(type), type.Name);
    }

    public static IEnumerable<string> GetJoinedColumns(this Type type)
    {
      if (type.BaseType.GetCustomAttribute<AfxBaseTypeAttribute>() != null)
      {
        yield return "id".WithAlias();
        yield return "RegisteredType".WithAlias();
        if (type.GetGenericSubClass(typeof(AfxObject<>)) != null)
        {
          yield return "Owner".WithAlias();
        }
        if (type.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
        {
          yield return "Owner".WithAlias();
          yield return "Reference".WithAlias();
        }
      }
      else
      {
        foreach (var name in type.BaseType.GetJoinedColumns()) yield return name;
      }

      foreach (var pi in type.GetSqlSelectProperties())
      {
        yield return pi.Name.WithAlias();
      }

      yield break;
    }

    public static IEnumerable<string> GetObjectColumns(this Type type)
    {
      if (type.BaseType.GetCustomAttribute<AfxBaseTypeAttribute>() != null)
      {
        yield return type.AfxDbName("id");
        yield return type.AfxDbName("RegisteredType");
        if (type.GetGenericSubClass(typeof(AfxObject<>)) != null)
        {
          yield return type.AfxDbName("Owner");
        }
        if (type.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
        {
          yield return type.AfxDbName("Owner");
          yield return type.AfxDbName("Reference");
        }
      }
      else
      {
        foreach (var name in type.BaseType.GetObjectColumns()) yield return name;
      }

      foreach (var pi in type.GetSqlSelectProperties())
      {
        yield return pi.AfxDbName();
      }

      yield break;
    }
    public static IEnumerable<string> GetSqlSelectJoins(this Type type)
    {
      yield return string.Format("{0}", type.AfxDbName());
      var current = type;
      while (!current.BaseType.IsAfxBaseType())
      {
        yield return string.Format("{0} ON {1}.[id]={0}.[id]", current.BaseType.AfxDbName(), current.AfxDbName());
        current = current.BaseType;
      }

      yield break;
    }

    public static string JoinOn(this PropertyInfo pi, Type type)
    {
      return string.Format("{0} AS [\\{2}] ON [\\{1}].[{3}]=[\\{2}].[id]", pi.PropertyType.AfxDbName(), PropertyPath.GetFullName(type.Name), PropertyPath.GetFullName(pi.PropertyType.Name), pi.Name);
    }

    public static string JoinOn(this Type ownerType, string propertyName, Type referenceType)
    {
      return string.Format("{0} AS [\\{2}] ON [\\{1}].[id]=[\\{2}].[{3}]", ownerType.AfxDbName(), PropertyPath.GetFullName(referenceType.Name), PropertyPath.GetFullName(ownerType.Name), propertyName);
    }

    public static string WithAlias(this string propertyName)
    {
      return string.Format("[\\{1}].[{0}] AS [{1}.{0}]", propertyName, PropertyPath.GetPath());
    }

    public static string ColumnName(this string fullColumnSpecification)
    {
      return fullColumnSpecification.Substring(fullColumnSpecification.LastIndexOf('.') + 1);
    }

    public static IEnumerable<PropertyInfo> GetSqlSelectProperties(this Type type)
    {
      foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
      {
        yield return pi;
      }
    }
  }
}
