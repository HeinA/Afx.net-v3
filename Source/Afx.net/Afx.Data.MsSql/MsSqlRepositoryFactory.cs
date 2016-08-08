using Afx.Collections;
using Afx.Data;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  [Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(RepositoryFactory))]
  public class MsSqlRepositoryFactory : RepositoryFactory
  {
    #region Build()

    public override void Build(bool debug, bool inMemory)
    {
      mAssemblies = new Collection<Assembly>();
      foreach (var type in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder())
      {
        mAssemblies.Add(Build(debug, inMemory, new ObjectDataConverterBuilder(type)));
        if (type.AfxIsAggregateObject())
        {
          mAssemblies.Add(Build(debug, inMemory, new AggregateObjectRespositoryBuilder(type)));
        }
        if (type.AfxIsAggregateCollection())
        {
          mAssemblies.Add(Build(debug, inMemory, new AggregateCollectionRespositoryBuilder(type)));
        }
      }
    }

    Assembly Build(bool debug, bool inMemory, BuilderBase builder)
    {
      CSharpCodeProvider cs = new CSharpCodeProvider();
      CompilerParameters cp = new CompilerParameters();

      cp.ReferencedAssemblies.Add(this.GetType().Assembly.GetName().Name + ".dll");
      cp.ReferencedAssemblies.Add("System.Reflection.dll");
      cp.ReferencedAssemblies.Add("System.Runtime.dll");
      cp.ReferencedAssemblies.Add("System.ObjectModel.dll");

      foreach (var an in this.GetType().Assembly.GetReferencedAssemblies())
      {
        cp.ReferencedAssemblies.Add(an.Name + ".dll");
      }

      foreach (var ta in Afx.ExtensibilityManager.BusinessObjectTypes.PersistentTypesInDependecyOrder().DistinctBy(ti => ti.Assembly).Select(ti => ti.Assembly))
      {
        cp.ReferencedAssemblies.Add(ta.GetName().Name + ".dll");
      }

      CompilerResults results = null;
      cp.GenerateInMemory = inMemory;
      cp.IncludeDebugInformation = debug;
      if (!inMemory)
      {
        DirectoryInfo di = Directory.CreateDirectory(string.Format(@".\{0}", ConnectionTypeName));
        cp.OutputAssembly = builder.GetFileName(di.FullName, "dll");
        if (debug)
        {
          string csFilename = builder.GetFileName(di.FullName, "cs");
          File.WriteAllText(csFilename, builder.ToString());
          results = cs.CompileAssemblyFromFile(cp, csFilename);
        }
        else
        {
          results = cs.CompileAssemblyFromSource(cp, builder.ToString());
        }
      }
      else
      {
        results = cs.CompileAssemblyFromSource(cp, builder.ToString());
      }

      if (results.Errors.Count != 0)
      {
        string codeText = builder.ToString();
        throw new InvalidOperationException(Properties.Resources.RepositoryCompilationErrors);
      }

      return results.CompiledAssembly;
    }

    #endregion

    #region GetObjectRespository()

    Dictionary<Type, AggregateObjectRepository> mAggregateObjectRepositoryDictionary;
    public AggregateObjectRepository GetObjectRespository(Type target)
    {
      if (mAggregateObjectRepositoryDictionary == null)
      {
        mAggregateObjectRepositoryDictionary = new Dictionary<Type, AggregateObjectRepository>();
        foreach (var a in Assemblies)
        {
          foreach (var t in a.GetTypes())
          {
            if (typeof(AggregateObjectRepository).IsAssignableFrom(t))
            {
              AggregateObjectRepository aor = (AggregateObjectRepository)Activator.CreateInstance(t);
              mAggregateObjectRepositoryDictionary.Add(aor.TargetType, aor);
            }
          }
        }
      }

      try
      {
        return mAggregateObjectRepositoryDictionary[target];
      }
      catch
      {
        throw new InvalidOperationException(string.Format(Properties.Resources.AggregateObjectRepositoryNotFound, target));
      }
    }

    public override AggregateObjectRepository<T> GetObjectRepository<T>()
    {
      return (AggregateObjectRepository<T>)GetObjectRespository(typeof(T));
    }


    #endregion

    #region GetCollectionRespository()

    Dictionary<Type, AggregateCollectionRepository> mAggregateCollectionRepositoryDictionary;
    public AggregateCollectionRepository GetCollectionRespository(Type target)
    {
      if (mAggregateCollectionRepositoryDictionary == null)
      {
        mAggregateCollectionRepositoryDictionary = new Dictionary<Type, AggregateCollectionRepository>();
        foreach (var a in Assemblies)
        {
          foreach (var t in a.GetTypes())
          {
            if (typeof(AggregateCollectionRepository).IsAssignableFrom(t))
            {
              AggregateCollectionRepository acr = (AggregateCollectionRepository)Activator.CreateInstance(t);
              mAggregateCollectionRepositoryDictionary.Add(acr.TargetType, acr);
            }
          }
        }
      }

      try
      {
        return mAggregateCollectionRepositoryDictionary[target];
      }
      catch
      {
        throw new InvalidOperationException(string.Format(Properties.Resources.AggregateCollectionRepositoryNotFound, target));
      }
    }

    public override AggregateCollectionRepository<T> GetCollectionRepository<T>()
    {
      return (AggregateCollectionRepository<T>)GetCollectionRespository(typeof(T));
    }

    #endregion

    #region GetObjectDataConverter

    Dictionary<Type, ObjectDataConverter> mObjectDataConverterDictionary;
    public override ObjectDataConverter GetObjectDataConverter(Type target)
    {
      if (mObjectDataConverterDictionary == null)
      {
        mObjectDataConverterDictionary = new Dictionary<Type, ObjectDataConverter>();
        foreach (var a in Assemblies)
        {
          foreach (var t in a.GetTypes())
          {
            if (typeof(ObjectDataConverter).IsAssignableFrom(t))
            {
              ObjectDataConverter acr = (ObjectDataConverter)Activator.CreateInstance(t);
              mObjectDataConverterDictionary.Add(acr.TargetType, acr);
            }
          }
        }
      }

      try
      {
        return mObjectDataConverterDictionary[target];
      }
      catch
      {
        throw new InvalidOperationException(string.Format(Properties.Resources.ObjectDataConverterNotFound, target));
      }
    }

    #endregion


    #region IEnumerable<Assembly> Assemblies

    Collection<Assembly> mAssemblies;
    IEnumerable<Assembly> Assemblies
    {
      get
      {
        if (mAssemblies == null)
        {
          DirectoryInfo di = new DirectoryInfo(string.Format(@".\{0}", ConnectionTypeName));
          foreach (var fi in di.GetFiles("*.dll"))
          {
            mAssemblies.Add(Assembly.LoadFile(fi.FullName));
          }
        }
        return mAssemblies;
      }
    }

    #endregion

    #region string ConnectionTypeName

    string ConnectionTypeName
    {
      get
      {
        var connectionProvider = Afx.ExtensibilityManager.GetObject<IConnectionProvider>(DataScope.CurrentScopeName);
        return connectionProvider.GetConnection().GetType().FullName;
      }
    }

    #endregion


    #region class BuilderBase

    abstract class BuilderBase : IDisposable
    {
      protected BuilderBase(Type targetType)
      {
        TargetType = targetType;
        StringWriter = new StringWriter();
        IndentedTextWriter = new IndentedTextWriter(StringWriter, "  ");
      }

      public abstract string GetFileName(string folder, string extension);

      public Type TargetType { get; private set; }
      protected StringWriter StringWriter { get; private set; }
      protected IndentedTextWriter IndentedTextWriter { get; private set; }

      public void Dispose()
      {
        IndentedTextWriter.Dispose();
        StringWriter.Dispose();
      }

      protected void Write(string value, params object[] args)
      {
        IndentedTextWriter.Write(value, args);
      }

      protected void WriteLine(string value, params object[] args)
      {
        IndentedTextWriter.WriteLine(value, args);
      }

      protected int Indent
      {
        get { return IndentedTextWriter.Indent; }
        set { IndentedTextWriter.Indent = value; }
      }

      protected void WriteLine()
      {
        IndentedTextWriter.WriteLine();
      }

      protected void StartScope()
      {
        IndentedTextWriter.WriteLine("{");
        IndentedTextWriter.Indent++;
      }

      protected void EndScope()
      {
        IndentedTextWriter.Indent--;
        IndentedTextWriter.WriteLine("}");
      }

      protected void StartNamespace()
      {
        WriteLine("using Afx.Data;");
        WriteLine("using Afx.Data.MsSql;");
        WriteLine("using System.Linq;");
        WriteLine();
        WriteLine("namespace Afx.Data.MsSql.Generated");
        StartScope();
      }

      public override string ToString()
      {
        return StringWriter.ToString();
      }
    }

    #endregion

    #region class AggregateObjectRespositoryBuilder

    class AggregateObjectRespositoryBuilder : BuilderBase
    {
      public AggregateObjectRespositoryBuilder(Type targetType)
        : base(targetType)
      {
        StartNamespace();
        WriteLine("public class {0}AggregateObjectRepository : MsSqlAggregateObjectRepository<{1}>", TargetType.Name, TargetType.FullName);
        StartScope();

        #region AggregateObjectQuery

        WriteLine("public override AggregateObjectQuery<{0}> Query(string conditions)", TargetType.FullName);
        StartScope();
        WriteLine("return new MsSqlAggregateObjectQuery<{0}>(this, conditions);", TargetType.FullName);
        EndScope();
        WriteLine();

        #endregion

        #region AggregateSelectsForObject

        WriteLine("protected override string AggregateSelectsForObject");
        StartScope();
        WriteLine("get {{ return \"{0}\"; }}", string.Join("; ", TargetType.AfxSqlAggregateSelects(SelectionType.Id)));
        EndScope();
        WriteLine();

        #endregion

        #region AggregateSelectsForQuery

        WriteLine("protected override string AggregateSelectsForQuery");
        StartScope();
        WriteLine("get {{ return \"{0}\"; }}", string.Join("; ", TargetType.AfxSqlAggregateSelects(SelectionType.Query)));
        EndScope();
        WriteLine();

        #endregion

        #region GetObjects

        WriteLine("protected override System.Collections.Generic.IEnumerable<{0}> GetObjects(ObjectDataRowCollection rows)", TargetType.FullName);
        StartScope();
        WriteLine("foreach (var row in rows.Where(r => typeof({0}).IsAssignableFrom(r.Type))", TargetType.FullName);
        //Order By
        WriteLine(")");
        StartScope();
        WriteLine("if (row.Instance == null) GetObjectDataConverter(row.Type).WriteObject(row, rows);");
        WriteLine("yield return ({0})row.Instance;", TargetType.FullName);
        EndScope();
        EndScope();
        WriteLine();

        #endregion

        EndScope(); // class
        EndScope(); // namespace
      }

      public override string GetFileName(string folder, string extension)
      {
        return string.Format(@"{0}\{1}AggregateObjectRepository.{2}", folder, TargetType.Name, extension);
      }
    }

    #endregion

    #region class AggregateObjectRespositoryBuilder

    class AggregateCollectionRespositoryBuilder : BuilderBase
    {
      public AggregateCollectionRespositoryBuilder(Type targetType)
        : base(targetType)
      {
        StartNamespace();
        WriteLine("public class {0}AggregateCollectionRepository : MsSqlAggregateCollectionRepository<{1}>", TargetType.Name, TargetType.FullName);
        StartScope();

        #region AggregateSelectsForObjects

        WriteLine("protected override string AggregateSelectsForObjects");
        StartScope();
        WriteLine("get {{ return \"{0}\"; }}", string.Join("; ", TargetType.AfxSqlAggregateSelects(SelectionType.All)));
        EndScope();
        WriteLine();

        #endregion

        #region GetObjects

        WriteLine("protected override System.Collections.Generic.IEnumerable<{0}> GetObjects(ObjectDataRowCollection rows)", TargetType.FullName);
        StartScope();
        Write("foreach (var row in rows.Where(r => typeof({0}).IsAssignableFrom(r.Type))", TargetType.FullName);
        //Order By
        WriteLine(")");
        StartScope();
        WriteLine("if (row.Instance == null) GetObjectDataConverter(row.Type).WriteObject(row, rows);");
        WriteLine("yield return ({0})row.Instance;", TargetType.FullName);
        EndScope();
        EndScope();
        WriteLine();

        #endregion

        EndScope(); // class
        EndScope(); // namespace
      }

      public override string GetFileName(string folder, string extension)
      {
        return string.Format(@"{0}\{1}AggregateCollectionRepository.{2}", folder, TargetType.Name, extension);
      }
    }

    #endregion

    #region ObjectDataConverterBuilder

    class ObjectDataConverterBuilder : BuilderBase
    {
      public ObjectDataConverterBuilder(Type targetType)
        : base(targetType)
      {
        StartNamespace();
        WriteLine("public class {0}DataConverter : MsSqlObjectDataConverter<{1}>", TargetType.Name, TargetType.FullName);
        StartScope();

        #region WriteObject

        WriteLine("protected override void WriteObject({0} target, ObjectDataRow source, ObjectDataRowCollection context)", TargetType.FullName);
        StartScope();

        #region Base Class

        if (!TargetType.Equals(TargetType.AfxImplementationBaseType()))
        {
          WriteLine("GetObjectDataConverter<{0}>().WriteObject(source, context);", TargetType.BaseType.FullName);
          WriteLine();
        }

        #endregion

        #region Properties

        foreach (var pi in TargetType.AfxPersistentProperties(false, false).Where(pi1 => pi1.DeclaringType.Equals(TargetType)))
        {
          if (!typeof(IAfxObject).IsAssignableFrom(pi.PropertyType))
          {
            WriteLine("if (source.DataRow[\"{0}\"] != System.DBNull.Value) target.{0} = ({1})source.DataRow[\"{0}\"];", pi.Name, pi.PropertyType.FullName);
          }
          else
          {
            WriteLine("// Load from context or cache");
          }
        }

        if (TargetType.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
        {
          Type referenceType = TargetType.GetGenericSubClass(typeof(AssociativeObject<,>)).GetGenericArguments()[1];
          if (referenceType.GetCustomAttribute<AggregateReferenceAttribute>() != null)
          {
            WriteLine("var referenceRow = context.FirstOrDefault(r => r.Id.Equals((System.Guid)source.DataRow[\"Reference\"]));");
            WriteLine("if (referenceRow.Instance == null) GetObjectDataConverter(referenceRow.Type).WriteObject(referenceRow, context);");
            WriteLine("target.Reference = ({0})referenceRow.Instance;", referenceType.FullName);
          }
          else
          {
            WriteLine("// Load from cache");
          }
        }

        #endregion

        #region Object Collections

        foreach (var pi in TargetType.AfxPersistentProperties(false, true).Where(pi1 => pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null && pi1.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>)) == null))
        {
          Type itemType = pi.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)).GetGenericArguments()[0];
          Write("foreach (var item in context.GetOwnedObjects(target.Id).Where(r => typeof({0}).IsAssignableFrom(r.Type))", itemType.FullName);
          //Order by
          WriteLine(")");
          StartScope();
          WriteLine("if (item.Instance == null) GetObjectDataConverter(item.Type).WriteObject(item, context);");
          WriteLine("target.{1}.Add(({0})item.Instance);", itemType.FullName, pi.Name);
          EndScope();
        }

        #endregion

        #region Associative Collections

        foreach (var pi in TargetType.AfxPersistentProperties(false, true).Where(pi1 => pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null && pi1.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>)) != null))
        {
          Type itemType = pi.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>)).GetGenericArguments()[1];
          WriteLine("System.Collections.Generic.List<{0}> list{1} = new System.Collections.Generic.List<{0}>();", itemType.FullName, itemType.Name);
          WriteLine("foreach (var item in context.GetOwnedObjects(target.Id).Where(r => typeof({0}).IsAssignableFrom(r.Type)))", itemType.FullName);
          StartScope();
          WriteLine("if (item.Instance == null) GetObjectDataConverter(item.Type).WriteObject(item, context);");
          WriteLine("list{1}.Add(({0})item.Instance);", itemType.FullName, itemType.Name);
          EndScope();
          WriteLine("System.Collections.IDictionary dict = target.{0};", pi.Name);
          Write("foreach (var item in list{0}", itemType.Name);
          //Order by
          WriteLine(") dict.Add(item.Reference, item);");
        }

        #endregion

        EndScope();
        WriteLine();

        #endregion

        #region WriteDatabase

        WriteLine("protected override DatabaseWriteType WriteDatabase({0} source)", TargetType.FullName);
        StartScope();
        if (TargetType.Equals(TargetType.AfxImplementationBaseType()))
        {
          WriteLine("DatabaseWriteType writeType = GetWriteType(source);");
        }
        else
        {
          WriteLine("DatabaseWriteType writeType = GetObjectDataConverter<{0}>().WriteDatabase(source);", TargetType.BaseType.FullName);
        }

        WriteLine("switch (writeType)");
        StartScope();

        #region Insert

        IEnumerable<Tuple<string, string, string>> insertData = TargetType.AfxInsertData();
        WriteLine("case DatabaseWriteType.Insert:");
        Indent++;
        WriteLine("using (var cmd = GetCommand(\"INSERT INTO {0} ({1}) VALUES ({2})\"))", TargetType.AfxDbName(), string.Join(", ", insertData.Select(id => id.Item1)), string.Join(", ", insertData.Select(id => id.Item2)));
        StartScope();
        foreach (var t in insertData)
        {
          WriteLine("cmd.Parameters.AddWithValue(\"{0}\", {1});", t.Item2, t.Item3);
        }
        WriteLine("cmd.ExecuteNonQuery();");
        EndScope();
        WriteLine("break;");
        Indent--;

        #endregion

        #region Update

        IEnumerable<Tuple<string, string, string>> updateData = TargetType.AfxUpdateData();
        if (updateData.Count() > 0)
        {
          WriteLine();
          WriteLine("case DatabaseWriteType.Update:");
          Indent++;
          WriteLine("using (var cmd = GetCommand(\"UPDATE {0} SET {1} WHERE [id]=@id\"))", TargetType.AfxDbName(), string.Join(", ", updateData.Select(ud => string.Format("{0}={1}", ud.Item1, ud.Item2))));
          StartScope();
          foreach (var t in updateData)
          {
            WriteLine("cmd.Parameters.AddWithValue(\"{0}\", {1});", t.Item2, t.Item3);
          }
          WriteLine("cmd.Parameters.AddWithValue(\"@id\", source.Id);");
          WriteLine("cmd.ExecuteNonQuery();");
          EndScope();
          WriteLine("break;");
          Indent--;
        }

        #endregion

        EndScope(); //switch

        #region Object Collections

        foreach (var pi in TargetType.AfxPersistentProperties(false, true).Where(pi1 => pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null && pi1.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>)) == null))
        {
          WriteLine("foreach (var item in source.{0}) GetObjectDataConverter(item).WriteDatabase(item);", pi.Name);
          WriteLine("foreach (var item in source.{0}.DeletedItems) GetObjectDataConverter(item).DeleteDatabase(item);", pi.Name);
        }

        #endregion

        #region Associative Collections

        foreach (var pi in TargetType.AfxPersistentProperties(false, true).Where(pi1 => pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null && pi1.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>)) != null))
        {
          WriteLine("foreach (var item in source.{0})", pi.Name);
          StartScope();
          WriteLine("var associative = source.{0}[item];", pi.Name);
          WriteLine("GetObjectDataConverter(associative).WriteDatabase(associative);");
          EndScope();
          WriteLine("foreach (var item in source.{0}.DeletedItems) GetObjectDataConverter(item).DeleteDatabase(item);", pi.Name);
        }

        #endregion

        WriteLine("return writeType;");
        EndScope();

        #endregion

        EndScope(); // class
        EndScope(); // namespace
      }

      public override string GetFileName(string folder, string extension)
      {
        return string.Format(@"{0}\{1}DataConverter.{2}", folder, TargetType.Name, extension);
      }
    }

    #endregion
  }
}
