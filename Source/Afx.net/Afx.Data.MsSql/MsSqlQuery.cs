using Afx.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public class MsSqlQuery<T>
  {
    Stack<PropertyInfo> mPropertyStack = new Stack<PropertyInfo>();
    List<PropertyInfo> mJoinedProperties = new List<PropertyInfo>();
    List<string> mJoins = new List<string>();
    List<QueryParameter> mParameters = new List<QueryParameter>();

    #region Constructors

    public MsSqlQuery()
    {
    }

    public MsSqlQuery(string conditions)
    {
      Conditions = conditions;
    }

    #endregion 

    #region string Conditions

    public const string ConditionsProperty = "Conditions";
    string mConditions;
    public string Conditions
    {
      get { return mConditions; }
      set { mConditions = value; }
    }

    #endregion

    #region AddParameter()

    public MsSqlQuery<T> AddParameter(string name, object value)
    {
      name = name.Trim();
      if (!name.StartsWith("@")) throw new InvalidOperationException(Properties.Resources.InvalidParameterName);
      mParameters.Add(new QueryParameter() { Name = name, Value = value });
      return this;
    }

    #endregion

    #region GetQuery()

    public string GetQuery()
    {
      mPropertyStack = new Stack<PropertyInfo>();
      mJoinedProperties = new List<PropertyInfo>();
      mJoins = new List<string>();

      string where = GetWhereClause(Conditions);
      string sql = string.Format("SELECT [\\].[id] FROM (SELECT {0} FROM {1}) AS [\\]{2} WHERE {3}", string.Join(", ", typeof(T).GetObjectColumns()), string.Join(" INNER JOIN ", typeof(T).GetSqlSelectJoins()), mJoins.Count == 0 ? string.Empty : string.Format(" INNER JOIN {0}", string.Join(" INNER JOIN ", mJoins)), where);
      return sql;
    }

    #endregion


    #region class QueryParameter

    class QueryParameter
    {
      public string Name { get; set; }
      public object Value { get; set; }
    }

    #endregion

    #region GetWhereClause()

    string GetWhereClause(string text)
    {
      Type currentType = typeof(T);
      using (StringWriter sw = new StringWriter())
      {
        int index = -1;
        while (index < text.Length - 1)
        {
          index++;

          switch (text[index])
          {
            case ' ':
            case '.':
              continue;

            case '(':
            case ')':
              sw.Write(text[index]);
              continue;

            case '&':
              sw.Write(" AND ");
              continue;

            case '|':
              sw.Write(" OR ");
              continue;

            default:
              bool processed = false;
              foreach (var pi in currentType.AllPersistentProperties())
              {
                if (index + pi.Name.Length >= text.Length) continue;
                if (pi.Name == text.Substring(index, pi.Name.Length))
                {
                  //Found a matching Property
                  processed = true;
                  index += pi.Name.Length;

                  if (pi.PropertyType.IsAfxBasedType())
                  {
                    Type propertyType = null;
                    if (text[index] == '[') propertyType = GetCastedType(text, ref index); // Property is casted
                    currentType = AddJoin(pi, propertyType);
                  }
                  else
                  {
                    AddCondition(sw, string.Format("[\\{0}].[{1}]", GetPath(), pi.Name), text, ref index);
                    currentType = typeof(T);
                    mPropertyStack.Clear();
                  }

                  break;
                }
              }
              if (processed) continue;

              throw new InvalidOperationException(string.Format(Properties.Resources.InvalidObjectProperty, index + 40 > text.Length ? text.Substring(index, text.Length - index) : text.Substring(index, 40))); //TODO
          }
        }

        return sw.ToString();
      }
    }

    #endregion

    #region AddJoin()

    private Type AddJoin(PropertyInfo pi, Type joinType)
    {
      mPropertyStack.Push(pi);

      string targetPropertyName = "id";
      //Find the right join Type
      Type associativeCollection = pi.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>));
      if (associativeCollection != null)
      {
        //Join Associative Collections on the Associative Type
        if (joinType != null && !(joinType.IsAssignableFrom(associativeCollection.GetGenericArguments()[1]) || associativeCollection.GetGenericArguments()[1].IsAssignableFrom(joinType))) throw new InvalidCastException(string.Format(Properties.Resources.InvalidCast, associativeCollection.GetGenericArguments()[1], joinType));
        if (joinType == null)
        {
          joinType = associativeCollection.GetGenericArguments()[1];
          targetPropertyName = "Owner";
        }
      }
      else
      {
        //Join Collections on the Item Type
        Type objectCollection = pi.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>));
        if (objectCollection != null)
        {
          if (joinType != null && !(joinType.IsAssignableFrom(objectCollection.GetGenericArguments()[0]) || objectCollection.GetGenericArguments()[0].IsAssignableFrom(joinType))) throw new InvalidCastException(string.Format(Properties.Resources.InvalidCast, objectCollection.GetGenericArguments()[0], joinType));
          if (joinType == null)
          {
            joinType = objectCollection.GetGenericArguments()[0];
            targetPropertyName = "Owner";
          }
        }
        else
        {
          //Join Properties on the Property Type
          if (joinType != null && !(joinType.IsAssignableFrom(pi.PropertyType) || pi.PropertyType.IsAssignableFrom(joinType))) throw new InvalidCastException(string.Format(Properties.Resources.InvalidCast, pi.PropertyType, joinType));
          if (joinType == null)
          {
            joinType = pi.PropertyType;
          }
        }
      }

      if (mJoinedProperties.Contains(pi)) return joinType;

      string innerSql = string.Format("SELECT {0} FROM {1}", string.Join(", ", joinType.GetObjectColumns()), string.Join(" INNER JOIN ", joinType.GetSqlSelectJoins()));
      mJoins.Add(string.Format("({0}) AS [\\{1}] ON [\\{1}].[{3}]=[\\{2}].[id]", innerSql, GetFullName(pi.Name), GetPreviousPath(), targetPropertyName));
      mJoinedProperties.Add(pi);
      return joinType;
    }

    public string GetFullName(string propertyName)
    {
      return string.Join(".", mPropertyStack.Select(pi => pi.Name).Reverse().Union(new string[] { propertyName.Replace("[", string.Empty).Replace("]", string.Empty).ColumnName() }));
    }

    public string GetPath()
    {
      return string.Join(".", mPropertyStack.Select(pi => pi.Name).Reverse());
    }

    public string GetPreviousPath()
    {
      if (mPropertyStack.Count <= 1) return string.Empty;
      return string.Join(".", mPropertyStack.Where(pi => !pi.Equals(mPropertyStack.Peek())).Select(pi => pi.Name).Reverse());
    }

    #endregion

    #region GetCastedType()

    Type GetCastedType(string text, ref int index)
    {
      int originalIndex = index;
      int i = GetCastEndOffset(text, index);
      string castTypeName = text.Substring(index + 1, i - index - 1);
      foreach (var type in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder())
      {
        if (type.FullName == castTypeName)
        {
          index = i + 1;
          return type;
        }
      }
      throw new InvalidOperationException(string.Format(Properties.Resources.CouldNotResolveType, originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));
    }

    int GetCastEndOffset(string text, int startingIndex)
    {
      for (int i = startingIndex; i < text.Length; i++)
      {
        if (text[i] == ']') return i;
      }
      throw new InvalidOperationException(string.Format(Properties.Resources.CouldNotResolveType, startingIndex + 40 > text.Length ? text.Substring(startingIndex, text.Length - startingIndex) : text.Substring(startingIndex, 40)));
    }

    #endregion

    #region AddCondition()

    void AddCondition(StringWriter sw, string fullColumnName, string text, ref int index)
    {
      int originalIndex = index;
      string parameterName = null;
      string condition = GetCondition(text, ref index).Trim();
      QueryParameter qp = null;

      if (condition.StartsWith(">="))
      {
        parameterName = condition.Substring(2).Trim();
        qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
        sw.Write("{0}{1}{2}", fullColumnName, ">=", parameterName);
      }
      else if (condition.StartsWith("<="))
      {
        parameterName = condition.Substring(2).Trim();
        qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
        sw.Write("{0}{1}{2}", fullColumnName, "<=", parameterName);
      }
      else if (condition.StartsWith("!="))
      {
        parameterName = condition.Substring(2).Trim();
        if (parameterName.ToUpperInvariant().Equals("NULL"))
        {
          qp = new QueryParameter();
          sw.Write("{0}{1}", fullColumnName, " IS NOT NULL");
        }
        else
        {
          qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
          sw.Write("{0}{1}{2}", fullColumnName, "<>", parameterName);
        }
      }
      else if (condition.StartsWith(">"))
      {
        parameterName = condition.Substring(1).Trim();
        qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
        sw.Write("{0}{1}{2}", fullColumnName, ">", parameterName);
      }
      else if (condition.StartsWith("<"))
      {
        parameterName = condition.Substring(1).Trim();
        qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
        sw.Write("{0}{1}{2}", fullColumnName, "<", parameterName);
      }
      else if (condition.StartsWith("="))
      {
        parameterName = condition.Substring(1).Trim();
        if (parameterName.ToUpperInvariant().Equals("NULL"))
        {
          qp = new QueryParameter();
          sw.Write("{0}{1}", fullColumnName, " IS NULL");
        }
        else
        {
          qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
          sw.Write("{0}{1}{2}", fullColumnName, "=", parameterName);
        }
      }
      else if (condition.ToUpperInvariant().StartsWith("CONTAINS"))
      {
        parameterName = condition.Substring(8).Trim();
        qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));

        string value = qp.Value as string;
        if (value == null) throw new InvalidOperationException(string.Format(Properties.Resources.OperatorOnlyValidOnStrings, "CONTAINS"));
        qp.Value = string.Format("%{0}%", value);

        sw.Write("{0}{1}{2}", fullColumnName, " LIKE ", parameterName);
      }
      else if (condition.ToUpperInvariant().StartsWith("STARTS WITH"))
      {
        parameterName = condition.Substring(11).Trim();
        qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));

        string value = qp.Value as string;
        if (value == null) throw new InvalidOperationException(string.Format(Properties.Resources.OperatorOnlyValidOnStrings, "STARTS WITH"));
        qp.Value = string.Format("{0}%", value);

        sw.Write("{0}{1}{2}", fullColumnName, " LIKE ", parameterName);
      }
      else if (condition.ToUpperInvariant().StartsWith("ENDS WITH"))
      {
        parameterName = condition.Substring(9).Trim();
        qp = mParameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));

        string value = qp.Value as string;
        if (value == null) throw new InvalidOperationException(string.Format(Properties.Resources.OperatorOnlyValidOnStrings, "ENDS WITH"));
        qp.Value = string.Format("%{0}", value);

        sw.Write("{0}{1}{2}", fullColumnName, " LIKE ", parameterName);
      }
      else throw new InvalidOperationException(string.Format(Properties.Resources.InvalidOperator, originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));

      if (qp == null) throw new InvalidOperationException(string.Format(Properties.Resources.ParameterNotProvided, parameterName));
    }

    string GetCondition(string text, ref int index)
    {
      int endIndex = index;
      int startIndex = index;
      for (int i = startIndex; i < text.Length; i++)
      {
        if (text[i] == '|' || text[i] == '&' || text[i] == ')')
        {
          endIndex = i;
          index = endIndex - 1;
          return text.Substring(startIndex, endIndex - startIndex).Trim();
        }
      }
      index = text.Length;
      return text.Substring(startIndex, text.Length - startIndex).Trim();
    }

    #endregion
  }
}
