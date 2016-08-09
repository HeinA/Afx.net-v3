using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public abstract class RepositoryFactory
  {
    public abstract void Build(bool debug, bool inMemory);

    protected internal abstract ObjectDataConverter GetObjectDataConverter(Type target);

    protected internal ObjectDataConverter GetObjectDataConverter(IAfxObject obj)
    {
      return GetObjectDataConverter(obj.GetType());
    }

    protected internal ObjectDataConverter<T> GetObjectDataConverter<T>()
      where T : class, IAfxObject
    {
      return (ObjectDataConverter<T>)GetObjectDataConverter(typeof(T));
    }


    public abstract AggregateObjectRepository<T> GetObjectRepository<T>()
      where T : class, IAfxObject;

    public abstract AggregateCollectionRepository<T> GetCollectionRepository<T>()
      where T : class, IAfxObject;

    public abstract IEnumerable<Type> AggregateCollectionRepositoryTypes { get; }
  }
}
