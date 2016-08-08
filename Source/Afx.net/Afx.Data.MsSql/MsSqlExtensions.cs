using Afx.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public static class MsSqlExtensions
  {
    public static ObjectDataRow[] AfxGetObjectData(this SqlCommand cmd)
    {
      return ((IDbCommand)cmd).AfxGetObjectData();
    }

    #region Types

    #region AfxDbName()

    public static string AfxDbName(this Type type, string propertyName)
    {
      return string.Format("[{0}].[{1}].[{2}]", MsSqlDataBuilder.GetSchema(type), type.Name, propertyName);
    }

    public static string AfxDbName(this Type type)
    {
      return string.Format("[{0}].[{1}]", MsSqlDataBuilder.GetSchema(type), type.Name);
    }

    #endregion

    #region AfxQueryColumns()

    public static IEnumerable<string> AfxQueryColumns(this Type type)
    {
      if (type.BaseType.AfxIsBaseType())
      {
        yield return type.AfxDbName("id");
        yield return type.AfxDbName("RegisteredType");
        if (type.GetGenericSubClass(typeof(AfxObject<>)) != null)
        {
          yield return type.AfxDbName("Owner");
        }
        if (type.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
        {
          yield return type.AfxDbName("Owner");
          yield return type.AfxDbName("Reference");
        }
      }
      else
      {
        foreach (var name in type.BaseType.AfxQueryColumns()) yield return name;
      }

      foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null && pi1.GetSetMethod() != null))
      {
        yield return pi.AfxDbName();
      }

      yield break;
    }

    #endregion

    #region AfxInsertData()

    public static IEnumerable<Tuple<string, string, string>> AfxInsertData(this Type type)
    {
      yield return new Tuple<string, string, string>("[id]", "@id", "source.Id");
      if (type.BaseType.AfxIsBaseType())
      {
        yield return new Tuple<string, string, string>("[RegisteredType]", "@rt", "DataScope.CurrentScope.GetRegisteredTypeId(source)");
        if (type.GetGenericSubClass(typeof(AfxObject<>)) != null)
        {
          yield return new Tuple<string, string, string>("[Owner]", "@owner", "source.Owner");
        }
        if (type.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
        {
          yield return new Tuple<string, string, string>("[Owner]", "@owner", "source.Owner");
          yield return new Tuple<string, string, string>("[Reference]", "@reference", "source.Reference");
        }
      }

      int iCount = 0;
      foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null && pi1.GetGetMethod() != null))
      {
        yield return new Tuple<string, string, string>(string.Format("[{0}]", pi.Name), string.Format("@P_{0}", ++iCount), string.Format("source.{0}", pi.Name));
      }

      yield break;
    }

    #endregion

    #region AfxInsertData()

    public static IEnumerable<Tuple<string, string, string>> AfxUpdateData(this Type type)
    {
      if (type.BaseType.AfxIsBaseType())
      {
        if (type.GetGenericSubClass(typeof(AfxObject<>)) != null)
        {
          yield return new Tuple<string, string, string>("[Owner]", "@owner", "source.Owner");
        }
        if (type.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
        {
          yield return new Tuple<string, string, string>("[Owner]", "@owner", "source.Owner");
          yield return new Tuple<string, string, string>("[Reference]", "@reference", "source.Reference");
        }
      }

      int iCount = 0;
      foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null && pi1.GetGetMethod() != null))
      {
        yield return new Tuple<string, string, string>(string.Format("[{0}]", pi.Name), string.Format("@P_{0}", ++iCount), string.Format("source.{0}", pi.Name));
      }

      yield break;
    }

    #endregion

    //public static IEnumerable<PropertyInfo> AfxQueryProperties(this Type type)
    //{
    //  foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null && pi1.GetSetMethod() != null))
    //  {
    //    yield return pi;
    //  }

    //  yield break;
    //}

    //public static IEnumerable<PropertyInfo> AfxCollectionProperties(this Type type)
    //{
    //  foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(pi1 => pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null && pi1.GetGetMethod() != null))
    //  {
    //    yield return pi;
    //  }

    //  yield break;
    //}

    #region AfxBaseJoins()

    public static IEnumerable<string> AfxBaseJoins(this Type type)
    {
      yield return string.Format("{0}", type.AfxDbName());
      var current = type;
      while (!current.BaseType.AfxIsBaseType())
      {
        yield return string.Format("{0} ON {1}.[id]={0}.[id]", current.BaseType.AfxDbName(), current.AfxDbName());
        current = current.BaseType;
      }

      yield break;
    }

    #endregion

    #region AfxIsAggregate()

    public static bool AfxIsAggregate(this Type type)
    {
      return type.GetCustomAttribute<AggregateAttribute>(true) != null;
    }

    #endregion

    #region AfxAggregateJoins()

    public static IEnumerable<string> AfxAggregateJoins(this Type type)
    {
      if (type.AfxIsAggregate()) yield break;
      var pi = type.GetProperty("Owner", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.GetProperty);
      if (pi.PropertyType == type) yield break;
      if (pi == null) throw new InvalidOperationException(); //TODO:
      var declaredType = type.AfxImplementationBaseType();

      foreach (var join in pi.PropertyType.AfxAggregateJoins()) yield return join;
      yield return string.Format("{0} ON {1}.[Owner]={0}.[id]", pi.PropertyType.AfxDbName(), declaredType.AfxDbName());

      yield break;
    }

    #endregion

    #region AfxAggregateId()

    public static string AfxAggregateId(this Type type)
    {
      if (type.AfxIsAggregate()) return type.AfxDbName("id");
      var pi = type.GetProperty("Owner", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.GetProperty);
      if (pi.PropertyType == type) throw new InvalidOperationException(); //TODO:
      if (pi == null) throw new InvalidOperationException(); //TODO:
      return pi.PropertyType.AfxAggregateId();
    }

    #endregion

    public static IEnumerable<string> AfxSqlAggregateSelects(this Type type, SelectionType selectionType)
    {
      foreach (var persistentType in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder())
      {
        if (persistentType.IsSubclassOf(type))
        {
          foreach (var select in persistentType.AfxSqlAggregateSelects(selectionType)) yield return select;
        }
      }

      if (type.IsAbstract) yield break;

      var columns = string.Join(", ", type.AfxQueryColumns());
      var joins = string.Join(" INNER JOIN ", type.AfxBaseJoins().Union(type.AfxAggregateJoins()));
      string aggregateId = null;
      if (selectionType != SelectionType.All) aggregateId = type.AfxAggregateId();

      if (selectionType == SelectionType.All) yield return string.Format("SELECT {0} FROM {1}", columns, joins);
      else if (selectionType == SelectionType.Id) yield return string.Format("SELECT {0} FROM {1} WHERE {2}=@id", columns, joins, aggregateId);
      else yield return string.Format("SELECT {0} FROM {1} WHERE {2} IN (SELECT id FROM #{{0}})", columns, joins, aggregateId);

      foreach (var pi in type.AfxPersistentProperties(true, true).Where(pi1 => pi1.PropertyType.AfxIsAfxType() && !pi1.DeclaringType.AfxIsBaseType()))
      {
        var associativeCollection = pi.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>));
        if (associativeCollection != null)
        {
          Type associativeType = associativeCollection.GetGenericArguments()[1];
          foreach (var s in associativeType.AfxSqlAggregateSelects(selectionType)) yield return s;
        }
        else
        {
          var collectionType = pi.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>));
          if (collectionType != null)
          {
            var itemType = collectionType.GetGenericArguments()[0];
            if (itemType != type) foreach (var s in itemType.AfxSqlAggregateSelects(selectionType)) yield return s;
          }
          else
          {
            //TODO: Check Associative Property
          }
        }
      }

      Type associativeType1 = type.GetGenericSubClass(typeof(AssociativeObject<,>));
      if (associativeType1 != null)
      {
        var referenceType = associativeType1.GetGenericArguments()[1];
        if (referenceType.AfxIsAggregateReferenceType())
        {
          columns = string.Join(", ", referenceType.AfxQueryColumns());
          joins = string.Join(" INNER JOIN ", referenceType.AfxBaseJoins().Union(new string[] { string.Format("{0} ON {1}={2}", type.AfxDbName(), referenceType.AfxDbName("id"), type.AfxDbName("Reference")) }).Union(type.AfxAggregateJoins()));

          if (selectionType == SelectionType.All) yield return string.Format("SELECT {0} FROM {1}", columns, joins);
          else if (selectionType == SelectionType.Id) yield return string.Format("SELECT {0} FROM {1} WHERE {2}=@id", columns, joins, aggregateId);
          else yield return string.Format("SELECT {0} FROM {1} WHERE {2} IN (SELECT id FROM #{{0}})", columns, joins, aggregateId);
        }
      }
    }

    #endregion

    #region Strings

    #region ColumnName()

    public static string ColumnName(this string fullColumnSpecification)
    {
      return fullColumnSpecification.Substring(fullColumnSpecification.LastIndexOf('.') + 1);
    }

    #endregion

    #endregion
  }
}
