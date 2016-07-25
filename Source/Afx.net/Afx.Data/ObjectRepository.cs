using Afx.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  [AfxBaseType]
  public abstract class ObjectRepository<T> : IObjectRepository
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

    /// <summary>
    /// Saves the object structure as well as deleting any objects 
    /// in any dependent collection's DeletedItems.
    /// The objects will only be written to the database if their IsDirty flag is set.
    /// </summary>
    /// <param name="target">The object to save</param>
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

    /// <summary>
    /// Saves the objects sturcture in the collection as well as deleting any objects in 
    /// the collection or any dependent collection's DeletedItems.
    /// The objects will only be written to the database if their IsDirty flag is set.
    /// </summary>
    /// <param name="targets">The collection to save</param>
    public void SaveObjects(ObjectCollection<T> targets)
    {
      SaveObjects(targets, new SaveContext());
    }

    /// <summary>
    /// Saves the objects in the collection according to the parameters specified in the SaveContext.
    /// Merging will not delete any objects, event if present in the collection's DeletedItems
    /// Setting the flag OnlyProcessDirty to false will result in all objects being updated.  
    /// This will impact performane for large or complex collections.
    /// </summary>
    /// <param name="targets">The collection to save</param>
    /// <param name="context">The context options for the operations</param>
    public void SaveObjects(ObjectCollection<T> targets, SaveContext context)
    {
      foreach (var item in targets)
      {
        //Call the Save Method on the appropriate Repository
        RepositoryInterfaceFor(item.GetType()).SaveObjectCore(item, context);
      }
      if (!context.Merge)
      {
        //Delete any objects in the Collections's DeletedItems if the context is not a Merge
        foreach (T obj in targets.DeletedItems)
        {
          DeleteObject(obj);
        }
      }
    }

    #endregion

    #region DeleteObject

    /// <summary>
    /// Deletes the object.
    /// </summary>
    /// <param name="target">The object to delete</param>
    public void DeleteObject(T target)
    {
      //Call the Delete Method onth root implementation.
      //Relationships & Triggers should take care of the entire hierarchy 
      ImplementationRootRepository.DeleteObjectCore(target);
    }

    #endregion

    #region Protected Properties

    #region Type ImplementationRoot

    Type mImplementationRoot;
    protected Type ImplementationRoot
    {
      get
      {
        if (mImplementationRoot != null) return mImplementationRoot;
        mImplementationRoot = typeof(T).GetAfxImplementationRoot();
        Guard.ThrowOperationExceptionIfNull(mImplementationRoot, Properties.Resources.NotAnAfxType, typeof(T));
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
        mImplementationRootRepository = RepositoryInterfaceFor(ImplementationRoot);
        return mImplementationRootRepository;
      }
    }

    #endregion

    #endregion

    #region IObjectRepository

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

    #endregion

    #region Statics

    static Dictionary<Type, IObjectRepository> mRepositories = new Dictionary<Type, IObjectRepository>();

    public static ObjectRepository<T> Instance()
    {
      return RepositoryFor<T>();
    }

    protected static ObjectRepository<T1> RepositoryFor<T1>()
      where T1 : class, IAfxObject
    {
      if (!mRepositories.ContainsKey(typeof(T1)))
      {
        var or = Afx.ExtensibilityManager.GetObject<ObjectRepository<T1>>();
        Guard.ThrowOperationExceptionIfNull(or, Properties.Resources.TypeRepositoryNotFound, typeof(T1));
        mRepositories.Add(typeof(T1), or);
      }
      return (ObjectRepository<T1>)mRepositories[typeof(T1)];
    }

    protected static IObjectRepository RepositoryInterfaceFor(Type type)
    {
      if (!mRepositories.ContainsKey(type))
      {
        Type generic = typeof(ObjectRepository<>).MakeGenericType(type);
        IObjectRepository or = (IObjectRepository)Afx.ExtensibilityManager.GetObject(generic);
        Guard.ThrowOperationExceptionIfNull(or, Properties.Resources.TypeRepositoryNotFound, type);
        mRepositories.Add(type, or);
      }
      return mRepositories[type];
    }

    protected static IObjectRepository RepositoryInterfaceFor<T1>()
      where T1 : class, IAfxObject
    {
      return RepositoryFor<T1>();
    }

    protected static DataSet ExecuteDataSet(IDbCommand cmd)
    {
      return DataBuilder.ExecuteDataSet(cmd);
    }

    #endregion
  }
}
