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

    public string ScopeName { get; private set; }

    public IEnumerable<RegisteredType> RegisteredTypes
    {
      get
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

    public int GetRegisteredTypeId(Type target)
    {
      return RegisteredTypes.First(rt => rt.Type.Equals(target)).Id;
    }

    public int GetRegisteredTypeId(IAfxObject target)
    {
      return GetRegisteredTypeId(target.GetType());
    }

    //internal ObjectDataConverter<T> GetObjectDataConverter<T>()
    //  where T : class, IAfxObject
    //{
    //  return (ObjectDataConverter<T>)GetObjectDataConverter(typeof(T));
    //}

    //internal ObjectDataConverter GetObjectDataConverter(IAfxObject obj)
    //{
    //  return GetObjectDataConverter(obj.GetType());
    //}

    //internal ObjectDataConverter GetObjectDataConverter(Type objectType)
    //{
    //  if (!mObjectWriterDictionary.ContainsKey(ScopeName))
    //  {
    //    List<ObjectDataConverter> list = new List<ObjectDataConverter>();
    //    foreach (var ow in Afx.ExtensibilityManager.GetObjects<ObjectDataConverter>(ConnectionType.AfxTypeName()))
    //    {
    //      list.Add(ow);
    //    }
    //    mObjectWriterDictionary.Add(ScopeName, list);
    //  }
    //  return mObjectWriterDictionary[ScopeName].FirstOrDefault(ow => ow.TargetType.Equals(objectType));
    //}

    protected Type ConnectionType
    {
      get
      {
        var connectionProvider = Afx.ExtensibilityManager.GetObject<IConnectionProvider>(ScopeName);
        return connectionProvider.GetConnection().GetType();
      }
    }

    public static DataScope DefaultScope { get; private set; }

    //public void BuildAndLoadRepositoriesInMemory()
    //{
    //  RepositoryBuilder.GetForDataScope().BuildAndLoadRepositoriesInMemory();
    //}
    //public void LoadRepositories()
    //{
    //  RepositoryBuilder.GetForDataScope().LoadRepositories();
    //}
    //public void BuildRepositories(bool debug)
    //{
    //  RepositoryBuilder.GetForDataScope().BuildRepositories(debug);
    //}

    public static void SetDefaultScope(string dataScopeName)
    {
      DefaultScope = new DataScope(dataScopeName);
    }

    [ThreadStatic]
    static Stack<DataScope> mScopeStack;
    static Stack<DataScope> ScopeStack
    {
      get { return mScopeStack ?? (mScopeStack = new Stack<DataScope>()); }
    }

    public static string CurrentScopeName
    {
      get { return ScopeStack.Count == 0 ? DefaultScope?.ScopeName : ScopeStack.Peek().ScopeName; }
    }

    public static DataScope CurrentScope
    {
      get { return ScopeStack.Count == 0 ? DefaultScope : ScopeStack.Peek(); }
    }

    //static Dictionary<string, DataCache> mDataCacheDictionary = new Dictionary<string, DataCache>();


    static Dictionary<string, RepositoryFactory> mRepositoryFactoryDictionary = new Dictionary<string, RepositoryFactory>();
    public RepositoryFactory RepositoryFactory
    {
      get
      {
        if (!mRepositoryFactoryDictionary.ContainsKey(CurrentScopeName))
        {
          RepositoryFactory rf = Afx.ExtensibilityManager.GetObject<RepositoryFactory>(ConnectionType.AfxTypeName());
          mRepositoryFactoryDictionary.Add(CurrentScopeName, rf);
        }
        return mRepositoryFactoryDictionary[CurrentScopeName];
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


    static Dictionary<string, List<RegisteredType>> mRegisteredTypeDictionary = new Dictionary<string, List<RegisteredType>>();
    static Dictionary<string, List<ObjectDataConverter>> mObjectWriterDictionary = new Dictionary<string, List<ObjectDataConverter>>();
  }
}
