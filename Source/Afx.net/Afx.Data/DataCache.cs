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
  internal class DataCache
  {
    object mLock = new object();
    
    #region Constructors

    public DataCache()
    {
      foreach (var type in DataScope.CurrentScope.RepositoryFactory.AggregateCollectionRepositoryTypes)
      {
        Refresh(type);
      }
    }

    #endregion

    #region GetObject()

    Dictionary<Guid, CachedObject> mObjectByIdDictionary = new Dictionary<Guid, CachedObject>();
    public T GetObject<T>(Guid id)
      where T : class, IAfxObject
    {
      lock (mLock)
      {
        if (!mObjectByIdDictionary.ContainsKey(id)) return null;
        return (T)mObjectByIdDictionary[id].TargetObject;
      }
    }

    public IAfxObject GetObject(Guid id)
    {
      lock (mLock)
      {
        if (!mObjectByIdDictionary.ContainsKey(id)) return null;
        return mObjectByIdDictionary[id].TargetObject;
      }
    }

    #endregion

    #region GetObjects()

    public IEnumerable<T> GetObjects<T>()
      where T : class, IAfxObject
    {
      lock (mLock)
      {
        return GetList(typeof(T)).Select(i => i.TargetObject).Cast<T>();
      }
    }

    #endregion



    #region Refresh

    void Refresh(Type rootType)
    {
      lock (mLock)
      {
        Clear(rootType);
        var mi = this.GetType().GetMethod("ProcessCollectionRepository", BindingFlags.Instance | BindingFlags.NonPublic);
        var miTyped = mi.MakeGenericMethod(rootType);
        miTyped.Invoke(this, null);
      }
    }

    #endregion

    #region Clear()

    void Clear(Type rootType)
    {
      foreach (var item in mObjectByIdDictionary.Values.Where(i => i.RootType.Equals(rootType)).ToArray())
      {
        mObjectByIdDictionary.Remove(item.TargetObject.Id);
      }

      foreach (var list in mObjectTypedListDictionary.Values)
      {
        foreach (var item in list.Where(i => i.RootType.Equals(rootType)).ToArray())
        {
          list.Remove(item);
        }
      }
    }

    #endregion

    #region ProcessCollectionRepository()

    void ProcessCollectionRepository<T>()
      where T : class, IAfxObject
    {
      try
      {
        var acr = DataScope.GetCollectionRepository<T>();
        acr.CollectionSaved -= CollectionSaved;
        acr.CollectionSaved += CollectionSaved;
        using (new ConnectionScope())
        {
          foreach (var target in acr.LoadCollection())
          {
            ProcessObject(typeof(T), target, target.GetType());
          }
        }
      }
      catch
      {
        throw;
      }
    }

    #endregion

    #region CollectionSaved()

    private void CollectionSaved(object sender, EventArgs e)
    {
      AggregateCollectionRepository acr = (AggregateCollectionRepository)sender;
      Refresh(acr.TargetType);
    }

    #endregion

    #region ProcessObject()

    void ProcessObject(Type rootType, IAfxObject target, Type processType)
    {
      if (processType.AfxIsBaseType())
      {
        return;
      }

      if (target.GetType() == processType)
      {
        if (!mObjectByIdDictionary.ContainsKey(target.Id)) mObjectByIdDictionary.Add(target.Id, new CachedObject(rootType, target));
      }

      ProcessObject(rootType, target, processType.BaseType);

      GetList(processType).Add(new CachedObject(rootType, target));

      foreach (var pi in processType.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.GetProperty).Where(pi1 => pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null && pi1.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>)) == null))
      {
        foreach (IAfxObject obj in (IList)pi.GetValue(target))
        {
          ProcessObject(rootType, obj, obj.GetType());
        }
      }
    }

    #endregion

    #region class CachedObject

    class CachedObject
    {
      public CachedObject(Type rootType, IAfxObject target)
      {
        RootType = rootType;
        TargetObject = target;
      }

      public Type RootType { get; private set; }
      public IAfxObject TargetObject { get; private set; }
    }

    #endregion

    #region GetList()

    Dictionary<Type, List<CachedObject>> mObjectTypedListDictionary = new Dictionary<Type, List<CachedObject>>();
    List<CachedObject> GetList(Type targetType)
    {
      if (!mObjectTypedListDictionary.ContainsKey(targetType))
      {
        mObjectTypedListDictionary.Add(targetType, new List<CachedObject>());
      }
      return mObjectTypedListDictionary[targetType];
    }

    #endregion
  }
}
