using Afx.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace Afx.Data.MsSql
{
  public class MsSqlAggregateObjectQuery<T> : AggregateObjectQuery<T>
    where T : class, IAfxObject
  {
    Stack<PropertyInfo> mPropertyStack = new Stack<PropertyInfo>();
    List<PropertyInfo> mJoinedProperties = new List<PropertyInfo>();
    List<string> mJoins = new List<string>();

    #region Constructors

    public MsSqlAggregateObjectQuery(AggregateObjectRepository<T> store)
      : base(store)
    {
    }

    public MsSqlAggregateObjectQuery(AggregateObjectRepository<T> store, string conditions)
      : base(store, conditions)
    {
    }

    #endregion 

    #region GetQuery()

    protected override string GetQuery()
    {
      mPropertyStack = new Stack<PropertyInfo>();
      mJoinedProperties = new List<PropertyInfo>();
      mJoins = new List<string>();

      string where = GetWhereClause(Conditions);
      string sql = string.Format("SELECT [\\].[id] INTO #{{0}} FROM (SELECT {0} FROM {1}) AS [\\]{2} WHERE {3}", string.Join(", ", typeof(T).AfxQueryColumns()), string.Join(" INNER JOIN ", typeof(T).AfxBaseJoins()), mJoins.Count == 0 ? string.Empty : string.Format(" INNER JOIN {0}", string.Join(" INNER JOIN ", mJoins)), where);
      return sql;
    }

    #endregion

    #region AppendParameters()

    protected override void AppendParameters(IDbCommand cmd)
    {
      SqlCommand sqlcmd = (SqlCommand)cmd;
      foreach (var p in Parameters)
      {
        sqlcmd.Parameters.AddWithValue(p.Name, p.Value);
      }
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
              foreach (var pi in currentType.AfxPersistentProperties())
              {
                if (index + pi.Name.Length >= text.Length) continue;
                if (pi.Name == text.Substring(index, pi.Name.Length))
                {
                  //Found a matching Property
                  processed = true;
                  index += pi.Name.Length;

                  if (pi.PropertyType.AfxIsAfxType())
                  {
                    Type propertyType = null;
                    if (text[index] == '[') propertyType = GetCastedType(text, ref index); // Property is casted
                    currentType = AddJoin(pi, propertyType);
                  }
                  else
                  {
                    AddCondition(sw, string.Format("[\\{0}].[{1}]", GetPath(), pi.Name), pi.PropertyType, text, ref index);
                    currentType = typeof(T);
                    mPropertyStack.Clear();
                  }

                  break;
                }
              }
              if (processed) continue;

              throw new QuerySyntaxException(string.Format(Properties.Resources.InvalidObjectProperty, currentType.FullName, index + 40 > text.Length ? text.Substring(index, text.Length - index) : text.Substring(index, 40)));
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
      string sourcePropertyName = "id";

      //Find the right join Type
      Type associativeCollection = pi.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>));
      if (associativeCollection != null)
      {
        //Join Associative Collections on the Associative Type
        var associativeType = associativeCollection.GetGenericArguments()[1];
        if (joinType != null && !(joinType.IsAssignableFrom(associativeType) || associativeType.IsAssignableFrom(joinType))) throw new InvalidCastException(string.Format(Properties.Resources.InvalidCast, associativeType, joinType));
        if (joinType == null)
        {
          joinType = associativeType;
          targetPropertyName = "Owner";
        }
      }
      else
      {
        //Join Collections on the Item Type
        Type objectCollection = pi.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>));
        if (objectCollection != null)
        {
          var itemType = objectCollection.GetGenericArguments()[0];
          if (joinType != null && !(joinType.IsAssignableFrom(itemType) || itemType.IsAssignableFrom(joinType))) throw new InvalidCastException(string.Format(Properties.Resources.InvalidCast, itemType, joinType));
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
            sourcePropertyName = pi.Name;
          }
        }
      }

      if (mJoinedProperties.Contains(pi)) return joinType;

      string innerSql = string.Format("SELECT {0} FROM {1}", string.Join(", ", joinType.AfxQueryColumns()), string.Join(" INNER JOIN ", joinType.AfxBaseJoins()));
      mJoins.Add(string.Format("({0}) AS [\\{1}] ON [\\{1}].[{3}]=[\\{2}].[{4}]", innerSql, GetFullName(pi.Name), GetPreviousPath(), targetPropertyName, sourcePropertyName));
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
      throw new QuerySyntaxException(string.Format(Properties.Resources.CouldNotResolveType, originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));
    }

    int GetCastEndOffset(string text, int startingIndex)
    {
      for (int i = startingIndex; i < text.Length; i++)
      {
        if (text[i] == ']') return i;
      }
      throw new QuerySyntaxException(string.Format(Properties.Resources.CouldNotResolveType, startingIndex + 40 > text.Length ? text.Substring(startingIndex, text.Length - startingIndex) : text.Substring(startingIndex, 40)));
    }

    #endregion

    #region AddCondition()

    void AddCondition(StringWriter sw, string fullColumnName, Type propertyType, string text, ref int index)
    {
      int originalIndex = index;
      string parameterName = null;
      string condition = GetCondition(text, ref index).Trim();
      QueryParameter qp = null;

      if (condition.ToUpperInvariant().StartsWith("DATEONLY"))
      {
        if (!typeof(Nullable<DateTime>).IsAssignableFrom(propertyType)) throw new QuerySyntaxException(string.Format(Properties.Resources.DateOnlyInvalidType, originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));
        condition = condition.Substring(8).Trim();
        fullColumnName = string.Format("CAST({0} AS DATE)", fullColumnName);
      }

      if (condition.StartsWith(">="))
      {
        parameterName = condition.Substring(2).Trim();
        qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
        sw.Write("{0}{1}{2}", fullColumnName, ">=", parameterName);
      }
      else if (condition.StartsWith("<="))
      {
        parameterName = condition.Substring(2).Trim();
        qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
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
          qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
          sw.Write("{0}{1}{2}", fullColumnName, "<>", parameterName);
        }
      }
      else if (condition.StartsWith(">"))
      {
        parameterName = condition.Substring(1).Trim();
        qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
        sw.Write("{0}{1}{2}", fullColumnName, ">", parameterName);
      }
      else if (condition.StartsWith("<"))
      {
        parameterName = condition.Substring(1).Trim();
        qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
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
          qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));
          sw.Write("{0}{1}{2}", fullColumnName, "=", parameterName);
        }
      }
      else if (condition.ToUpperInvariant().StartsWith("CONTAINS"))
      {
        if (!propertyType.Equals(typeof(string))) throw new QuerySyntaxException(string.Format(Properties.Resources.InvalidStringType, "CONTAINS", originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));
        parameterName = condition.Substring(8).Trim();
        qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));

        string value = qp.Value as string;
        if (value == null) throw new QuerySyntaxException(string.Format(Properties.Resources.OperatorOnlyValidOnStrings, "CONTAINS"));
        qp.Value = string.Format("%{0}%", value);

        sw.Write("{0}{1}{2}", fullColumnName, " LIKE ", parameterName);
      }
      else if (condition.ToUpperInvariant().StartsWith("STARTS WITH"))
      {
        if (!propertyType.Equals(typeof(string))) throw new QuerySyntaxException(string.Format(Properties.Resources.InvalidStringType, "STARTS WITH", originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));
        parameterName = condition.Substring(11).Trim();
        qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));

        string value = qp.Value as string;
        if (value == null) throw new QuerySyntaxException(string.Format(Properties.Resources.OperatorOnlyValidOnStrings, "STARTS WITH"));
        qp.Value = string.Format("{0}%", value);

        sw.Write("{0}{1}{2}", fullColumnName, " LIKE ", parameterName);
      }
      else if (condition.ToUpperInvariant().StartsWith("ENDS WITH"))
      {
        if (!propertyType.Equals(typeof(string))) throw new QuerySyntaxException(string.Format(Properties.Resources.InvalidStringType, "ENDS WITH", originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));
        parameterName = condition.Substring(9).Trim();
        qp = Parameters.FirstOrDefault(qp1 => qp1.Name.Equals(parameterName));

        string value = qp.Value as string;
        if (value == null) throw new QuerySyntaxException(string.Format(Properties.Resources.OperatorOnlyValidOnStrings, "ENDS WITH"));
        qp.Value = string.Format("%{0}", value);

        sw.Write("{0}{1}{2}", fullColumnName, " LIKE ", parameterName);
      }
      else if (condition.ToUpperInvariant().StartsWith("WITHIN"))
      {
        parameterName = condition.Substring(7).Trim();
        var parameters = Regex.Split(parameterName, "AND", RegexOptions.IgnoreCase);
        if (parameters.Length != 2) throw new QuerySyntaxException(string.Format(Properties.Resources.InvalidWithinParameterCount, originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));
        string startParameter = parameters[0].Trim();
        string endParameter = parameters[1].Trim();
        qp = Parameters.FirstOrDefault(qp2 => qp2.Name.Equals(endParameter.Trim()));
        var qp1 = Parameters.FirstOrDefault(qp2 => qp2.Name.Equals(startParameter.Trim()));
        if (qp1 == null) throw new QuerySyntaxException(string.Format(Properties.Resources.ParameterNotProvided, parameterName));

        sw.Write("({0}{1}{2} AND {0}{3}{4})", fullColumnName, ">=", startParameter, "<=", endParameter);
      }
      else if (condition.ToUpperInvariant().StartsWith("BETWEEN"))
      {
        parameterName = condition.Substring(7).Trim();
        var parameters = Regex.Split(parameterName, "AND", RegexOptions.IgnoreCase);
        if (parameters.Length != 2) throw new QuerySyntaxException(string.Format(Properties.Resources.InvalidBetweenParameterCount, originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));
        string startParameter = parameters[0].Trim();
        string endParameter = parameters[1].Trim();
        qp = Parameters.FirstOrDefault(qp2 => qp2.Name.Equals(endParameter.Trim()));
        var qp1 = Parameters.FirstOrDefault(qp2 => qp2.Name.Equals(startParameter.Trim()));
        if (qp1 == null) throw new QuerySyntaxException(string.Format(Properties.Resources.ParameterNotProvided, parameterName));

        sw.Write("({0}{1}{2} AND {0}{3}{4})", fullColumnName, ">", startParameter, "<", endParameter);
      }
      else throw new QuerySyntaxException(string.Format(Properties.Resources.InvalidOperator, originalIndex + 40 > text.Length ? text.Substring(originalIndex, text.Length - originalIndex) : text.Substring(originalIndex, 40)));

      if (qp == null) throw new QuerySyntaxException(string.Format(Properties.Resources.ParameterNotProvided, parameterName));
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
