using Afx.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  [AfxBaseType]
  public abstract class ObjectRepository<T> : ObjectRepository<T>.IObjectRepository
    where T : class, IAfxObject
  {
    #region LoadObject()

    public T LoadObject(Guid id)
    {
      LoadContext context = new LoadContext() { Id = id }; 
      return (T)ImplementationRootRepository.LoadObjectCore(context);
    }

    #endregion

    #region LoadObjects()

    public ObjectCollection<T> LoadObjects()
    {
      return LoadObjects(Guid.Empty);
    }

    public ObjectCollection<T> LoadObjects(Guid owner)
    {
      LoadContext context = new LoadContext() { Owner = owner };
      return LoadObjectsInner(context, ImplementationRootRepository);
    }

    ObjectCollection<T> LoadObjectsInner(LoadContext context, IObjectRepository repo)
    {
      ObjectCollection<T> objects = new ObjectCollection<T>();
      IAfxObject[] objs = repo.LoadObjectsCore(context);
      foreach (T obj in objs)
      {
        objects.Add(obj);
      }
      return objects;
    }

    #endregion

    #region SaveObject

    public void SaveObject(T target)
    {
      SaveObject(target, new SaveContext());
    }

    public void SaveObject(T target, SaveContext context)
    {
      RepositoryInterfaceFor(target.GetType()).SaveObjectCore(target, context);
    }

    #endregion

    #region SaveObjects

    public void SaveObjects(ObjectCollection<T> targets)
    {
      SaveObjects(targets, new SaveContext());
    }

    public void SaveObjects(ObjectCollection<T> targets, SaveContext context)
    {
      foreach (var item in targets)
      {
        RepositoryInterfaceFor(item.GetType()).SaveObjectCore(item, context);
      }
      if (!context.Merge)
      {
        foreach (T obj in targets.DeletedItems)
        {
          DeleteObject(obj);
        }
      }
    }

    #endregion

    #region DeleteObject

    public void DeleteObject(T target)
    {
      ImplementationRootRepository.DeleteObjectCore(target);
    }

    #endregion

    #region Protected Properties

    #region Type ImplementationRoot

    Type mImplementationRoot;
    bool mImplementationRootProcessed = false;
    protected Type ImplementationRoot
    {
      get
      {
        if (mImplementationRoot != null) return mImplementationRoot;
        if (mImplementationRootProcessed) Guard.ThrowOperationExceptionIfNull(mImplementationRoot, Properties.Resources.NotAnAfxType, typeof(T));
        mImplementationRoot = typeof(T).GetAfxImplementationRoot();
        Guard.ThrowOperationExceptionIfNull(mImplementationRoot, Properties.Resources.NotAnAfxType, typeof(T));
        mImplementationRootProcessed = true;
        return mImplementationRoot;
      }
    }

    #endregion

    #region IObjectRepository ImplementationRootRepository

    IObjectRepository mImplementationRootRepository;
    protected IObjectRepository ImplementationRootRepository
    {
      get
      {
        if (mImplementationRootRepository != null) return mImplementationRootRepository;
        mImplementationRootRepository = GetRepository(ImplementationRoot);
        return mImplementationRootRepository;
      }
    }

    #endregion

    #endregion

    #region IObjectRepository

    #region Protected Definition

    protected interface IObjectRepository
    {
      Type TargetType { get; }

      bool IsNew(Guid id);

      IAfxObject LoadObjectCore(LoadContext context);
      void LoadObjectCore(IAfxObject target, LoadContext context);

      IAfxObject[] LoadObjectsCore(LoadContext context);

      void SaveObjectCore(IAfxObject target, SaveContext context);

      void DeleteObjectCore(IAfxObject target);
    }

    #endregion

    protected abstract bool IsNew(Guid id);
    bool IObjectRepository.IsNew(Guid id)
    {
      return IsNew(id);
    }

    protected abstract void SaveObjectCore(T target, SaveContext context);
    void IObjectRepository.SaveObjectCore(IAfxObject target, SaveContext context)
    {
      SaveObjectCore((T)target, context);
    }

    protected abstract T LoadObjectCore(LoadContext context);
    IAfxObject IObjectRepository.LoadObjectCore(LoadContext context)
    {
      return LoadObjectCore(context);
    }

    protected abstract void LoadObjectCore(T target, LoadContext context);
    void IObjectRepository.LoadObjectCore(IAfxObject target, LoadContext context)
    {
      LoadObjectCore((T)target, context);
    }

    protected abstract T[] LoadObjectsCore(LoadContext context);
    IAfxObject[] IObjectRepository.LoadObjectsCore(LoadContext context)
    {
      return LoadObjectsCore(context);
    }

    protected abstract void DeleteObjectCore(T target);
    void IObjectRepository.DeleteObjectCore(IAfxObject target)
    {
      DeleteObjectCore((T)target);
    }

    Type IObjectRepository.TargetType { get { return typeof(T); } }

    #endregion

    #region Statics

    public static ObjectRepository<T> Instance()
    {
      return (ObjectRepository<T>)GetRepository(typeof(T));
    }

    protected static ObjectRepository<T1> RepositoryFor<T1>()
      where T1 : class, IAfxObject
    {
      return (ObjectRepository<T1>)GetRepository(typeof(T));
    }

    protected static IObjectRepository RepositoryInterfaceFor(Type type)
    {
      return GetRepository(type);
    }

    protected static IObjectRepository RepositoryInterfaceFor<T1>()
      where T1 : class, IAfxObject
    {
      return GetRepository(typeof(T1));
    }

    protected static DataSet ExecuteDataSet(IDbCommand cmd)
    {
      return DataBuilder.ExecuteDataSet(cmd);
    }

    static IObjectRepository GetRepository(Type objectType)
    {
      var or = Afx.ExtensibilityManager.GetObjects<IObjectRepository>().FirstOrDefault(or1 => or1.TargetType.Equals(objectType));
      Guard.ThrowOperationExceptionIfNull(or, Properties.Resources.TypeRepositoryNotFound, objectType);
      return or;
    }

    #endregion
  }
}
