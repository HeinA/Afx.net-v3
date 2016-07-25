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
  public abstract class ObjectRepository<T> : ObjectRepository, IObjectRepository
    where T : class, IAfxObject
  {
    Type mImplementationRoot;
    Type ImplementationRoot
    {
      get
      {
        if (mImplementationRoot != null) return mImplementationRoot;
        mImplementationRoot = typeof(T).GetAfxImplementationRoot();
        Guard.ThrowOperationExceptionIfNull(mImplementationRoot, Properties.Resources.NotAnAfxType, typeof(T));
        return mImplementationRoot;
      }
    }

    IObjectRepository mImplementationRootRepository;
    IObjectRepository ImplementationRootRepository
    {
      get
      {
        if (mImplementationRootRepository != null) return mImplementationRootRepository;
        mImplementationRootRepository = GetRepository(ImplementationRoot);
        return mImplementationRootRepository;
      }
    }

    #region LoadObject()

    public T LoadObject(Guid id)
    {
      LoadContext context = ImplementationRootRepository.GetInstance(id);
      Type instanceType = context.LoadTargets[0].AssemblyType;
      var repo = GetRepository(instanceType);

      T obj = (T)repo.LoadObjectCore(context);
      return obj;
    }

    #endregion

    #region LoadObjects()

    public ObjectCollection<T> LoadObjects()
    {
      return LoadObjects(Guid.Empty);
    }

    public ObjectCollection<T> LoadObjects(Guid owner)
    {
      LoadContext context = ImplementationRootRepository.GetInstances(owner);
      return LoadObjectsInner(context, ImplementationRootRepository);
    }

    ObjectCollection<T> LoadObjectsInner(LoadContext context, IObjectRepository repo)
    {
      ObjectCollection<T> objects = new ObjectCollection<T>();
      foreach (var instanceType in context.LoadTargets.DistinctBy(t => t.AssemblyType).Select(t => t.AssemblyType))
      {
        repo = GetRepository(instanceType);

        LoadContext lc = new LoadContext(context.Owner);
        lc.LoadObjectTargets(context.LoadTargets.Where(t => t.AssemblyType.Equals(instanceType)));
        IAfxObject[] objs = repo.LoadObjectsCore(lc);
        foreach (T obj in objs)
        {
          objects.Add(obj);
        }
      }
      return objects;
    }

    #endregion

    #region SaveObject

    public void SaveObject(T target)
    {
      SaveObjectInner(target, new SaveContext());
    }

    public void SaveObject(T target, bool merge)
    {
      SaveObjectInner(target, new SaveContext() { Merge = merge });
    }

    void SaveObjectInner(T target, SaveContext context)
    {
      context.IsNew = ImplementationRootRepository.IsNew(target.Id);

      Type targetType = target.GetType();
      var repo = GetRepository(targetType);
      Guard.ThrowOperationExceptionIfNull(repo, Properties.Resources.TypeRepositoryNotFound, targetType);

      repo.SaveObjectCore(target, context);
    }

    #endregion

    #region SaveObjects

    public void SaveObjects(ObjectCollection<T> target)
    {
      SaveObjectsInner(target, new SaveContext());
    }

    public void SaveObjects(ObjectCollection<T> target, bool merge)
    {
      SaveObjectsInner(target, new SaveContext() { Merge = merge });
    }

    void SaveObjectsInner(ObjectCollection<T> target, SaveContext context)
    {
      DeleteContext deleteContext = new DeleteContext();
      foreach (var item in target)
      {
        context.IsNew = ImplementationRootRepository.IsNew(item.Id);

        Type targetType = item.GetType();
        var repo = GetRepository(targetType);
        Guard.ThrowOperationExceptionIfNull(repo, Properties.Resources.TypeRepositoryNotFound, targetType);

        repo.SaveObjectCore(item, context);
        deleteContext.ActiveTargets.Add(item.Id);
      }
      if (!context.Merge) GetRepositoryInterface<T>().DeleteObjectsCore(deleteContext);
    }

    #endregion

    #region IObjectRepository

    protected abstract LoadContext GetInstance(Guid id);
    LoadContext IObjectRepository.GetInstance(Guid id)
    {
      return GetInstance(id);
    }

    protected abstract LoadContext GetInstances(Guid owner);
    LoadContext IObjectRepository.GetInstances(Guid owner)
    {
      return GetInstances(owner);
    }

    protected abstract bool IsNew(Guid id);
    bool IObjectRepository.IsNew(Guid id)
    {
      return IsNew(id);
    }

    protected abstract void DeleteObjectsCore(DeleteContext context);
    void IObjectRepository.DeleteObjectsCore(DeleteContext context)
    {
      DeleteObjectsCore(context);
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

    protected abstract T[] LoadObjectsCore(LoadContext context);
    IAfxObject[] IObjectRepository.LoadObjectsCore(LoadContext context)
    {
      return LoadObjectsCore(context);
    }

    Type IObjectRepository.TargetType { get { return typeof(T); } }

    #endregion
  }

  public class ObjectRepository
  {
    public static ObjectRepository<T> GetRepository<T>()
      where T : class, IAfxObject
    {
      return (ObjectRepository<T>)GetRepository(typeof(T));
    }

    public static IObjectRepository GetRepositoryInterface<T>()
      where T : class, IAfxObject
    {
      return GetRepository(typeof(T));
    }

    internal static IObjectRepository GetRepository(Type objectType)
    {
      IObjectRepository or = (IObjectRepository)Afx.ExtensibilityManager.GetObjects<IObjectRepository>().FirstOrDefault(or1 => or1.TargetType.Equals(objectType));
      Guard.ThrowOperationExceptionIfNull(or, Properties.Resources.TypeRepositoryNotFound, objectType);
      return or;
    }

    protected static DataSet ExecuteDataSet(IDbCommand cmd)
    {
      return DataBuilder.ExecuteDataSet(cmd);
    }
  }
}
