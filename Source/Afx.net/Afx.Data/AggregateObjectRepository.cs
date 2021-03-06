﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public abstract class AggregateObjectRepository
  {
    public abstract Type TargetType { get; }
  }

  public abstract class AggregateObjectRepository<T> : AggregateObjectRepository
    where T : class, IAfxObject
  {

    #region Type TargetType

    public override Type TargetType
    {
      get { return typeof(T); }
    }

    #endregion

    #region Load()

    public T Load(Guid id)
    {
      var sql = AggregateSelectsForObject;
      using (new StateSuppressor())
      using (var cmd = ConnectionScope.CurrentScope.GetCommand(sql))
      {
        AddParameter(cmd, "@id", id);
        return GetObjects(new ObjectDataRowCollection(cmd.AfxGetObjectData())).FirstOrDefault();
      }
    }

    #endregion

    #region Save()

    public void Save(T target)
    {
      DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter(target).WriteDatabase(target);
    }

    #endregion

    #region Delete()

    public void Delete(T target)
    {
      DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter(target).DeleteDatabase(target);
    }

    #endregion

    public abstract AggregateObjectQuery<T> Query(string conditions);


    #region LoadObjects

    protected internal T[] LoadObjects(AggregateObjectQuery<T> query)
    {
      var sql = string.Format("{0}; {1}", query.GetQuery(), AggregateSelectsForQuery);
      sql = string.Format(sql, Guid.NewGuid().ToString().ToUpperInvariant().Replace("-", string.Empty));
      using (new StateSuppressor())
      using (var cmd = ConnectionScope.CurrentScope.GetCommand(sql))
      {
        query.AppendParameters(cmd);
        return GetObjects(new ObjectDataRowCollection(cmd.AfxGetObjectData())).ToArray();
      }
    }

    #endregion

    #region GetObjectDataConverter()

    protected ObjectDataConverter<T1> GetObjectDataConverter<T1>()
      where T1 : class, IAfxObject
    {
      return DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter<T1>();
    }

    protected ObjectDataConverter GetObjectDataConverter(Type objectType)
    {
      return DataScope.CurrentScope.RepositoryFactory.GetObjectDataConverter(objectType);
    }

    #endregion

    protected abstract IEnumerable<T> GetObjects(ObjectDataRowCollection rows);
    protected abstract string AggregateSelectsForQuery { get; }
    protected abstract string AggregateSelectsForObject { get; }
    protected abstract void AddParameter(IDbCommand cmd, string name, object value);
  }
}
