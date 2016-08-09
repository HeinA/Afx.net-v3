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
    public DataScope(string dataScopeName)
    {
      Guard.ThrowIfNullOrEmpty(dataScopeName, nameof(dataScopeName));
      ScopeName = dataScopeName;
      ScopeStack.Push(this);
    }

    public void Dispose()
    {
      if (ScopeStack.Count > 0) ScopeStack.Pop();
    }

    public static void SetDefaultScope(string dataScopeName)
    {
      DefaultScope = new DataScope(dataScopeName);
    }

    public static string CurrentScopeName
    {
      get { return ScopeStack.Count == 0 ? DefaultScope?.ScopeName : ScopeStack.Peek().ScopeName; }
    }

    public static DataScope CurrentScope
    {
      get { return ScopeStack.Count == 0 ? DefaultScope : ScopeStack.Peek(); }
    }

    public string ScopeName { get; private set; }
    public static DataScope DefaultScope { get; private set; }

    static object mLock = new object();

    #region Registered Types

    static Dictionary<string, List<RegisteredType>> mRegisteredTypeDictionary = new Dictionary<string, List<RegisteredType>>();

    public IEnumerable<RegisteredType> RegisteredTypes
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

    public int GetRegisteredTypeId(Type target)
    {
      return RegisteredTypes.First(rt => rt.Type.Equals(target)).Id;
    }

    public int GetRegisteredTypeId(IAfxObject target)
    {
      return GetRegisteredTypeId(target.GetType());
    }

    #endregion

    #region Cache

    static Dictionary<string, DataCache> mDataCacheDictionary = new Dictionary<string, DataCache>();
    public DataCache DataCache
    {
      get
      {
        lock (mLock)
        {
          if (!mDataCacheDictionary.ContainsKey(CurrentScopeName))
          {
            mDataCacheDictionary.Add(CurrentScopeName, new DataCache());
          }
          return mDataCacheDictionary[CurrentScopeName];
        }
      }
    }

    public static T GetObject<T>(Guid id)
      where T : class, IAfxObject
    {
      return CurrentScope.DataCache.GetObject<T>(id);
    }

    public  static IEnumerable<T> GetObjects<T>()
      where T : class, IAfxObject
    {
      return CurrentScope.DataCache.GetObjects<T>();
    }


    #endregion

    #region  Repositories

    static Dictionary<string, RepositoryFactory> mRepositoryFactoryDictionary = new Dictionary<string, RepositoryFactory>();
    public RepositoryFactory RepositoryFactory
    {
      get
      {
        lock (mLock)
        {
          if (!mRepositoryFactoryDictionary.ContainsKey(CurrentScopeName))
          {
            RepositoryFactory rf = Afx.ExtensibilityManager.GetObject<RepositoryFactory>(ConnectionType.AfxTypeName());
            mRepositoryFactoryDictionary.Add(CurrentScopeName, rf);
          }
          return mRepositoryFactoryDictionary[CurrentScopeName];
        }
      }
    }

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

    #endregion

    public void DoDataStructureValidation()
    {
      Guard.ThrowOperationExceptionIfNull(ConnectionScope.CurrentScope, Properties.Resources.NoConnectionScope);
      IDataBuilder builder = Afx.ExtensibilityManager.GetObject<IDataBuilder>(ConnectionType.AfxTypeName());
      builder.ValidateDataStructure();
    }

    protected Type ConnectionType
    {
      get
      {
        var connectionProvider = Afx.ExtensibilityManager.GetObject<IConnectionProvider>(ScopeName);
        return connectionProvider.GetConnection().GetType();
      }
    }

    [ThreadStatic]
    static Stack<DataScope> mScopeStack;
    static Stack<DataScope> ScopeStack
    {
      get { return mScopeStack ?? (mScopeStack = new Stack<DataScope>()); }
    }
  }
}
