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
    static CompositionContainer CompositionContainer { get; set; }

    #region Constructors

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

    #endregion

    #region IEnumerable<TypeInfo> BusinessObjectTypes

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

    #endregion

    #region GetObject()

    public static T GetObject<T>()
    {
      return CompositionContainer.GetExportedValueOrDefault<T>();
    }

    public static T GetObject<T>(string contractName)
    {
      return CompositionContainer.GetExportedValueOrDefault<T>(contractName);
    }

    public static object GetObject(Type type)
    {
      MethodInfo method = CompositionContainer.GetType().GetMethod("GetExportedValueOrDefault", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
      MethodInfo generic = method.MakeGenericMethod(type);
      return generic.Invoke(CompositionContainer, null);
    }

    #endregion

    #region GetObjects()

    public static IEnumerable<T> GetObjects<T>()
    {
      return CompositionContainer.GetExportedValues<T>();
    }

    public static IEnumerable<T> GetObjects<T>(string contractName)
    {
      return CompositionContainer.GetExportedValues<T>(contractName);
    }

    #endregion


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

    #endregion
  }
}
