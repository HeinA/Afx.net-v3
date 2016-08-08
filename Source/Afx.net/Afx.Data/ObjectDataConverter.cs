﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public abstract class ObjectDataConverter
  {
    public abstract Type TargetType { get; }
    public abstract void WriteObject(ObjectDataRow source, ObjectDataRowCollection data);
    public abstract DatabaseWriteType WriteDatabase(IAfxObject source);
    public abstract void DeleteDatabase(IAfxObject source);
    protected ObjectDataConverter<T> GetObjectDataConverter<T>()
      where T : class, IAfxObject
    {
      return DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter<T>();
    }

    protected ObjectDataConverter GetObjectDataConverter(Type objectType)
    {
      return DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter(objectType);
    }

    protected ObjectDataConverter GetObjectDataConverter(IAfxObject obj)
    {
      return DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter(obj);
    }
  }

  public abstract class ObjectDataConverter<T> : ObjectDataConverter
    where T : class, IAfxObject
  {
    public override sealed void WriteObject(ObjectDataRow source, ObjectDataRowCollection data)
    {
      if (source.Instance == null)
      {
        source.Instance = Activator.CreateInstance<T>();
        source.Instance.Id = source.Id;
      }
      WriteObject((T)source.Instance, source, data);
    }

    public override sealed DatabaseWriteType WriteDatabase(IAfxObject source)
    {
      return WriteDatabase((T)source);
    }

    public override Type TargetType
    {
      get { return typeof(T); }
    }

    protected abstract void WriteObject(T target, ObjectDataRow source, ObjectDataRowCollection context);
    protected abstract DatabaseWriteType WriteDatabase(T source);
  }
}
