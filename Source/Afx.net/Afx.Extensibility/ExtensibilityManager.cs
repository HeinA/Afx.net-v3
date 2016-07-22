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
            if (t.IsSubclassOf(typeof(Afx.AfxObject))) mBusinessObjectTypes.Add(t);
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

    static CompositionContainer CompositionContainer { get; set; }

    public static T GetObject<T>()
    {
      return CompositionContainer.GetExportedValueOrDefault<T>();
    }

    public static T GetObject<T>(string contractName)
    {
      return CompositionContainer.GetExportedValueOrDefault<T>(contractName);
    }

    #region void PreLoadDeployedAssemblies()

    static bool mAssembliesLoaded = false;
    static void PreLoadDeployedAssemblies()
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

    static void PreLoadAssembliesFromPath(string p)
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
