using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  [Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(IDataBuilder))]
  public sealed class MsSqlDataBuilder : IDataBuilder
  {
    static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    const string Owner = "Owner";
    const string Reference = "Reference";
    const string Id = "id";
    const string Ix = "ix";
    const string RegisteredType = "RegisteredType";

    List<Type> mCreatedTables = new List<Type>();

    #region ValidateDataStructure()

    public void ValidateDataStructure()
    {
      Guard.ThrowOperationExceptionIfNull(ConnectionScope.CurrentScope, Afx.Data.Properties.Resources.NoConnectionScope);

      try
      {
        Log.Info("Validating Database");

        ValidateSchemas();
        ValidateRegisteredTypes();

        DropConstraints();

        ValidateTables();
        ValidateTableColumns();

        UpdateTables();
        UpdateTableColumns();

        WriteDeleteTriggers();

        DropTables();
        mCreatedTables.Clear();
      }
      catch (Exception ex)
      {
        Log.Error("Database Validation Failed", ex);
        throw;
      }
    }

    #endregion


    #region PersistentTypes

    IEnumerable<TypeInfo> PersistentTypes
    {
      get
      {
        IEnumerable<TypeInfo> types = Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder();
        if (types.Count() == 0) yield break;

        foreach (var bot in types)
        {
          yield return bot;
        }
      }
    }

    #endregion

    #region Schema

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

    void ValidateSchemas()
    {
      List<string> schemas = new List<string>();
      schemas.Add("Afx");
      foreach (var a in PersistentTypes.Select(t => t.Assembly).Distinct())
      {
        var schema = GetSchema(a);
        if (!schemas.Contains(schema)) schemas.Add(schema);
      }

      foreach (var schema in schemas)
      {
        string sqlSchema = string.Format("IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{0}') BEGIN EXEC('CREATE SCHEMA [{0}]') END", schema);
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sqlSchema))
        {
          cmd.ExecuteNonQuery();
        }
      }
    }

    #endregion

    #region Registered Types

    void ValidateRegisteredTypes()
    {
      string sqlTableExists = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Afx' AND  TABLE_NAME = 'RegisteredType'";
      int count = 0;
      using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sqlTableExists))
      {
        count = (int)cmd.ExecuteScalar();
      }

      if (count == 0)
      {
        string sql = "CREATE TABLE [Afx].[RegisteredType] ([Id] [int] IDENTITY(1,1) NOT NULL, [AssemblyFullName] [varchar](300) NOT NULL, [Schema] [varchar](300) NOT NULL, [TableName] [varchar](300) NOT NULL, CONSTRAINT [PK_RegisteredType] PRIMARY KEY CLUSTERED ([Id] ASC) ON [PRIMARY]) ON [PRIMARY]";
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
        {
          cmd.ExecuteNonQuery();
        }
        sql = "CREATE TABLE [Afx].[RegisteredConstraints] ([Id] [int] IDENTITY(1,1) NOT NULL, [RegisteredType] [int] NOT NULL, [ConstraintName] [varchar](300) NOT NULL, CONSTRAINT [PK_RegisteredConstraints] PRIMARY KEY CLUSTERED ([RegisteredType] ASC, [Id] ASC) ON [PRIMARY], CONSTRAINT FK_RegisteredConstraints_RegisteredType FOREIGN KEY ([RegisteredType]) REFERENCES [Afx].[RegisteredType] ([id]) ON DELETE CASCADE) ON [PRIMARY]";
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
        {
          cmd.ExecuteNonQuery();
        }
        sql = "CREATE UNIQUE INDEX [UIX_Afx_RegisteredType_AssemblyFullName] ON [Afx].[RegisteredType](AssemblyFullName)";
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
        {
          cmd.ExecuteNonQuery();
        }
      }

      foreach (var t in PersistentTypes)
      {
        string sql = "INSERT INTO [Afx].[RegisteredType] ([AssemblyFullName], [Schema], [TableName]) SELECT @tn, @sn, @tbl WHERE NOT EXISTS (SELECT 1 FROM [Afx].[RegisteredType] WHERE [AssemblyFullName]=@tn)";
        using (var cmd = GetCommand(sql))
        {
          cmd.Parameters.AddWithValue("@tn", t.AfxTypeName());
          cmd.Parameters.AddWithValue("@sn", GetSchema(t));
          cmd.Parameters.AddWithValue("@tbl", t.Name);
          cmd.ExecuteNonQuery();
        }
      }
    }

    #endregion

    #region Tables

    #region ValidateTables()

    void ValidateTables()
    {
      foreach (var t in PersistentTypes)
      {
        string sqlTableExists = string.Format("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND  TABLE_NAME = '{1}'", GetSchema(t), t.Name);
        int count = 0;
        using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sqlTableExists))
        {
          count = (int)cmd.ExecuteScalar();
        }

        if (count == 0)
        {
          Log.InfoFormat("Creating Table [{0}].[{1}]", GetSchema(t), t.Name);

          string sql = string.Format("CREATE TABLE [{0}].[{1}] ([id] UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL CONSTRAINT [Default_{0}_{1}_id] DEFAULT NEWID(), [ix] INT NOT NULL IDENTITY(1,1){2}) ON [PRIMARY]", GetSchema(t), t.Name, t.BaseType.GetCustomAttribute<AfxBaseTypeAttribute>() != null ? ", [RegisteredType] INT NOT NULL" : string.Empty);
          using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
          {
            cmd.ExecuteNonQuery();
          }

          sql = string.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT [PK_{0}_{1}] PRIMARY KEY NONCLUSTERED (id)", GetSchema(t), t.Name);
          using (IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand(sql))
          {
            cmd.ExecuteNonQuery();
          }

          mCreatedTables.Add(t);
        }
      }
    }

    #endregion

    #region UpdateTables()

    void UpdateTables()
    {
      foreach (var t in mCreatedTables)
      {
        ITableCreated tableCreated = Afx.ExtensibilityManager.GetObject<ITableCreated>(t.AfxDbName());
      }

      foreach (var t in PersistentTypes)
      {
        string sql = null;
        PropertyInfo piOwner = t.GetProperty(Owner, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

        if (piOwner != null)
        {
          sql = string.Format("CREATE UNIQUE CLUSTERED INDEX [CIX_{0}_{1}] ON [{0}].[{1}]([Owner], ix); INSERT INTO [Afx].[RegisteredConstraints] ([RegisteredType], [ConstraintName]) SELECT [id], 'CIX_{0}_{1}' FROM [Afx].[RegisteredType] WHERE [AssemblyFullName]=@fn", GetSchema(t), t.Name);
        }
        else
        {
          sql = string.Format("CREATE UNIQUE CLUSTERED INDEX [CIX_{0}_{1}] ON [{0}].[{1}](ix); INSERT INTO [Afx].[RegisteredConstraints] ([RegisteredType], [ConstraintName]) SELECT [id], 'CIX_{0}_{1}' FROM [Afx].[RegisteredType] WHERE [AssemblyFullName]=@fn", GetSchema(t), t.Name);
        }
        Log.InfoFormat("Creating Clustered Index for {0}", t.AfxDbName());
        using (var cmd = GetCommand(sql))
        {
          cmd.Parameters.AddWithValue("@fn", t.AfxTypeName());
          cmd.ExecuteNonQuery();
        }

        if (t.BaseType.GetCustomAttribute<AfxBaseTypeAttribute>() != null && GetConstraint(t, RegisteredType) == null)
        {
          string sqlCreateConstraint = string.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT FK_{1}_RegisteredType FOREIGN KEY ([RegisteredType]) REFERENCES [Afx].[RegisteredType]	([id])", GetSchema(t), t.Name);
          using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlCreateConstraint))
          {
            cmd.ExecuteNonQuery();
          }
        }

        if (PersistentTypes.Contains(t.BaseType))
        {
          ValidateContraint(t, Id, t.BaseType, !t.Equals(t.BaseType));
        }
      }
    }

    #endregion

    #region DropTables()

    void DropTables()
    {
      string sql = "SELECT * FROM [Afx].[RegisteredType]";
      using (var cmd = GetCommand(sql))
      {
        DataSet ds = ExecuteDataSet(cmd);
        foreach (DataRow dr in ds.Tables[0].Rows)
        {
          if (Type.GetType((string)dr["AssemblyFullName"]) == null)
          {
            Log.InfoFormat("Dropping Table [{0}].[{1}]", dr["Schema"], dr["TableName"]);
            sql = string.Format("DROP TABLE [{0}].[{1}]; DELETE FROM [Afx].[RegisteredType] WHERE [id]=@id", dr["Schema"], dr["TableName"]);
            using (var cmd1 = GetCommand(sql))
            {
              cmd1.Parameters.AddWithValue("@id", dr["id"]);
              cmd1.ExecuteNonQuery();
            }
          }
        }
      }
    }

    #endregion

    #endregion

    #region Table Columns

    #region ValidateTableColumns()

    void ValidateTableColumns()
    {
      foreach (var t in PersistentTypes)
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
            if (!DoesColumnExist(ds, pi.AfxDbName(t))) CreateTableColumn(pi, t, isNullable);
            processedProperties.Add(pi);
          }
        }

        #endregion

        foreach (var pi in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.GetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
        {
          if (!DoesColumnExist(ds, pi.AfxDbName())) CreateTableColumn(pi, t, true);
          processedProperties.Add(pi);
        }

        if (piOwner != null && piOwner.PropertyType.Equals(t)) ValidateCircularOwnerTrigger(t);
        if (piReference != null && piReference.PropertyType.GetCustomAttribute<AggregateReferenceAttribute>(true) != null) ValidateOwnedReferenceTriggers(t, piReference.PropertyType);

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
    }

    #endregion

    #region void UpdateTableColumns()

    void UpdateTableColumns()
    {
      foreach (var t in PersistentTypes)
      {
        if (t.AfxImplementationBaseType().Equals(t))
        {
          if (t.GetGenericSubClass(typeof(AfxObject<>)) != null || t.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
          {
            var pi = t.GetProperty(Owner, BindingFlags.Public | BindingFlags.Instance);
            if (pi != null)
            {
              UpdateTableColumn(pi, t, pi.PropertyType.Equals(t));
            }
          }

          if (t.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
          {
            var pi = t.GetProperty(Reference, BindingFlags.Public | BindingFlags.Instance);
            if (pi != null)
            {
              UpdateTableColumn(pi, t, false);
            }
          }
        }

        foreach (var pi in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.GetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
        {
          UpdateTableColumn(pi, t, pi.GetCustomAttribute<RequiredAttribute>() == null);
        }
      }
    }

    #endregion

    #region DropColumn()

    void DropColumn(Type type, DataRow drColumn)
    {
      try
      {
        DataRow dr1 = GetConstraint(type, (string)drColumn["COLUMN_NAME"]);
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

    #region CreateTableColumn()

    void CreateTableColumn(PropertyInfo pi, Type definingType, bool allowNull)
    {
      string dbTypeText = GetDbDataType(pi, definingType);
      Log.InfoFormat("Creating Column {0}: {1}", pi.AfxDbName(definingType), dbTypeText);

      string schemaName = GetSchema(definingType);
      string sqlColumn = string.Format("ALTER TABLE [{0}].[{1}] ADD [{2}] {3}{4} NULL", schemaName, definingType.Name, pi.Name, dbTypeText, allowNull ? string.Empty : " NOT"); //TODO: Filestream
      using (var cmd = GetCommand(sqlColumn))
      {
        cmd.ExecuteNonQuery();
      }

      IColumnCreated cc = Afx.ExtensibilityManager.GetObject<IColumnCreated>(pi.AfxDbName());
      if (cc != null) cc.OnColumnCreated();
    }

    #endregion

    #region UpdateTableColumn()

    void UpdateTableColumn(PropertyInfo pi, Type definingType, bool allowNull)
    {
      string dbTypeText = GetDbDataType(pi, definingType);
      string schemaName = GetSchema(definingType);
      string sqlColumn = string.Format("ALTER TABLE [{0}].[{1}] ALTER COLUMN [{2}] {3}{4} NULL", schemaName, definingType.Name, pi.Name, dbTypeText, allowNull ? string.Empty : " NOT"); //TODO: Filestream
      using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlColumn))
      {
        cmd.ExecuteNonQuery();
      }

      if (pi.PropertyType.IsSubclassOf(typeof(AfxObject)))
      {
        ValidateContraint(definingType, pi.Name, pi.PropertyType, pi.Name == Owner && !pi.PropertyType.Equals(definingType));
      }
    }

    #endregion

    #endregion

    #region Constraints 

    #region DropConstraints()

    void DropConstraints()
    {
      try
      {
        string sql = "SELECT [RC].[id], [RT].[Schema], [RT].[TableName], [RC].[ConstraintName] FROM [Afx].[RegisteredConstraints] [RC] INNER JOIN [Afx].[RegisteredType] [RT] on [RT].[id]=[RC].[RegisteredType]";
        using (var cmd = GetCommand(sql))
        {
          DataSet ds = ExecuteDataSet(cmd);
          foreach (DataRow dr in ds.Tables[0].Rows)
          {
            Log.InfoFormat("Dropping Constraint {0}", dr["ConstraintName"]);
            if (((string)dr["ConstraintName"]).StartsWith("CIX_")) sql = string.Format("DROP INDEX [{2}] ON [{0}].[{1}] WITH ( ONLINE = OFF ); DELETE FROM [Afx].[RegisteredConstraints] WHERE [id]=@id", dr["Schema"], dr["TableName"], dr["ConstraintName"]);
            else sql = string.Format("ALTER TABLE [{0}].[{1}] DROP CONSTRAINT [{2}]; DELETE FROM [Afx].[RegisteredConstraints] WHERE [id]=@id", dr["Schema"], dr["TableName"], dr["ConstraintName"]);
            using (var cmd1 = GetCommand(sql))
            {
              cmd1.Parameters.AddWithValue("@id", dr["id"]);
              cmd1.ExecuteNonQuery();
            }
          }
        }
      }
      catch
      {
        throw;
      }
    }

    #endregion

    #region ValidateContraint()

    void ValidateContraint(Type ownerType, string propertyName, Type referencedType, bool cascadeDelete)
    {
      DataRow dr = GetConstraint(ownerType, propertyName);
      if (dr == null)
      {
        CreateConstraint(ownerType, propertyName, referencedType, cascadeDelete);
      }
    }

    #endregion

    #region GetConstraint()

    DataRow GetConstraint(Type ownerType, string propertyName)
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

    #region CreateConstraint()

    void CreateConstraint(Type ownerType, string propertyName, Type referencedType, bool cascadeDelete)
    {
      try
      {
        Log.InfoFormat("Creating Constraint for {0}.[{1}]", ownerType.AfxDbName(), propertyName);
        string sqlCreateConstraint = string.Format("ALTER TABLE [{0}].[{1}] ADD CONSTRAINT [FK_{1}_{2}] FOREIGN KEY ([{3}]) REFERENCES [{4}].[{2}]	([{5}]) ON UPDATE NO ACTION ON DELETE {6}; INSERT INTO [Afx].[RegisteredConstraints] ([RegisteredType], [ConstraintName]) SELECT [id], 'FK_{1}_{2}' FROM [Afx].[RegisteredType] WHERE [AssemblyFullName]=@fn", GetSchema(ownerType), ownerType.Name, referencedType.Name, propertyName, GetSchema(referencedType), Id, cascadeDelete ? "CASCADE" : "NO ACTION");
        using (SqlCommand cmd = (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sqlCreateConstraint))
        {
          cmd.Parameters.AddWithValue("@fn", ownerType.AfxTypeName());
          cmd.ExecuteNonQuery();
        }
      }
      catch
      {
        throw;
      }
    }

    #endregion

    #endregion

    #region Triggers

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

    void WriteDeleteTriggers()
    {
      try
      {
        foreach (Type type in PersistentTypes)
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

        foreach (Type type in PersistentTypes)
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

    #endregion

    #region GetCommand()

    SqlCommand GetCommand(string sql)
    {
      return (SqlCommand)ConnectionScope.CurrentScope.GetCommand(sql);
    }
    SqlCommand GetCommand()
    {
      return (SqlCommand)GetCommand(string.Empty);
    }

    #endregion

    #region ExecuteDataSet()

    public static DataSet ExecuteDataSet(IDbCommand cmd)
    {
      System.Data.DataSet ds = new System.Data.DataSet();
      ds.EnforceConstraints = false;
      ds.Locale = CultureInfo.InvariantCulture;
      using (IDataReader r = cmd.ExecuteReader())
      {
        ds.Load(r, LoadOption.OverwriteChanges, string.Empty);
        r.Close();
      }

      return ds;
    }

    #endregion
  }
}
