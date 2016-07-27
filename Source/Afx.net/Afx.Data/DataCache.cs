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
    internal DataCache(DataScopeCache dataScopeCache)
      : base(dataScopeCache)
    {
      Refresh();
    }

    public void Refresh()
    {
      DataScopeCache.RefreshDataCache<T>(this);
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
    protected DataCache(DataScopeCache dataScopeCache)
    {
      DataScopeCache = dataScopeCache;
    }

    public DataScopeCache DataScopeCache { get; private set; }

    public static void Initialize()
    {
      DataScopeCache dsc = GetDataScopeCache();
      foreach (var type in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder().Where(t => t.GetCustomAttribute<DataCacheAttribute>() != null))
      {
        Type type1 = typeof(DataCache<>).MakeGenericType(type);
        ConstructorInfo ci = type1.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(DataScopeCache) }, null);
        ci.Invoke(new object[] { dsc });
      }
    }

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

    public static DataScopeCache GetDataScopeCache()
    {
      DataScopeCache dsc = null;
      if (!mScopeDictionary.ContainsKey(DataScope.CurrentScope))
      {
        dsc = new DataScopeCache();
        mScopeDictionary.Add(DataScope.CurrentScope, dsc);
      }
      if (dsc == null) dsc = mScopeDictionary[DataScope.CurrentScope];
      return dsc;
    }
  }
}
