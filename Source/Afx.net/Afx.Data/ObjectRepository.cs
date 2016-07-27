using Afx.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  [AfxBaseType]
  public abstract class ObjectRepository<T> : ObjectRepository, IObjectRepository
    where T : class, IAfxObject
  {
    #region LoadObject()

    public T LoadObject(Guid id)
    {
      LoadContext context = new LoadContext() { Target = id }; 
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
      LoadContext context = new LoadContext() { Target = owner };
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

    public static ObjectRepository<T> Instance()
    {
      return RepositoryFor<T>();
    }

    #endregion
  }

  public class ObjectRepository
  {
    static ObjectRepository()
    {
    }

    static Dictionary<string, Dictionary<Type, IObjectRepository>> mConnectionTypeNameRepositoryDictionary = new Dictionary<string, Dictionary<Type, IObjectRepository>>();

    static IObjectRepository GetObject(Type type)
    {
      Guard.ThrowOperationExceptionIfNull(ConnectionScope.CurrentScope, Properties.Resources.NoConnectionScope);

      string connectionName = ConnectionScope.CurrentScope.ConnectionName;
      string connectionTypeName = ConnectionScope.CurrentScope.Connection.GetType().AfxTypeName();

      if (!mConnectionTypeNameRepositoryDictionary.ContainsKey(connectionName))
      {
        Dictionary<Type, IObjectRepository> dict = new Dictionary<Type, IObjectRepository>();
        var orb = Afx.ExtensibilityManager.GetObject<IObjectRepositoryBuilder>(connectionTypeName);
        foreach (var type1 in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder())
        {
          dict.Add(type1, orb.BuildRepository(type1));
        }
        mConnectionTypeNameRepositoryDictionary.Add(connectionName, dict);
      }
      else
      {
      }

      var repositoryDictionary = mConnectionTypeNameRepositoryDictionary[connectionName];
      return repositoryDictionary[type];
    }


    protected static ObjectRepository<T> RepositoryFor<T>()
      where T : class, IAfxObject
    {
      return (ObjectRepository<T>)GetObject(typeof(T));
    }

    protected static IObjectRepository RepositoryInterfaceFor<T>()
      where T : class, IAfxObject
    {
      return GetObject(typeof(T));
    }

    protected static IObjectRepository RepositoryInterfaceFor(Type type)
    {
      return GetObject(type);
    }

    protected static DataSet ExecuteDataSet(IDbCommand cmd)
    {
      return DataBuilder.ExecuteDataSet(cmd);
    }
  }
}
