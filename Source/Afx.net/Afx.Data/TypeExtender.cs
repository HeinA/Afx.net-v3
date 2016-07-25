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
    public static string AfxTypeName(this Type type)
    {
      return string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
    }

    public static Type GetGenericSubClass(this Type type, Type targetType)
    {
      while (type != null && type != typeof(object))
      {
        var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        if (targetType == cur)
        {
          return type;
        }
        type = type.BaseType;
      }
      return null;
    }
  }

  public static class TypeInfoExtender
  {
    public static string AfxTypeName(this TypeInfo type)
    {
      return string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
    }

    static List<TypeInfo> mSortedByDataDependencies;
    public static IEnumerable<TypeInfo> PersistentTypesInDependecyOrder(this IEnumerable<TypeInfo> types)
    {
      if (mSortedByDataDependencies == null)
      {
        mSortedByDataDependencies = new List<TypeInfo>();
        Queue<TypeInfo> unsorted = new Queue<TypeInfo>(types.Where(t => t.GetCustomAttribute<PersistentAttribute>(true) != null));
        while (unsorted.Count > 0)
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
      return true;
    }
  }
}
