using Afx.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Afx
{
  public static class ExtensibilityManager
  {
    static ExtensibilityManager()
    {
      PreLoadDeployedAssemblies();

      AggregateCatalog aggregateCatalog = new AggregateCatalog();
      foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (a.GetCustomAttribute<AfxAssemblyAttribute>() != null)
        {
          aggregateCatalog.Catalogs.Add(new AssemblyCatalog(a));

          foreach (var t in a.DefinedTypes)
          {
            if (t.IsSubclassOf(typeof(AfxObject))) mBusinessObjectTypes.Add(t);
          }
        }
      }

      CompositionContainer = new CompositionContainer(aggregateCatalog);
    }

    static List<TypeInfo> mBusinessObjectTypes = new List<TypeInfo>();
    public static IEnumerable<TypeInfo> BusinessObjectTypes
    {
      get
      {
        foreach (var t in mBusinessObjectTypes)
        {
          yield return t;
        }
      }
    }

    static List<Assembly> mAfxAssemblies = new List<Assembly>();
    public static Type[] GetImplementationsOfGenericType(Type generic)
    {
      List<Type> types = new List<Type>();
      foreach (var assembly in mAfxAssemblies)
      {
        foreach (var t in assembly.DefinedTypes)
        {
          if (t.GetGenericSubClass(generic.UnderlyingSystemType) != null)
          {
            types.Add(t.UnderlyingSystemType);
          }
        }
      }
      return types.ToArray();
    }

    static CompositionContainer CompositionContainer { get; set; }

    public static object GetObject(Type type)
    {
      MethodInfo method = CompositionContainer.GetType().GetMethod("GetExportedValueOrDefault", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
      MethodInfo generic = method.MakeGenericMethod(type);
      return generic.Invoke(CompositionContainer, null);
    }

    public static T GetObject<T>()
    {
      return CompositionContainer.GetExportedValueOrDefault<T>();
    }

    public static IEnumerable<T> GetObjects<T>()
    {
      return CompositionContainer.GetExportedValues<T>();
    }

    public static IEnumerable<T> GetObjects<T>(string contractName)
    {
      return CompositionContainer.GetExportedValues<T>(contractName);
    }

    public static T GetObject<T>(string contractName)
    {
      return CompositionContainer.GetExportedValueOrDefault<T>(contractName);
    }

    #region void PreLoadDeployedAssemblies()

    static bool mAssembliesLoaded = false;

    public static void PreLoadDeployedAssemblies()
    {
      if (mAssembliesLoaded) return;
      foreach (var path in GetBinFolders())
      {
        PreLoadAssembliesFromPath(path);
      }
      mAssembliesLoaded = true;
    }

    static IEnumerable<string> GetBinFolders()
    {
      List<string> toReturn = new List<string>();
      if (HttpContext.Current != null)
      {
        toReturn.Add(HttpRuntime.BinDirectory);
      }
      else
      {
        toReturn.Add(AppDomain.CurrentDomain.BaseDirectory);
      }

      return toReturn;
    }

    public static void PreLoadAssembliesFromPath(string p)
    {
      FileInfo[] files = null;
      files = new DirectoryInfo(p).GetFiles("*.dll", SearchOption.AllDirectories);

      AssemblyName a = null;
      string s = null;
      foreach (var fi in files)
      {
        s = fi.FullName;
        a = AssemblyName.GetAssemblyName(s);
        if (!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => AssemblyName.ReferenceMatchesDefinition(a, assembly.GetName())))
        {
          Assembly.Load(a);
        }
      }
    }

    #endregion
  }
}
