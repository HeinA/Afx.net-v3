﻿using Afx.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  internal sealed class DataScopeCache
  {
    internal DataScopeCache()
    {
      DataScope = Afx.Data.DataScope.CurrentScopeName;

      foreach (var type in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder().Where(t => t.GetCustomAttribute<AggregateCollectionAttribute>() != null))
      {
        //if (GetDataCache(type) != null) continue;

        try
        {
          Type type1 = typeof(DataCache<>).MakeGenericType(type);
          ConstructorInfo ci = type1.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(DataScopeCache) }, null);
          ci.Invoke(new object[] { this });
        }
        catch (TargetInvocationException ex)
        {
          throw ex.InnerException;
        }
      }
    }

    static object mLock = new object();
    internal static object Lock
    {
      get { return mLock; }
    }

    class CachedObject
    {
      public CachedObject(IAfxObject target, DataCache dataCache)
      {
        Target = target;
        DataCache = dataCache;
      }

      public IAfxObject Target { get; private set; }
      public DataCache DataCache { get; private set; }
    }

    internal string DataScope { get; private set; }

    Dictionary<Guid, CachedObject> mObjectDictionary = new Dictionary<Guid, CachedObject>();
    Dictionary<Type, DataCache> mDataCacheDictionary = new Dictionary<Type, DataCache>();

    internal IAfxObject GetObject(Guid id)
    {
      lock (Lock)
      {
        if (mObjectDictionary.ContainsKey(id)) return mObjectDictionary[id].Target;
        else throw new InvalidOperationException();
      }
    }

    internal void ProcessObject(IAfxObject obj, DataCache dataCache)
    {
      AddObject(obj, dataCache);

      foreach (var pi in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).Where(pi1 => pi1.GetCustomAttribute<DataCacheAttribute>() != null && pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null && pi1.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>)) == null))
      {
        foreach (IAfxObject o in (IEnumerable)pi.GetValue(obj))
        {
          ProcessObject(o, dataCache);
        }
      }
    }

    internal void RegisterType(Type type, DataCache dataCache)
    {
      if (!mDataCacheDictionary.ContainsKey(type)) mDataCacheDictionary.Add(type, dataCache);
    }

    void AddObject(IAfxObject obj, DataCache dataCache)
    {
      if (mObjectDictionary.ContainsKey(obj.Id)) throw new InvalidOperationException();
      mObjectDictionary.Add(obj.Id, new CachedObject(obj, dataCache));
      Type current = obj.GetType();

      while (current.GetCustomAttribute<AfxBaseTypeAttribute>() == null)
      {
        RegisterType(current, dataCache);
        dataCache.RegisterObject(obj, current);
        current = current.BaseType;
      }
    }

    internal void ClearCache(DataCache dataCache)
    {
      foreach (var ct in mObjectDictionary.Values.Where(ct1 => ct1.DataCache == dataCache).ToArray())
      {
        mObjectDictionary.Remove(ct.Target.Id);
      }
    }

    internal IEnumerable<DataCache> DataCachesForTypes(IEnumerable<Type> types)
    {
      return types.Where(t => mDataCacheDictionary.ContainsKey(t)).Select(t => mDataCacheDictionary[t]).Distinct();
    }

    internal DataCache GetDataCache(Type type)
    {
      if (!mDataCacheDictionary.ContainsKey(type)) return null;
      return mDataCacheDictionary[type];
    }
  }
}
