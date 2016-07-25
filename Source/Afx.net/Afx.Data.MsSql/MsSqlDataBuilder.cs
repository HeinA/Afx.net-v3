using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public class MsSqlDataBuilder : DataBuilder, IDataBuilder
  {
    static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    const string Owner = "Owner";
    const string Reference = "Reference";
    const string Id = "id";
    const string Ix = "ix";
    const string RegisteredType = "RegisteredType";

    public void ValidateDataStructure()
    {
      Guard.ThrowOperationExceptionIfNull(ConnectionScope.CurrentScope, Afx.Data.Properties.Resources.NoConnectionScope);

      try
      {
        Log.Info("Database Validation Starting");

        List<TypeInfo> types = IdentifyPesistentTypes();
        ValidateSchemas(types);
        ValidateRegisteredTypes(types);
        ValidateTableStubs(types);
        ValidateTableProperties(types);
        WriteDeleteTriggers(types);

        Log.Info("Database Validation Completed");
      }
      catch (Exception ex)
      {
        Log.Error("Database Validation Failed", ex);
        throw;
      }
    }

    #region GetTableInsteadOfDeleteTrigger()

    Dictionary<Type, StringWriter> mTableInsteadOfDeleteTriggers;
    StringWriter GetTableInsteadOfDeleteTrigger(Type type)
    {
      if (mTableInsteadOfDeleteTriggers == null) mTableInsteadOfDeleteTriggers = new Dictionary<Type, StringWriter>();
      if (!mTableInsteadOfDeleteTriggers.ContainsKey(type)) mTableInsteadOfDeleteTriggers.Add(type, new StringWriter());
      return mTableInsteadOfDeleteTriggers[type];
    }

    #endregion

    #region GetTableAfterDeleteTrigger()

    Dictionary<Type, StringWriter> mTableAfterDeleteTriggers;
    StringWriter GetTableAfterDeleteTrigger(Type type)
    {
      if (mTableAfterDeleteTriggers == null) mTableAfterDeleteTriggers = new Dictionary<Type, StringWriter>();
      if (!mTableAfterDeleteTriggers.ContainsKey(type)) mTableAfterDeleteTriggers.Add(type, new StringWriter());
      return mTableAfterDeleteTriggers[type];
    }

    #endregion

    #region IdentifyPesistentTypes()

    List<TypeInfo> IdentifyPesistentTypes()
    {
      List<TypeInfo> types = new List<TypeInfo>();

      Log.Info("Pesistent Type Identification Starting");
      foreach (var bot in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder())
      {
        Log.InfoFormat("{{{0}}}", bot.AfxTypeName());
        types.Add(bot);
      }
      Log.Info("Pesistent Type Identification Completed");

      return types;
    }

    #endregion

    #region GetSchema()

    public static string GetSchema(Type t)
    {
      return GetSchema(t.Assembly);
    }

    public static string GetSchema(Assembly a)
    {
      var sa = a.GetCustomAttribute<SchemaAttribute>();
      var schema = sa != null ? sa.SchemaName : "dbo";
      return schema.Replace(" ", string.Empty);
    }

    #endregion

    #region ValidateSchemas()

    void ValidateSchemas(List<TypeInfo> types)
    {
      Log.Info("Schema Validation Starting");

      List<string> schemas = new List<string>();
      schemas.Add("Afx");
      foreach (var a in types.Select(t => t.Assembly).Distinct())
      {
        var schema = GetSchema(a);
        if (!schemas.Contains(schema)) schemas.Add(schema);
      }

      foreach (var schema in schemas)
      {
        Log.InfoFormat("[{0}]", schema);
        string sqlSchema = string.Format("IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{0}') BEGIN EXEC('CREATE SCHEMA [{0}]') END", schema);
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sqlSchema))
        {
          cmd.ExecuteNonQuery();
        }
      }

      Log.Info("Schema Validation Completed");
    }

    #endregion

    #region ValidateRegisteredTypes()

    void ValidateRegisteredTypes(List<TypeInfo> types)
    {
      string sqlTableExists = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Afx' AND  TABLE_NAME = 'RegisteredType'";
      int count = 0;
      using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sqlTableExists))
      {
        count = (int)cmd.ExecuteScalar();
      }

      if (count == 0)
      {
        string sql = "CREATE TABLE [Afx].[RegisteredType] ([Id] [int] IDENTITY(1,1) NOT NULL, [FullName] [varchar](300) NOT NULL, CONSTRAINT [PK_CodeType] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY]";
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
        {
          cmd.ExecuteNonQuery();
        }
        sql = "CREATE UNIQUE INDEX [UIX_Afx_RegisteredType_FullName] ON [Afx].[RegisteredType](FullName)";
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
        {
          cmd.ExecuteNonQuery();
        }
      }

      foreach (var t in types.Where(t1 => !t1.IsAbstract))
      {
        string sql = string.Format("INSERT INTO [Afx].[RegisteredType] ([FullName]) SELECT '{0}' WHERE NOT EXISTS (SELECT 1 FROM [Afx].[RegisteredType] WHERE [FullName]='{0}')", t.AfxTypeName());
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
        {
          cmd.ExecuteNonQuery();
        }
      }
    }

    #endregion

    #region ValidateTableStubs()

    void ValidateTableStubs(List<TypeInfo> types)
    {
      Log.Info("Table Validation Starting");

      foreach (var t in types)
      {
        PropertyInfo piOwner = t.GetProperty(Owner, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

        string sqlTableExists = string.Format("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND  TABLE_NAME = '{1}'", GetSchema(t), t.Name);
        int count = 0;
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sqlTableExists))
        {
          count = (int)cmd.ExecuteScalar();
        }

        if (count == 0)
        {
          Log.InfoFormat("[{0}].[{1}]", GetSchema(t), t.Name);
          bool addType = false;
          if (t.BaseType.GetCustomAttribute<AfxBaseTypeAttribute>() != null) addType = true;

          string sql = string.Format("CREATE TABLE [{0}].[{1}] ([id] UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL CONSTRAINT [Default_{0}_{1}_id] DEFAULT NEWID(), [ix] INT NOT NULL IDENTITY(1,1){2}{3}) ON [PRIMARY]", GetSchema(t), t.Name, addType ? ", [RegisteredType] INT NOT NULL" : string.Empty, piOwner != null ? string.Format(", [Owner] uniqueidentifier {0}NULL", piOwner.PropertyType != t ? "NOT " : string.Empty) : string.Empty);
          using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
          {
            cmd.ExecuteNonQuery();
          }

          sql = string.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT [PK_{0}_{1}] PRIMARY KEY NONCLUSTERED (id)", GetSchema(t), t.Name);
          using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
          {
            cmd.ExecuteNonQuery();
          }

          if (piOwner != null)
          {
            sql = string.Format("CREATE UNIQUE CLUSTERED INDEX [CIX_{0}_{1}] ON [{0}].[{1}]([Owner], ix)", GetSchema(t), t.Name);
          }
          else
          {
            sql = string.Format("CREATE UNIQUE CLUSTERED INDEX [CIX_{0}_{1}] ON [{0}].[{1}](ix)", GetSchema(t), t.Name);
          }
          using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
          {
            cmd.ExecuteNonQuery();
          }

          sql = string.Format("EXEC sys.sp_addextendedproperty @name = N'Afx Managed', @value = N'Yes', @level0type = N'SCHEMA', @level0name = '{0}', @level1type = N'TABLE', @level1name = '{1}'", GetSchema(t), t.Name);
          using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
          {
            cmd.ExecuteNonQuery();
          }

          if (addType && GetRelationship(t, RegisteredType) == null)
          {
            string sqlCreateConstraint = string.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT FK_{1}_RegisteredType FOREIGN KEY ([RegisteredType]) REFERENCES [Afx].[RegisteredType]	([id])", GetSchema(t), t.Name);
            using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlCreateConstraint))
            {
              cmd.ExecuteNonQuery();
            }
          }

          if (types.Contains(t.BaseType))
          {
            ValidateRelationship(t, Id, t.BaseType, !t.Equals(t.BaseType));
          }
        }
      }

      Log.Info("Table Validation Completed");
    }

    #endregion

    #region ValidateTableProperties()

    void ValidateTableProperties(List<TypeInfo> types)
    {
      Log.Info("Property Validation Starting");

      foreach (var t in types)
      {
        List<PropertyInfo> processedProperties = new List<PropertyInfo>();
        DataSet ds = GetExistingTableColumns(t);

        #region Verify Framework Properties

        Type baseType = t.GetGenericSubClass(typeof(AfxObject<>));
        if (baseType == null) baseType = t.GetGenericSubClass(typeof(AssociativeObject<,>));
        PropertyInfo piOwner = null;
        PropertyInfo piReference = null;

        if (baseType != null && t.BaseType.Equals(baseType))
        {
          foreach (var pi in baseType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.GetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
          {
            bool isNullable = false;
            if (pi.Name == Owner)
            {
              Type afxBaseType = pi.PropertyType.GetGenericSubClass(typeof(AfxObject<>));
              if (afxBaseType != null && afxBaseType.GetGenericArguments()[0].Equals(t)) isNullable = true;
              piOwner = pi;
            }
            if (pi.Name == Reference) piReference = pi;
            if (!DoesColumnExist(ds, pi.AfxDbName(t))) CreateTableProperty(pi, t, isNullable);
            processedProperties.Add(pi);
          }
        }

        #endregion

        foreach (var pi in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.GetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
        {
          if (!DoesColumnExist(ds, pi.AfxDbName())) CreateTableProperty(pi, t, true);
          UpdateTableProperty(pi, t, pi.GetCustomAttribute<RequiredAttribute>() == null);
          processedProperties.Add(pi);
        }

        if (piOwner != null && piOwner.PropertyType.Equals(t)) ValidateCircularOwnerTrigger(t);
        if (piReference != null && t.GetCustomAttribute<OwnedReferenceAttribute>(true) != null) ValidateOwnedReferenceTriggers(t, piReference.PropertyType);

        #region Drop Old Columns

        foreach (DataRow dr in ds.Tables[0].Rows)
        {
          string columnName = (string)dr["COLUMN_NAME"];
          if (columnName != Id && columnName != Ix && columnName != RegisteredType && !processedProperties.Any(pi1 => pi1.Name.Equals(columnName)))
          {
            DropColumn(t, dr);
          }
        }

        #endregion
      }

      Log.Info("Property Validation Completed");
    }

    #region DropColumn()

    void DropColumn(Type type, DataRow drColumn)
    {
      try
      {
        DataRow dr1 = GetRelationship(type, (string)drColumn["COLUMN_NAME"]);
        if (dr1 != null)
        {
          Log.WarnFormat("Dropping Constraint [{0}]", dr1["ConstraintName"]);

          string sqlDrop = string.Format("ALTER TABLE [{0}].[{1}] DROP CONSTRAINT [{2}]", drColumn["TABLE_SCHEMA"], drColumn["TABLE_NAME"], dr1["ConstraintName"]);
          using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlDrop))
          {
            cmd.ExecuteNonQuery();
          }
        }

        Log.WarnFormat("Dropping Column [{0}].[{1}].[{2}]", drColumn["TABLE_SCHEMA"], drColumn["TABLE_NAME"], drColumn["COLUMN_NAME"]);

        string sqlAlter = string.Format("ALTER TABLE [{0}].[{1}] DROP COLUMN [{2}]", drColumn["TABLE_SCHEMA"], drColumn["TABLE_NAME"], drColumn["COLUMN_NAME"]);
        using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlAlter))
        {
          cmd.ExecuteNonQuery();
        }
      }
      catch
      {
        throw;
      }
    }

    #endregion

    #region DoesColumnExist()

    bool DoesColumnExist(DataSet ds, string fullColumnName)
    {
      foreach (DataRow dr in ds.Tables[0].Rows)
      {
        string id = (string)dr[Id];
        if (id.Equals(fullColumnName)) return true;
      }
      return false;
    }

    #endregion

    #region GetExistingTableColumns()

    DataSet GetExistingTableColumns(TypeInfo t)
    {
      string schemaName = GetSchema(t);
      string sqlColumns = string.Format("SELECT '[{0}].[{1}].[' + COLUMN_NAME + ']' as ID, TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, IS_NULLABLE, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_PRECISION_RADIX, NUMERIC_SCALE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=@ts AND TABLE_NAME=@tn", schemaName, t.Name);
      using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlColumns))
      {
        cmd.Parameters.AddWithValue("@ts", schemaName);
        cmd.Parameters.AddWithValue("@tn", t.Name);
        return ExecuteDataSet(cmd);
      }
    }

    #endregion

    #region GetDbDataType()

    string GetDbDataType(PropertyInfo pi, Type definingType)
    {
      IDbText dt = null;
      if (pi.PropertyType.IsSubclassOf(typeof(AfxObject)))
      {
        dt = Afx.ExtensibilityManager.GetObject<IDefaultDbText>(typeof(Guid).AfxTypeName());
        if (dt != null) return dt.DbText;
        else throw new CouldNotMapDataTypeException();
      }
      else
      {
        dt = Afx.ExtensibilityManager.GetObject<IDbText>(pi.AfxDbName(definingType));
        if (dt != null) return dt.DbText;
        else
        {
          string typeName = pi.PropertyType.AfxTypeName();
          dt = Afx.ExtensibilityManager.GetObject<IDbText>(typeName);
          if (dt != null) return dt.DbText;
          else
          {
            dt = Afx.ExtensibilityManager.GetObject<IDefaultDbText>(typeName);
            if (dt != null) return dt.DbText;
            else throw new CouldNotMapDataTypeException();
          }
        }
      }
    }

    #endregion

    #region CreateTableProperty()

    void CreateTableProperty(PropertyInfo pi, Type definingType, bool allowNull)
    {
      string dbTypeText = GetDbDataType(pi, definingType);
      Log.InfoFormat("{0}: {1}", pi.AfxDbName(definingType), dbTypeText);

      string schemaName = GetSchema(definingType);
      string sqlColumn = string.Format("ALTER TABLE [{0}].[{1}] ADD [{2}] {3}{4} NULL", schemaName, definingType.Name, pi.Name, dbTypeText, allowNull ? string.Empty : " NOT"); //TODO: Filestream
      using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlColumn))
      {
        cmd.ExecuteNonQuery();
      }

      if (pi.PropertyType.IsSubclassOf(typeof(AfxObject)))
      {
        ValidateRelationship(definingType, pi.Name, pi.PropertyType, pi.Name == Owner && !pi.PropertyType.Equals(definingType));
      }

      IColumnCreated cc = Afx.ExtensibilityManager.GetObject<IColumnCreated>(pi.AfxDbName());
      if (cc != null) cc.OnColumnCreated();
    }

    #endregion

    #region UpdateTableProperty()

    void UpdateTableProperty(PropertyInfo pi, Type definingType, bool allowNull)
    {
      string dbTypeText = GetDbDataType(pi, definingType);
      string schemaName = GetSchema(definingType);
      string sqlColumn = string.Format("ALTER TABLE [{0}].[{1}] ALTER COLUMN [{2}] {3}{4} NULL", schemaName, definingType.Name, pi.Name, dbTypeText, allowNull ? string.Empty : " NOT"); //TODO: Filestream
      using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlColumn))
      {
        cmd.ExecuteNonQuery();
      }
    }

    #endregion

    #endregion

    #region ValidateRelationship()

    void ValidateRelationship(Type ownerType, string propertyName, Type referencedType, bool cascadeDelete)
    {
      DataRow dr = GetRelationship(ownerType, propertyName);
      if (dr == null)
      {
        CreateRelationship(ownerType, propertyName, referencedType, cascadeDelete);
      }
    }

    #region GetRelationship()

    //DataRow GetRelationship(Type ownerType, string propertyName, Type referencedType)
    //{
    //  string sql = @" select	RC.CONSTRAINT_NAME as ConstraintName
    //                    ,		CCU1.TABLE_SCHEMA as OwnerSchema
    //                    ,		CCU1.TABLE_NAME as OwnerTable
    //                    ,		CCU1.COLUMN_NAME as OwnerColumn
    //                    ,		CCU2.TABLE_SCHEMA as ReferencedSchema
    //                    ,		CCU2.TABLE_NAME as ReferencedTable
    //                    ,		CCU2.COLUMN_NAME as ReferencedColumn
    //                    ,		RC.UPDATE_RULE as [Update]
    //                    ,		RC.DELETE_RULE as [Delete]
    //                    from INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
    //                    inner join INFORMATION_SCHEMA.CONSTRAINT_TABLE_USAGE CTU on CTU.CONSTRAINT_NAME=RC.CONSTRAINT_NAME
    //                    inner join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE CCU1 on RC.CONSTRAINT_NAME=CCU1.CONSTRAINT_NAME
    //                    inner join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE CCU2 on RC.UNIQUE_CONSTRAINT_NAME=CCU2.CONSTRAINT_NAME
    //                    where CCU1.TABLE_SCHEMA=@osn and CCU1.TABLE_NAME=@otn and CCU1.COLUMN_NAME=@ocn
    //                    and   CCU2.TABLE_SCHEMA=@rsn and CCU2.TABLE_NAME=@rtn and CCU2.COLUMN_NAME=@rcn";

    //  using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sql))
    //  {
    //    cmd.Parameters.AddWithValue("@osn", GetSchema(ownerType));
    //    cmd.Parameters.AddWithValue("@otn", ownerType.Name);
    //    cmd.Parameters.AddWithValue("@ocn", propertyName); // Id);
    //    cmd.Parameters.AddWithValue("@rsn", GetSchema(referencedType));
    //    cmd.Parameters.AddWithValue("@rtn", referencedType.Name);
    //    cmd.Parameters.AddWithValue("@rcn", Id);
    //    DataSet ds = ExecuteDataSet(cmd);
    //    if (ds.Tables[0].Rows.Count == 0) return null;
    //    return ds.Tables[0].Rows[0];
    //  }
    //}

    DataRow GetRelationship(Type ownerType, string propertyName)
    {
      string sql = @" select	RC.CONSTRAINT_NAME as ConstraintName
                        ,		CCU1.TABLE_SCHEMA as OwnerSchema
                        ,		CCU1.TABLE_NAME as OwnerTable
                        ,		CCU1.COLUMN_NAME as OwnerColumn
                        ,		CCU2.TABLE_SCHEMA as ReferencedSchema
                        ,		CCU2.TABLE_NAME as ReferencedTable
                        ,		CCU2.COLUMN_NAME as ReferencedColumn
                        ,		RC.UPDATE_RULE as [Update]
                        ,		RC.DELETE_RULE as [Delete]
                        from INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
                        inner join INFORMATION_SCHEMA.CONSTRAINT_TABLE_USAGE CTU on CTU.CONSTRAINT_NAME=RC.CONSTRAINT_NAME
                        inner join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE CCU1 on RC.CONSTRAINT_NAME=CCU1.CONSTRAINT_NAME
                        inner join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE CCU2 on RC.UNIQUE_CONSTRAINT_NAME=CCU2.CONSTRAINT_NAME
                        where CCU1.TABLE_SCHEMA=@osn and CCU1.TABLE_NAME=@otn and CCU1.COLUMN_NAME=@ocn";

      using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sql))
      {
        cmd.Parameters.AddWithValue("@osn", GetSchema(ownerType));
        cmd.Parameters.AddWithValue("@otn", ownerType.Name);
        cmd.Parameters.AddWithValue("@ocn", propertyName);
        DataSet ds = ExecuteDataSet(cmd);
        if (ds.Tables[0].Rows.Count == 0) return null;
        return ds.Tables[0].Rows[0];
      }
    }

    #endregion

    #region CreateRelationship()

    void CreateRelationship(Type ownerType, string propertyName, Type referencedType, bool cascadeDelete)
    {
      try
      {
        Log.InfoFormat("Creating Constraint for {0}.[{1}]", ownerType.AfxDbName(), propertyName);
        string sqlCreateConstraint = string.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT FK_{1}_{2} FOREIGN KEY ([{3}]) REFERENCES [{4}].[{2}]	([{5}]) ON UPDATE NO ACTION ON DELETE {6}", GetSchema(ownerType), ownerType.Name, referencedType.Name, propertyName, GetSchema(referencedType), Id, cascadeDelete ? "CASCADE" : "NO ACTION");
        using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlCreateConstraint))
        {
          cmd.ExecuteNonQuery();
        }

        //string sqlCreateIndex = string.Format("CREATE NONCLUSTERED INDEX IX_{0}_{2} ON [{1}].[{0}]([{2}])", ownerType.Name, GetSchema(ownerType), propertyName);
        //using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlCreateIndex))
        //{
        //  cmd.ExecuteNonQuery();
        //}
      }
      catch
      {
        throw;
      }
    }

    #endregion

    #endregion

    #region ValidateCircularOwnerTrigger()

    void ValidateCircularOwnerTrigger(Type type)
    {
      StringWriter sw = GetTableInsteadOfDeleteTrigger(type);
      sw.WriteLine();
      sw.WriteLine("\tDECLARE @CHILDREN as TABLE ([id] UNIQUEIDENTIFIER)");
      sw.WriteLine("\tINSERT INTO @CHILDREN");
      sw.WriteLine("\tSELECT [id] FROM DELETED");
      sw.WriteLine();
      sw.WriteLine("\tWHILE (@@ROWCOUNT <> 0)");
      sw.WriteLine("\tBEGIN");
      sw.WriteLine("\t\tINSERT INTO @CHILDREN");
      sw.WriteLine("\t\tSELECT [id] FROM {0}", type.AfxDbName());
      sw.WriteLine("\t\tWHERE [id] NOT IN (SELECT [id] FROM @CHILDREN)");
      sw.WriteLine("\t\tAND [Owner] IN (SELECT [id] FROM @CHILDREN)");
      sw.WriteLine("\tEND");
      sw.WriteLine();
      sw.WriteLine("\tDELETE FROM {0} WHERE [id] IN (SELECT [id] FROM @CHILDREN)", type.AfxDbName());
    }

    #endregion

    #region ValidateOwnedReferenceTriggers()

    void ValidateOwnedReferenceTriggers(Type type, Type targetType)
    {
      StringWriter sw = GetTableAfterDeleteTrigger(type);
      sw.WriteLine();
      sw.WriteLine("\tDELETE FROM {0} WHERE {0}.[id] IN (SELECT [Reference] FROM DELETED D WHERE NOT EXISTS (SELECT 1 FROM {1} O WHERE O.[Reference]=D.[Reference] AND O.[id]<>D.[id]))", targetType.AfxDbName(), type.AfxDbName());
      sw.WriteLine("\tDELETE FROM {0} WHERE {0}.[id] IN (SELECT [id] FROM DELETED)", type.AfxDbName());
    }

    #endregion

    #region WriteDeleteTriggers()

    const string InsteadOfDelete = "InsteadOfDelete";
    const string AfterDelete = "AfterDelete";

    void WriteDeleteTriggers(List<TypeInfo> types)
    {
      try
      {
        foreach (Type type in types)
        {
          DeleteTriggerIfExists(type, InsteadOfDelete);
          if (mTableInsteadOfDeleteTriggers == null || !mTableInsteadOfDeleteTriggers.ContainsKey(type)) continue;

          Log.InfoFormat("Writing Instead Of Delete Trigger for {0}", type.AfxDbName());

          StringWriter sw = mTableInsteadOfDeleteTriggers[type];
          using (StringWriter swOuter = new StringWriter())
          {
            swOuter.WriteLine("CREATE TRIGGER [{0}].[InsteadOfDelete_{1}] ON {2} INSTEAD OF DELETE", GetSchema(type), type.Name, type.AfxDbName());
            swOuter.WriteLine("AS");
            swOuter.WriteLine("BEGIN");
            swOuter.WriteLine("\tSET NOCOUNT ON");
            swOuter.WriteLine(sw);
            swOuter.WriteLine("END");

            string sql = swOuter.ToString();
            using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sql))
            {
              cmd.ExecuteNonQuery();
            }
            sw.Dispose();
          }
        }

        foreach (Type type in types)
        {
          DeleteTriggerIfExists(type, AfterDelete);
          if (mTableAfterDeleteTriggers == null || !mTableAfterDeleteTriggers.ContainsKey(type)) continue;

          Log.InfoFormat("Writing After Delete Trigger for {0}", type.AfxDbName());

          StringWriter sw = mTableAfterDeleteTriggers[type];
          using (StringWriter swOuter = new StringWriter())
          {
            swOuter.WriteLine("CREATE TRIGGER [{0}].[AfterDelete_{1}] ON {2} AFTER DELETE", GetSchema(type), type.Name, type.AfxDbName());
            swOuter.WriteLine("AS");
            swOuter.WriteLine("BEGIN");
            swOuter.WriteLine("\tSET NOCOUNT ON");
            swOuter.WriteLine(sw);
            swOuter.WriteLine("END");

            string sql = swOuter.ToString();
            using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sql))
            {
              cmd.ExecuteNonQuery();
            }
            sw.Dispose();
          }
        }
      }
      catch
      {
        throw;
      }
    }

    void DeleteTriggerIfExists(Type type, string startsWith)
    {
      string triggername = GetDeleteTrigger(type);
      if (triggername != null && triggername.StartsWith(startsWith))
      {
        string sql = string.Format("DROP TRIGGER [{0}].[{1}]", GetSchema(type), triggername);
        using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sql))
        {
          cmd.ExecuteNonQuery();
        }
      }
    }

    string GetDeleteTrigger(Type t)
    {
      string sql = string.Format("select O.name from sys.objects O inner join sys.trigger_events TE on O.object_id=TE.object_id inner join sys.objects OP on O.parent_object_id=OP.object_id inner join sys.schemas S on S.schema_id=OP.schema_id  where O.type = 'TR' and TE.type_desc='DELETE' and s.name='{0}' and op.name='{1}'", GetSchema(t), t.Name);
      using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sql))
      {
        return (string)cmd.ExecuteScalar();
      }
    }

    #endregion
  }
}
