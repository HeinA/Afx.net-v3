using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Afx.Data
{
  public class DataScope : IDisposable
  {
    static object mLock = new object();

    #region Constructors

    public DataScope(string dataScopeName)
    {
      Guard.ThrowIfNullOrEmpty(dataScopeName, nameof(dataScopeName));
      ScopeName = dataScopeName;
      ScopeStack.Push(this);
    }

    #endregion

    public string ScopeName { get; private set; }

    #region SetDefaultScope

    static DataScope DefaultScope { get; set; }
    public static void SetDefaultScope(string dataScopeName)
    {
      DefaultScope = new DataScope(dataScopeName);
    }

    #endregion

    #region DataScope CurrentScope

    public static DataScope CurrentScope
    {
      get { return ScopeStack.Count == 0 ? DefaultScope : ScopeStack.Peek(); }
    }

    #endregion


    #region Cache

    public static IAfxObject GetObject(Guid id)
    {
      return CurrentScope.DataCache.GetObject(id);
    }

    public static T GetObject<T>(Guid id)
      where T : class, IAfxObject
    {
      return CurrentScope.DataCache.GetObject<T>(id);
    }

    public static IEnumerable<T> GetObjects<T>()
      where T : class, IAfxObject
    {
      return CurrentScope.DataCache.GetObjects<T>();
    }

    static Dictionary<string, DataCache> mDataCacheDictionary = new Dictionary<string, DataCache>();
    DataCache DataCache
    {
      get
      {
        lock (mLock)
        {
          if (!mDataCacheDictionary.ContainsKey(CurrentScope?.ScopeName))
          {
            mDataCacheDictionary.Add(CurrentScope?.ScopeName, new DataCache());
          }
          return mDataCacheDictionary[CurrentScope?.ScopeName];
        }
      }
    }

    #endregion

    #region  Repositories

    public static AggregateObjectRepository<T> GetObjectRepository<T>()
      where T : class, IAfxObject
    {
      return CurrentScope.RepositoryFactory.GetObjectRepository<T>();
    }

    public static AggregateCollectionRepository<T> GetCollectionRepository<T>()
      where T : class, IAfxObject
    {
      return CurrentScope.RepositoryFactory.GetCollectionRepository<T>();
    }

    static Dictionary<string, RepositoryFactory> mRepositoryFactoryDictionary = new Dictionary<string, RepositoryFactory>();
    internal RepositoryFactory RepositoryFactory
    {
      get
      {
        lock (mLock)
        {
          if (!mRepositoryFactoryDictionary.ContainsKey(CurrentScope?.ScopeName))
          {
            RepositoryFactory rf = Afx.ExtensibilityManager.GetObject<RepositoryFactory>(ConnectionType.AfxTypeName());
            mRepositoryFactoryDictionary.Add(CurrentScope?.ScopeName, rf);
          }
          return mRepositoryFactoryDictionary[CurrentScope?.ScopeName];
        }
      }
    }

    #endregion

    #region Registered Types

    public int GetRegisteredTypeId(Type target)
    {
      return RegisteredTypes.First(rt => rt.Type.Equals(target)).Id;
    }

    public int GetRegisteredTypeId(IAfxObject target)
    {
      return GetRegisteredTypeId(target.GetType());
    }

    public Type GetRegisteredType(int id)
    {
      return RegisteredTypes.FirstOrDefault(rt => rt.Id.Equals(id))?.Type;
    }

    static Dictionary<string, List<RegisteredType>> mRegisteredTypeDictionary = new Dictionary<string, List<RegisteredType>>();
    IEnumerable<RegisteredType> RegisteredTypes
    {
      get
      {
        lock (mLock)
        {
          if (!mRegisteredTypeDictionary.ContainsKey(ScopeName))
          {
            using (var ts = new TransactionScope(TransactionScopeOption.Suppress))
            using (new ConnectionScope(true))
            {
              var loader = Afx.ExtensibilityManager.GetObject<IRegisteredTypeLoader>(ConnectionType.AfxTypeName());
              List<RegisteredType> list = new List<RegisteredType>();
              foreach (var rt in loader.LoadTypes())
              {
                list.Add(rt);
              }
              mRegisteredTypeDictionary.Add(ScopeName, list);
            }
          }
          return mRegisteredTypeDictionary[ScopeName].AsEnumerable();
        }
      }
    }

    #endregion

    #region ValidateDataStructure()

    public void ValidateDataStructure()
    {
      Guard.ThrowOperationExceptionIfNull(ConnectionScope.CurrentScope, Properties.Resources.NoConnectionScope);
      IDataBuilder builder = Afx.ExtensibilityManager.GetObject<IDataBuilder>(ConnectionType.AfxTypeName());
      builder.ValidateDataStructure();
    }

    #endregion

    #region BuildRepositories

    public void BuildRepositoriesInMemory()
    {
      RepositoryFactory.Build(false, true);
    }

    public void BuildRepositoriesDebug()
    {
      RepositoryFactory.Build(true, false);
    }

    public void BuildRepositoriesRelease()
    {
      RepositoryFactory.Build(false, false);
    }

    #endregion

    #region Dispose()

    public void Dispose()
    {
      if (ScopeStack.Count > 0) ScopeStack.Pop();
    }

    #endregion


    #region Type ConnectionType

    protected Type ConnectionType
    {
      get
      {
        var connectionProvider = Afx.ExtensibilityManager.GetObject<IConnectionProvider>(ScopeName);
        return connectionProvider.GetConnection().GetType();
      }
    }

    #endregion

    #region Stack<DataScope> ScopeStack

    [ThreadStatic]
    static Stack<DataScope> mScopeStack;
    static Stack<DataScope> ScopeStack
    {
      get { return mScopeStack ?? (mScopeStack = new Stack<DataScope>()); }
    }

    #endregion
  }
}
