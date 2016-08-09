using Afx.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public static class DataExtensions
  {
    public static IEnumerable<PropertyInfo> AfxPersistentProperties(this Type type)
    {
      return AfxPersistentProperties(type, true, true);
    }

    public static IEnumerable<PropertyInfo> AfxPersistentProperties(this Type type, bool flattenHierarchy)
    {
      return AfxPersistentProperties(type, flattenHierarchy, true);
    }

    public static IEnumerable<PropertyInfo> AfxPersistentProperties(this Type type, bool flattenHierarchy, bool includeCollections)
    {
      if (type.AfxIsBaseType())
      {
        var pi = type.GetProperty("Id");
        if (pi != null) yield return pi;

        pi = type.GetProperty("Owner");
        if (pi != null) yield return pi;

        pi = type.GetProperty("Reference");
        if (pi != null) yield return pi;
      }
      else
      {
        foreach (var pi in type.BaseType.AfxPersistentProperties(includeCollections))
        {
          if (flattenHierarchy || (!flattenHierarchy && pi.DeclaringType.AfxIsBaseType())) yield return pi;
        }

        foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null || (includeCollections && pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null)))
        {
          yield return pi;
        }
      }

      yield break;
    }

    public static ObjectDataRow[] AfxGetObjectData(this IDbCommand cmd)
    {
      List<ObjectDataRow> list = new List<ObjectDataRow>();
      using (var dr = cmd.ExecuteReader())
      {
        while (!dr.IsClosed)
        {
          DataTable dt = new DataTable();
          dt.Load(dr);
          foreach (DataRow dr1 in dt.Rows)
          {
            list.Add(new ObjectDataRow(dr1));
          }
        }
      }
      return list.ToArray();
    }

    public static bool AfxIsPersistentObject(this Type target)
    {
      bool persistent = target.GetCustomAttribute<AggregateAttribute>(true) != null;
      if (persistent) return true;
      var ownedType = target.GetGenericSubClass(typeof(AfxObject<>));
      if (ownedType != null) return ownedType.GetGenericArguments()[0].AfxIsPersistentObject();
      ownedType = target.GetGenericSubClass(typeof(AssociativeObject<,>));
      if (ownedType != null) return ownedType.GetGenericArguments()[0].AfxIsPersistentObject();
      return false;
    }

    public static bool AfxIsAggregateObject(this Type target)
    {
      return target.GetCustomAttribute<AggregateObjectAttribute>(true) != null;
    }

    public static bool AfxIsAggregateCollection(this Type target)
    {
      return target.GetCustomAttribute<AggregateCollectionAttribute>(true) != null;
    }

    public static bool AfxIsAggregateRoot(this Type target)
    {
      return target.AfxIsAggregateObject() || target.AfxIsAggregateCollection();
    }

    public static IEnumerable<TypeInfo> PersistentTypesInDependecyOrder(this IEnumerable<TypeInfo> types)
    {
      List<TypeInfo> sortedByDataDependencies = new List<TypeInfo>();
      int retries = 0;
      Queue<TypeInfo> unsorted = new Queue<TypeInfo>(types.Where(t => t.AfxIsPersistentObject()));
      while (unsorted.Count > 0 && retries < unsorted.Count)
      {
        TypeInfo ti = unsorted.Dequeue();
        if (!AreAllDataDependenciesMet(ti, sortedByDataDependencies)) unsorted.Enqueue(ti);
        else sortedByDataDependencies.Add(ti);
      }

      foreach (var t in sortedByDataDependencies)
      {
        yield return t;
      }
    }

    public static IEnumerable<Type> PersistentTypesInDependecyOrder(this IEnumerable<Type> types)
    {
      List<TypeInfo> sortedByDataDependencies = new List<TypeInfo>();
      int retries = 0;
      Queue<Type> unsorted = new Queue<Type>(types.Where(t => t.AfxIsPersistentObject())); 
      while (unsorted.Count > 0 && retries < unsorted.Count)
      {
        Type ti = unsorted.Dequeue();
        if (!AreAllDataDependenciesMet(ti.GetTypeInfo(), sortedByDataDependencies)) unsorted.Enqueue(ti);
        else sortedByDataDependencies.Add(ti.GetTypeInfo());
      }

      foreach (var t in sortedByDataDependencies)
      {
        yield return t;
      }
    }

    static bool AreAllDataDependenciesMet(TypeInfo ti, List<TypeInfo> processed)
    {
      foreach (var pi in ti.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty).Where(pi1 => pi1.PropertyType.IsSubclassOf(typeof(AfxObject)) && pi1.GetCustomAttribute<PersistentAttribute>() != null))
      {
        if (pi.PropertyType != ti && !processed.Contains(pi.PropertyType.GetTypeInfo())) return false;
      }
      if (ti.BaseType.GetCustomAttribute<AfxBaseTypeAttribute>() == null)
      {
        if (!processed.Contains(ti.BaseType.GetTypeInfo())) return false;
      }
      return true;
    }

    public static string AfxLinqOrderByForObject(this Type targetType)
    {
      string linq = null;

      foreach (var a in targetType.GetCustomAttributes<OrderByAttribute>(true).Reverse())
      {
        foreach (var propertyNameWithOrder in a.Properties)
        {
          if (propertyNameWithOrder.ToUpperInvariant().EndsWith(" DESC"))
          {
            var propertyName = propertyNameWithOrder.Substring(0, propertyNameWithOrder.Length - 5);

            if (linq == null) linq = string.Format(".OrderByDescending(i => i.{0})", propertyName);
            else linq += string.Format(".ThenByDescending(i => i.{0})", propertyName);
          }
          else
          {
            var propertyName = propertyNameWithOrder;
            if (propertyNameWithOrder.ToUpperInvariant().EndsWith(" ASC"))
            {
              propertyName = propertyNameWithOrder.Substring(0, propertyNameWithOrder.Length - 4);
            }

            if (linq == null) linq = string.Format(".OrderBy(i => i.{0})", propertyName);
            else linq += string.Format(".ThenBy(i => i.{0})", propertyName);
          }
        }
      }

      return linq;
    }
    public static string AfxLinqOrderByForObjectDataRow(this Type targetType)
    {
      string linq = null;

      foreach (var a in targetType.GetCustomAttributes<OrderByAttribute>(true).Reverse())
      {
        foreach (var propertyNameWithOrder in a.Properties)
        {
          if (propertyNameWithOrder.ToUpperInvariant().EndsWith(" DESC"))
          {
            var propertyName = propertyNameWithOrder.Substring(0, propertyNameWithOrder.Length - 5);

            if (linq == null) linq = string.Format(".OrderByDescending(r => r.DataRow[\"{0}\"])", propertyName);
            else linq += string.Format(".ThenByDescending(r => r.DataRow[\"{0}\"])", propertyName);
          }
          else
          {
            var propertyName = propertyNameWithOrder;
            if (propertyNameWithOrder.ToUpperInvariant().EndsWith(" ASC"))
            {
              propertyName = propertyNameWithOrder.Substring(0, propertyNameWithOrder.Length - 4);
            }

            if (linq == null) linq = string.Format(".OrderBy(r => r.DataRow[\"{0}\"])", propertyName);
            else linq += string.Format(".ThenBy(r => r.DataRow[\"{0}\"])", propertyName);
          }
        }
      }

      return linq;
    }
  }
}
