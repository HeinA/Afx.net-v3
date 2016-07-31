using Afx.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public static class TypeExtender
  {
  }

  public static class TypeInfoExtender
  {
    public static bool IsAfxBaseType(this TypeInfo type)
    {
      return type.GetCustomAttribute<AfxBaseTypeAttribute>() != null;
    }

    public static bool IsAfxBasedType(this TypeInfo type)
    {
      return type.IsSubclassOf(typeof(AfxObject)) || type.GetGenericSubClass(typeof(ObjectCollection<>)) != null;
    }

    public static bool IsAfxBaseType(this Type type)
    {
      return IsAfxBaseType(type.GetTypeInfo());
    }

    public static bool IsAfxBasedType(this Type type)
    {
      return IsAfxBasedType(type.GetTypeInfo());
    }

    public static IEnumerable<PropertyInfo> AllPersistentProperties(this Type type)
    {
      if (type.IsAfxBaseType())
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
        foreach (var pi in type.BaseType.AllPersistentProperties())
        {
          yield return pi;
        }

        foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null || pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null))
        {
          yield return pi;
        }
      }
      yield break;
    }

    static List<TypeInfo> mSortedByDataDependencies;
    public static IEnumerable<TypeInfo> PersistentTypesInDependecyOrder(this IEnumerable<TypeInfo> types)
    {
      if (mSortedByDataDependencies == null)
      {
        mSortedByDataDependencies = new List<TypeInfo>();
        int retries = 0;
        Queue<TypeInfo> unsorted = new Queue<TypeInfo>(types.Where(t => t.GetCustomAttribute<PersistentAttribute>(true) != null));
        while (unsorted.Count > 0 && retries < unsorted.Count)
        {
          TypeInfo ti = unsorted.Dequeue();
          if (!AreAllDataDependenciesMet(ti, mSortedByDataDependencies)) unsorted.Enqueue(ti);
          else mSortedByDataDependencies.Add(ti);
        }
      }

      foreach (var t in mSortedByDataDependencies)
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
  }
}
