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
      try
      {
        Refresh();
      }
      catch
      {
        throw;
      }
    }

    public override void Refresh()
    {
      Log.InfoFormat("Refreshing Cache for {0}", typeof(T).FullName);

      base.Refresh();

      ObjectCollection<T> objects = null;
      using (new ConnectionScope())
      {
        objects = new ObjectCollection<T>(DataScope.GetCollectionRepository<T>().LoadCollection());
      }

      lock (DataScopeCache.Lock)
      {
        DataScopeCache.RegisterType(typeof(T), this);
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
