using Afx.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public abstract class AggregateCollectionRepository
  {
    public abstract Type TargetType { get; }
  }

  public abstract class AggregateCollectionRepository<T> : AggregateCollectionRepository
    where T : class, IAfxObject
  {
    public override Type TargetType
    {
      get { return typeof(T); }
    }

    public ObjectCollection<T> LoadCollection()
    {
      string s = AggregateSelectsForObjects;
      using (new StateSuppressor())
      using (var cmd = ConnectionScope.CurrentScope?.GetCommand(s))
      {
        if (cmd == null) throw new InvalidOperationException(Properties.Resources.NoConnectionScope);
        return new ObjectCollection<T>(GetObjects(new ObjectDataRowCollection(cmd.AfxGetObjectData())).ToArray());
      }
    }

    public void Save(ObjectCollection<T> targets)
    {
      foreach (var target in targets)
      {
        DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter(target.GetType()).WriteDatabase(target);
      }

      foreach (var target in targets.DeletedItems)
      {
        DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter(target.GetType()).DeleteDatabase(target);
      }

      CollectionSaved?.Invoke(this, EventArgs.Empty);
    }

    protected ObjectDataConverter<T1> GetObjectDataConverter<T1>()
      where T1 : class, IAfxObject
    {
      return DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter<T1>();
    }

    protected ObjectDataConverter GetObjectDataConverter(Type objectType)
    {
      return DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter(objectType);
    }

    protected abstract IEnumerable<T> GetObjects(ObjectDataRowCollection rows);
    protected abstract string AggregateSelectsForObjects { get; }

    public event EventHandler CollectionSaved;
  }
}
