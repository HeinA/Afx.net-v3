using Afx.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public abstract class AggregateObjectQuery<T>
    where T : class, IAfxObject
  {
    #region Constructors

    protected AggregateObjectQuery(AggregateObjectRepository<T> store)
    {
      Repository = store;
    }

    protected AggregateObjectQuery(AggregateObjectRepository<T> store, string conditions)
      : this(store)
    {
      Conditions = conditions;
    }

    #endregion

    protected AggregateObjectRepository<T> Repository { get; private set; }

    public string Conditions { get; set; }

    #region QueryParameter[] Parameters

    List<QueryParameter> mParameters = new List<QueryParameter>();
    protected QueryParameter[] Parameters
    {
      get { return mParameters.ToArray(); }
    }

    #endregion

    #region AddParameter()

    public AggregateObjectQuery<T> AddParameter(string name, object value)
    {
      name = name.Trim();
      if (!name.StartsWith("@")) throw new ArgumentException(Properties.Resources.InvalidParameterName);
      mParameters.Add(new QueryParameter() { Name = name, Value = value });
      return this;
    }

    #endregion

    protected internal abstract void AppendParameters(IDbCommand cmd);

    public ObjectCollection<T> Submit()
    {
      return new ObjectCollection<T>(Repository.LoadObjects(this));
    }

    protected internal abstract string GetQuery();

    #region class QueryParameter

    protected class QueryParameter
    {
      public string Name { get; set; }
      public object Value { get; set; }
    }

    #endregion
  }
}
