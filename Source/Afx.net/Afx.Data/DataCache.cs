using Afx.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public sealed class DataCache<T> : DataCache
    where T : class, IAfxObject
  {
    static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    internal DataCache(DataScopeCache dataScopeCache)
      : base(dataScopeCache)
    {
      Refresh();
    }

    public override void Refresh()
    {
      Log.InfoFormat("Refreshing Cache for {0}", typeof(T).FullName);

      base.Refresh();

      ObjectCollection<T> objects = null;
      using (new ConnectionScope())
      {
        objects = ObjectRepository<T>.Get().LoadObjects();
      }

      lock (DataScopeCache.Lock)
      {
        DataScopeCache.ClearCache(this);
        foreach (var obj in objects)
        {
          DataScopeCache.ProcessObject(obj, this);
        }
      }

      OnDataCacheUpdated();
    }

    internal void OnDataCacheUpdated()
    {
      DataCacheUpdated?.Invoke(this, EventArgs.Empty);
    }

    public static DataCache<T> Get()
    {
      return (DataCache<T>)GetDataScopeCache().GetDataCache(typeof(T));
    }

    public event EventHandler DataCacheUpdated;
  }

  public abstract class DataCache
  {
    internal DataCache(DataScopeCache dataScopeCache)
    {
      DataScopeCache = dataScopeCache;
    }

    Dictionary<Type, List<IAfxObject>> mTypeListDictionary;

    internal static IEnumerable<DataCache> ForTypes(IEnumerable<Type> types)
    {
      return GetDataScopeCache().DataCachesForTypes(types);
    }

    internal void RegisterObject(IAfxObject obj, Type registrationType)
    {
      if (!mTypeListDictionary.ContainsKey(registrationType)) mTypeListDictionary.Add(registrationType, new List<IAfxObject>());
      var list = mTypeListDictionary[registrationType];
      list.Add(obj);
    }

    public virtual void Refresh()
    {
      mTypeListDictionary = new Dictionary<Type, List<IAfxObject>>();
    }

    internal DataScopeCache DataScopeCache { get; private set; }

    //internal static void InitializeForDataScope()
    //{
    //  DataScopeCache dsc = GetDataScopeCache();
    //  foreach (var type in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder().Where(t => t.GetCustomAttribute<DataCacheAttribute>() != null))
    //  {
    //    if (dsc.GetDataCache(type) != null) continue;

    //    try
    //    {
    //      Type type1 = typeof(DataCache<>).MakeGenericType(type);
    //      ConstructorInfo ci = type1.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(DataScopeCache) }, null);
    //      ci.Invoke(new object[] { dsc });
    //    }
    //    catch (TargetInvocationException ex)
    //    {
    //      throw ex.InnerException;
    //    }
    //  }
    //}

    static Dictionary<string, DataScopeCache> mScopeDictionary = new Dictionary<string, DataScopeCache>();

    public static IAfxObject GetObject(Guid id)
    {
      return GetDataScopeCache().GetObject(id);
    }

    public static T GetObject<T>(Guid id)
        where T : class, IAfxObject
    {
      return (T)GetDataScopeCache().GetObject(id);
    }

    public static IEnumerable<T> GetObjects<T>()
    {
      lock (DataScopeCache.Lock)
      {
        return GetDataScopeCache().GetDataCache(typeof(T)).mTypeListDictionary[typeof(T)].Cast<T>();
      }
    }

    internal static DataScopeCache GetDataScopeCache()
    {
      DataScopeCache dsc = null;
      if (!mScopeDictionary.ContainsKey(DataScope.CurrentScopeName))
      {
        dsc = new DataScopeCache();
        mScopeDictionary.Add(DataScope.CurrentScopeName, dsc);
      }
      if (dsc == null) dsc = mScopeDictionary[DataScope.CurrentScopeName];
      return dsc;
    }
  }
}
