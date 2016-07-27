using Afx.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class DataScopeCache
  {
    internal DataScopeCache()
    {
      DataScope = Afx.Data.DataScope.CurrentScope;
    }

    protected static object mLock = new object();

    class CachedObject
    {
      public CachedObject(IAfxObject target, Type ownerType)
      {
        Target = target;
        OwnerType = ownerType;
      }

      public IAfxObject Target { get; private set; }
      public Type OwnerType { get; private set; }
    }

    public string DataScope { get; private set; }

    Dictionary<Guid, CachedObject> mObjectDictionary = new Dictionary<Guid, CachedObject>();
    Dictionary<Type, List<CachedObject>> mTypeListDictionary = new Dictionary<Type, List<CachedObject>>();
    Dictionary<Type, DataCache> mDataCacheDictionary = new Dictionary<Type, DataCache>();

    public IAfxObject GetObject(Guid id)
    {
      lock (mLock)
      {
        if (mObjectDictionary.ContainsKey(id)) return mObjectDictionary[id].Target;
        else throw new InvalidOperationException();
      }
    }

    internal void ProcessObject(IAfxObject obj)
    {
      ProcessObject(obj, obj.GetType());
    }

    void ProcessObject(IAfxObject obj, Type ownerType)
    {
      if (mObjectDictionary.ContainsKey(obj.Id)) throw new InvalidOperationException();
      mObjectDictionary.Add(obj.Id, new CachedObject(obj, ownerType));

      //if (!mDataCacheDictionary.ContainsKey(typeof(T))) mDataCacheDictionary.Add(typeof(T), dataCache);

      foreach (var pi in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).Where(pi1 => pi1.GetCustomAttribute<DataCacheAttribute>() != null && pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null))
      {
        foreach (IAfxObject o in (IEnumerable)pi.GetValue(obj))
        {
          ProcessObject(o, ownerType);
        }
      }
    }

    internal void ClearCache(Type ownerType)
    {
      foreach (var ct in mObjectDictionary.Values.Where(ct1 => ct1.OwnerType == ownerType).ToArray())
      {
        mObjectDictionary.Remove(ct.Target.Id);
      }
    }

    internal DataCache GetDataCache(Type type)
    {
      return mDataCacheDictionary[type];
    }

    internal void RefreshDataCache<T>(DataCache<T> dataCache)
      where T : class, IAfxObject
    {
      ObjectCollection<T> objects = null;
      using (new ConnectionScope())
      {
        objects = ObjectRepository<T>.Instance().LoadObjects();
      }

      lock (mLock)
      {
        ClearCache(typeof(T));
        foreach (var obj in objects)
        {
          ProcessObject(obj);
        }
      }

      dataCache.OnDataCacheUpdated();
    }
  }
}
