using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public sealed class RepositoryBuilder
  {
    static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public RepositoryBuilder(string connectionTypeName)
    {
      Guard.ThrowOperationExceptionIfNull(DataScope.CurrentScopeName, Properties.Resources.NoDataScope);
      string connectionName = DataScope.CurrentScopeName;
      ConnectionTypeName = connectionTypeName;

      if (mRepositorySourceDictionary.ContainsKey(connectionName)) throw new InvalidOperationException(String.Format(Properties.Resources.NoRepositoriesLoadedForConnectionType, connectionTypeName)); 
    }

    public string ConnectionTypeName { get; private set; }

    Dictionary<Type, IObjectRepository> mRepositoryDictionary = new Dictionary<Type, IObjectRepository>();

    public void BuildRepositories(bool debug)
    {
      Log.InfoFormat("Building Repositories for connections of type {0}", ConnectionTypeName);
      var orb = Afx.ExtensibilityManager.GetObject<IObjectRepositoryBuilder>(ConnectionTypeName);
      foreach (var type in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder())
      {
        Log.InfoFormat("Building Repository for {0}", type.FullName);
        orb.BuildRepository(type, debug, false);
      }
    }

    public void LoadRepositories()
    {
      Log.InfoFormat("Loading Repositories for connections of type {0}", ConnectionTypeName);
      var orb = Afx.ExtensibilityManager.GetObject<IObjectRepositoryBuilder>(ConnectionTypeName);
      foreach (var type in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder())
      {
        Log.InfoFormat("Loading Repository for {0}", type.FullName);
        mRepositoryDictionary.Add(type, (IObjectRepository)Assembly.LoadFrom(orb.GetAssemblyName(type)).CreateInstance(orb.GetRepositoryTypeFullName(type)));
      }
    }

    public void BuildAndLoadRepositoriesInMemory()
    {
      Log.InfoFormat("Building & Loading Repositories for connections of type {0}", ConnectionTypeName);
      var orb = Afx.ExtensibilityManager.GetObject<IObjectRepositoryBuilder>(ConnectionTypeName);
      foreach (var type in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder())
      {
        Log.InfoFormat("Building & Loading Repository for {0}", type.FullName);
        mRepositoryDictionary.Add(type, (IObjectRepository)orb.BuildRepository(type, false, true).CreateInstance(orb.GetRepositoryTypeFullName(type)));
      }
    }

    static Dictionary<string, RepositoryBuilder> mRepositorySourceDictionary = new Dictionary<string, RepositoryBuilder>();
    public static ObjectRepository<T> GetRepository<T>()
      where T : class, IAfxObject
    {
      return (ObjectRepository<T>)GetRepository(typeof(T));
    }

    public static IObjectRepository GetRepository(Type type)
    {
      var rb = GetForDataScope();
      if (!rb.mRepositoryDictionary.ContainsKey(type)) throw new InvalidProgramException(string.Format(Properties.Resources.TypeRepositoryNotFound, type.AfxTypeName()));
      return rb.mRepositoryDictionary[type];
    }

    public static RepositoryBuilder GetForDataScope()
    {
      Guard.ThrowOperationExceptionIfNull(DataScope.CurrentScopeName, Properties.Resources.NoDataScope);
      string connectionName = DataScope.CurrentScopeName;
      var connectionProvider = Afx.ExtensibilityManager.GetObject<IConnectionProvider>(connectionName);

      string connectionTypeName = connectionProvider.GetConnection().GetType().AfxTypeName();
      if (!mRepositorySourceDictionary.ContainsKey(connectionTypeName))
      {
        mRepositorySourceDictionary.Add(connectionTypeName, new RepositoryBuilder(connectionTypeName));
      }
      return mRepositorySourceDictionary[connectionTypeName];
    }
  }
}
