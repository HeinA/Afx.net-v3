using Afx.Collections;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  [Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(IObjectRepositoryBuilder))]
  public class MsSqlObjectRepositoryBuilder : IObjectRepositoryBuilder
  {
    static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public IObjectRepository BuildRepository(Type type)
    {
      using (StringWriter sw = new StringWriter())
      using (IndentedTextWriter tw = new IndentedTextWriter(sw, "  "))
      {
        tw.WriteLine("using Afx.Data;\r\n");
        tw.WriteLine("namespace Afx.Data.MsSql.Generated\r\n{");
        tw.Indent++;
        tw.WriteLine("[System.ComponentModel.Composition.Export(typeof(ObjectRepository<{0}>))]", type.FullName);
        tw.WriteLine("public class MsSql{0}Repository : Afx.Data.MsSql.MsSqlObjectRepository<{1}>", type.Name, type.FullName);
        tw.WriteLine("{");
        tw.Indent++;

        tw.WriteLine("public override string Columns");
        tw.WriteLine("{");
        tw.Indent++;
        tw.Write("get { return \"");
        WriteReadColumns(tw, type);
        tw.WriteLine("\"; }");
        tw.Indent--;
        tw.WriteLine("}");

        tw.WriteLine();
        tw.WriteLine("public override string TableJoin");
        tw.WriteLine("{");
        tw.Indent++;
        tw.Write("get { return \"");
        WriteJoins(tw, type);
        tw.WriteLine("\"; }");
        tw.Indent--;
        tw.WriteLine("}");

        tw.WriteLine();
        tw.WriteLine("public override void FillObject({0} target, LoadContext context, System.Data.DataRow dr)", type.FullName);
        tw.WriteLine("{");
        tw.Indent++;
        WriteFillObject(tw, type);
        tw.Indent--;
        tw.WriteLine("}");

        tw.WriteLine();
        tw.WriteLine("protected override void SaveObjectCore({0} target, SaveContext context)", type.FullName);
        tw.WriteLine("{");
        tw.Indent++;
        WriteSaveObjectCore(tw, type);
        tw.Indent--;
        tw.WriteLine("}");

        tw.Indent--;
        tw.WriteLine("}");
        tw.Indent--;
        tw.WriteLine("}");

        string code = sw.ToString();

        CSharpCodeProvider cs = new CSharpCodeProvider();
        CompilerParameters cp = new CompilerParameters();

        cp.ReferencedAssemblies.Add(this.GetType().Assembly.GetName().Name + ".dll");
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

#if DEBUGGENERATED
        cp.GenerateInMemory = false;
        cp.IncludeDebugInformation = true;
        Directory.CreateDirectory(string.Format(@".\{0}", ConnectionScope.CurrentScope.ConnectionName));
        cp.OutputAssembly = string.Format(@".\{1}\{0}.Repository.dll", type.Name, ConnectionScope.CurrentScope.ConnectionName);
        string csFilename = string.Format(@".\{1}\{0}.Repository.cs", type.Name, ConnectionScope.CurrentScope.ConnectionName);
        File.WriteAllText(csFilename, code);
        CompilerResults results = cs.CompileAssemblyFromFile(cp, csFilename);
#else
        cp.GenerateInMemory = true;
        cp.IncludeDebugInformation = false;
        CompilerResults results = cs.CompileAssemblyFromSource(cp, code);
#endif


        if (results.Errors.Count != 0)
        {
          throw new InvalidOperationException();
        }

        var a = results.CompiledAssembly;
        return (IObjectRepository)a.CreateInstance(string.Format("Afx.Data.MsSql.Generated.MsSql{0}Repository", type.Name));
      }
    }

    private void WriteSaveObjectCore(IndentedTextWriter tw, Type type)
    {
      Type afxImplementationRoot = type.GetAfxImplementationRoot();

      tw.WriteLine("if (context.ShouldProcess(target))");
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("if (IsNew(target.Id))");
      tw.WriteLine("{");
      tw.Indent++;
      if (afxImplementationRoot != type)
      {
        tw.WriteLine("RepositoryInterfaceFor<{0}>().SaveObjectCore(target, context);", type.BaseType.FullName);
        tw.WriteLine();
      }
      WriteInsert(tw, type);
      tw.Indent--;
      tw.WriteLine("}");
      WriteUpdate(tw, type);
      if (afxImplementationRoot == type)
      {
        tw.WriteLine("context.SavedObjects.Add(target);");
        tw.WriteLine();
      }
      tw.Indent--;
      tw.WriteLine("}");
      WriteSaveObjectCoreCollections(tw, type);
    }

    private void WriteSaveObjectCoreCollections(IndentedTextWriter tw, Type type)
    {
      foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.GetProperty).Where(pi1 => pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null))
      {
        Type collectionType = pi.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>));
        if (collectionType != null)
        {
          WriteSaveObjectCoreAssociativeCollection(tw, type, pi, collectionType);
        }
        else
        {
          collectionType = pi.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>));
          if (collectionType != null)
          {
            WriteSaveObjectCoreObjectCollection(tw, type, pi, collectionType);
          }
        }
      }
    }

    private void WriteSaveObjectCoreObjectCollection(IndentedTextWriter tw, Type type, PropertyInfo pi, Type collectionType)
    {
      Type itemType = collectionType.GetGenericArguments()[0];
      tw.WriteLine();
      tw.WriteLine("foreach (var item in target.{0})", pi.Name);
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("RepositoryFor<{0}>().SaveObject(item, context);", itemType.FullName);
      tw.Indent--;
      tw.WriteLine("}");

      tw.WriteLine("if (!context.Merge)");
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("foreach ({0} obj in target.{1}.DeletedItems)", itemType.FullName, pi.Name);
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("RepositoryFor<{0}>().DeleteObject(obj);", itemType.FullName);
      tw.WriteLine("context.DeletedObjects.Add(obj);");
      tw.Indent--;
      tw.WriteLine("}");
      tw.Indent--;
      tw.WriteLine("}");
    }

    private void WriteSaveObjectCoreAssociativeCollection(IndentedTextWriter tw, Type type, PropertyInfo pi, Type collectionType)
    {
      Type associativeType = collectionType.GetGenericArguments()[1];
      tw.WriteLine();
      tw.WriteLine("foreach (var item in target.{0})", pi.Name);
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("{0} ao = target.{1}[item];", associativeType.FullName, pi.Name);
      tw.WriteLine("RepositoryFor<{0}>().SaveObject(ao, context);", associativeType.FullName);
      tw.Indent--;
      tw.WriteLine("}");

      tw.WriteLine("if (!context.Merge)");
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("foreach ({0} obj in target.{1}.DeletedItems)", associativeType.FullName, pi.Name);
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("RepositoryFor<{0}>().DeleteObject(obj);", associativeType.FullName);
      tw.WriteLine("context.DeletedObjects.Add(obj);");
      tw.Indent--;
      tw.WriteLine("}");
      tw.Indent--;
      tw.WriteLine("}");
    }

    void WriteUpdate(IndentedTextWriter tw, Type type)
    {
      Type associativeType = type.GetGenericSubClass(typeof(AssociativeObject<,>));
      Type ownedType = type.GetGenericSubClass(typeof(AfxObject<>));

      if (GetWriteColumns(type, false).Count() > 0)
      {
        tw.WriteLine("else");
        tw.WriteLine("{");
        tw.Indent++;
        int count = 1;
        List<string> parameters = new List<string>();
        foreach (var parameter in GetWriteColumns(type, false))
        {
          if (parameter == "[Owner]") parameters.Add("[Owner]=@owner");
          else parameters.Add(string.Format("{0}=@P_{1}", parameter, count++));
        }
        count = 1;
        tw.WriteLine("string sql = \"UPDATE {0} SET {1} WHERE [id]=@id\";", type.AfxDbName(), string.Join(", ", parameters));
        tw.WriteLine("Log.Debug(sql);");
        tw.WriteLine();
        tw.WriteLine("using (System.Data.SqlClient.SqlCommand cmd = GetCommand(sql))");
        tw.WriteLine("{");
        tw.Indent++;
        WriteParameters(tw, type, false);
        tw.WriteLine("cmd.Parameters.AddWithValue(\"@id\", target.Id);");
        if (associativeType == null && ownedType != null)
        {
          tw.WriteLine("cmd.Parameters.AddWithValue(\"@owner\", target.Owner != null ? (object)target.Owner.Id : (object)System.DBNull.Value);");
        }
        tw.WriteLine("cmd.ExecuteNonQuery();");
        tw.Indent--;
        tw.WriteLine("}");
        tw.Indent--;
        tw.WriteLine("}");
      }
    }

    void WriteInsert(IndentedTextWriter tw, Type type)
    {
      Type associativeType = type.GetGenericSubClass(typeof(AssociativeObject<,>));
      Type ownedType = type.GetGenericSubClass(typeof(AfxObject<>));

      tw.Write("string sql = \"INSERT INTO {0} (", type.AfxDbName(), type.AfxDbName());
      tw.Write(string.Join(", ", GetWriteColumns(type, true)));
      tw.Write(") SELECT ");
      tw.Write(string.Join(", ", GetWriteValues(type, true)));
      tw.WriteLine(" FROM [Afx].[RegisteredType] [RT] WHERE [RT].[FullName]=@assemblyFullName\";");
      tw.WriteLine("Log.Debug(sql);");
      tw.WriteLine();
      tw.WriteLine("using (System.Data.SqlClient.SqlCommand cmd = GetCommand(sql))");
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("cmd.Parameters.AddWithValue(\"@id\", target.Id);");
      tw.WriteLine("cmd.Parameters.AddWithValue(\"@assemblyFullName\", target.GetType().AfxTypeName());");
      if (associativeType != null)
      {
        tw.WriteLine("cmd.Parameters.AddWithValue(\"@owner\", target.Owner != null ? (object)target.Owner.Id : (object)System.DBNull.Value);");
        tw.WriteLine("cmd.Parameters.AddWithValue(\"@reference\", target.Reference);");
      }
      else if (ownedType != null)
      {
        tw.WriteLine("cmd.Parameters.AddWithValue(\"@owner\", target.Owner != null ? (object)target.Owner.Id : (object)System.DBNull.Value);");
      }
      WriteParameters(tw, type, true);
      tw.WriteLine("cmd.ExecuteNonQuery();");
      tw.Indent--;
      tw.WriteLine("}");
    }

    void WriteParameters(IndentedTextWriter tw, Type type, bool forInsert)
    {
      Type afxImplementationRoot = type.GetAfxImplementationRoot();
      Type associativeType = type.GetGenericSubClass(typeof(AssociativeObject<,>));
      Type ownedType = type.GetGenericSubClass(typeof(AfxObject<>));
      int count = 1;
      if (type == afxImplementationRoot)
      {
        foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
        {
          //if (!forInsert && associativeType != null && (pi.Name == "Owner" || pi.Name == "Reference"))
          //{
          //  continue;
          //}
          WriteParameter(tw, pi, count);
        }
      }
      else
      {
        Type current = type;
        while (current != afxImplementationRoot)
        {
          foreach (var pi in current.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
          {
            WriteParameter(tw, pi, count);
          }
          current = current.BaseType;
        }
      }
    }

    void WriteParameter(IndentedTextWriter tw, PropertyInfo pi, int count)
    {
      if (pi.PropertyType == typeof(string))
      {
        if (pi.PropertyType.GetCustomAttribute<DefaultsToNullAttribute>() == null)
        {
          tw.WriteLine("cmd.Parameters.AddWithValue(\"@P_{0}\", target.{1});", count++, pi.Name);
        }
        else
        {
          tw.WriteLine("cmd.Parameters.AddWithValue(\"@P_{0}\", System.String.IsNullOrWhiteSpace(target.{1}) ? (object)System.DBNull.Value : target.{1});", count++, pi.Name);
        }
      }
      else
      {
        if (pi.PropertyType.IsValueType)
        {
          if (pi.PropertyType.GetCustomAttribute<DefaultsToNullAttribute>() == null)
          {
            tw.WriteLine("cmd.Parameters.AddWithValue(\"@P_{0}\", target.{1});", count++, pi.Name);
          }
          else
          {
            tw.WriteLine("cmd.Parameters.AddWithValue(\"@P_{0}\", target.{1} == default({2}) ? (object)System.DBNull.Value : target.{1});", count++, pi.Name, pi.PropertyType);
          }
        }
        else
        {
          tw.WriteLine("cmd.Parameters.AddWithValue(\"@P_{0}\", ((object)target.{1}) == null ? (object)System.DBNull.Value : target.{1});", count++, pi.Name);
        }
      }
    }

    IEnumerable<string> GetWriteColumns(Type type, bool forInsert)
    {
      Type afxImplementationRoot = type.GetAfxImplementationRoot();
      Type associativeType = type.GetGenericSubClass(typeof(AssociativeObject<,>));
      Type ownedType = type.GetGenericSubClass(typeof(AfxObject<>));
      if (type == afxImplementationRoot)
      {
        if (forInsert)
        {
          yield return "[id]";
          yield return "[RegisteredType]";
        }
        if (associativeType != null)
        {
          if (forInsert)
          {
            yield return "[Owner]";
            yield return "[Reference]";
          }
        }
        else if (ownedType != null)
        {
          yield return "[Owner]";
        }
        foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
        {
          //if (!forInsert && associativeType != null && (pi.Name == "Owner" || pi.Name == "Reference"))
          //{
          //  continue;
          //}
          yield return string.Format("[{0}]", pi.Name);
        }
      }
      else
      {
        Type current = type;
        while (current != afxImplementationRoot)
        {
          foreach (var pi in current.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
          {
            yield return string.Format("[{0}]", pi.Name);
          }
          current = current.BaseType;
        }
      }
    }

    IEnumerable<string> GetWriteValues(Type type, bool forInsert)
    {
      Type afxImplementationRoot = type.GetAfxImplementationRoot();
      Type associativeType = type.GetGenericSubClass(typeof(AssociativeObject<,>));
      Type ownedType = type.GetGenericSubClass(typeof(AfxObject<>));
      int count = 1;
      if (type == afxImplementationRoot)
      {
        if (forInsert)
        {
          yield return "@id";
          yield return "[RT].[id]";
        }
        if (associativeType != null)
        {
          if (forInsert)
          {
            yield return "@owner";
            yield return "@reference";
          }
        }
        else if (ownedType != null)
        {
          yield return "@owner";
        }
        foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
        {
          //if (!forInsert && associativeType != null && (pi.Name == "Owner" || pi.Name == "Reference"))
          //{
          //  continue;
          //}
          yield return string.Format("@P_{0}", count++);
        }
      }
      else
      {
        Type current = type;
        while (current != afxImplementationRoot)
        {
          foreach (var pi in current.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
          {
            yield return string.Format("@P_{0}", count++);
          }
          current = current.BaseType;
        }
      }
    }

    private void WriteReadColumns(IndentedTextWriter tw, Type type)
    {
      List<string> columns = new List<string>();
      Type afxImplementationRoot = type.GetAfxImplementationRoot();
      Type associativeType = type.GetGenericSubClass(typeof(AssociativeObject<,>));
      Type ownedType = type.GetGenericSubClass(typeof(AfxObject<>));

      if (type == afxImplementationRoot)
      {
        columns.Add(string.Format("{0}.[id]", type.AfxDbName()));
        columns.Add("[Afx].[RegisteredType].[FullName] as AssemblyFullName");
        if (ownedType != null) columns.Add(string.Format("{0}.[Owner]", type.AfxDbName()));
        if (associativeType != null) columns.Add(string.Format("{0}.[Reference]", type.AfxDbName()));
        foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
        {
          columns.Add(string.Format("{0}.[{1}]", type.AfxDbName(), pi.Name));
        }
      }
      else
      {
        Type current = type;
        while (current != afxImplementationRoot)
        {
          foreach (var pi in current.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
          {
            columns.Add(string.Format("{0}.[{1}]", type.AfxDbName(), pi.Name));
          }
          current = current.BaseType;
        }
      }
      tw.Write(string.Join(", ", columns));
    }

    private void WriteJoins(IndentedTextWriter tw, Type type)
    {
      Type afxImplementationRoot = type.GetAfxImplementationRoot();
      if (type == afxImplementationRoot)
      {
        tw.Write("{0} INNER JOIN [Afx].[RegisteredType] ON {0}.[RegisteredType]=[RegisteredType].[id]", type.AfxDbName());
      }
      else
      {
        List<string> joins = new List<string>();
        joins.Add(type.AfxDbName());
        Type current = type.BaseType;
        while (current != afxImplementationRoot)
        {
          current = current.BaseType;
          joins.Add(string.Format("{0} ON {0}.[id]={1}.[id]", current.AfxDbName(), type.AfxDbName()));
        }
        tw.Write(string.Join(" INNER JOIN ", joins));
      }
    }

    private void WriteFillObject(IndentedTextWriter tw, Type type)
    {
      List<string> columns = new List<string>();
      Type afxImplementationRoot = type.GetAfxImplementationRoot();
      Type afxRoot = afxImplementationRoot.BaseType;

      if (type == afxImplementationRoot)
      {
        tw.WriteLine("context.RegisterObject(target);");
        tw.WriteLine();
      }

      if (type != afxImplementationRoot && type.BaseType != afxImplementationRoot)
      {
        tw.WriteLine("SqlRepositoryFor<{0}>().FillObject(target, context, dr);", type.BaseType.FullName);
        tw.WriteLine();
      }

      if (type == afxImplementationRoot)
      {
        WriteFillObjectCollections(tw, type, type);
      }
      else
      {
        Type current = type;
        while (current != afxImplementationRoot)
        {
          WriteFillObjectCollections(tw, type, current);
          current = current.BaseType;
        }
      }

      if (type == afxImplementationRoot)
      {
        WriteFillObjectProperties(tw, type);
      }
      else
      {
        Type current = type;
        while (current != afxImplementationRoot)
        {
          WriteFillObjectProperties(tw, current);
          current = current.BaseType;
        }
      }

      if (type.GetGenericSubClass(typeof(AssociativeObject<,>)) != null)
      {
        var pi = afxRoot.GetProperty("Reference", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty);
        if (afxImplementationRoot.GetCustomAttribute<CompositeReferenceAttribute>() != null)
        {
          tw.WriteLine(string.Format("if (dr[\"Reference\"] != System.DBNull.Value) target.Reference = RepositoryFor<{0}>().LoadObject((System.Guid)dr[\"Reference\"]);", pi.PropertyType.FullName));
        }
        else
        {
          //TODO: Load from cache if available
        }
      }
    }

    void WriteFillObjectProperties(IndentedTextWriter tw, Type current)
    {
      foreach (var pi in current.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetProperty).Where(pi1 => pi1.GetCustomAttribute<PersistentAttribute>() != null))
      {
        if (!typeof(IAfxObject).IsAssignableFrom(pi.PropertyType))
        {
          tw.WriteLine("if (dr[\"{0}\"] != System.DBNull.Value) target.{0} = ({1})dr[\"{0}\"];", pi.Name, pi.PropertyType.FullName);
        }
        else
        {
          //Is an Afx Object Reference

          //Check if object is in context (Previously loaded owned collection)

          //Otherwise, Load from cache if availabe
        }
      }
    }

    void WriteFillObjectCollections(IndentedTextWriter tw, Type type, Type current)
    {
      foreach (var pi in current.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.GetProperty).Where(pi1 => pi1.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>)) != null))
      {
        Type collectionType = pi.PropertyType.GetGenericSubClass(typeof(AssociativeCollection<,>));
        if (collectionType != null)
        {
          WriteFillObjectAssociativeCollection(tw, type, pi, collectionType);
        }
        else
        {
          collectionType = pi.PropertyType.GetGenericSubClass(typeof(ObjectCollection<>));
          if (collectionType != null)
          {
            WriteFillObjectObjectCollection(tw, type, pi, collectionType);
          }
        }
      }
    }

    void WriteFillObjectAssociativeCollection(IndentedTextWriter tw, Type type, PropertyInfo pi, Type collectionType)
    {
      Type associativeType = collectionType.GetGenericArguments()[1];
      tw.WriteLine("System.Collections.IDictionary items = target.{0};", pi.Name);
      tw.WriteLine("foreach (var obj in RepositoryFor<{0}>().LoadObjects(target.Id))", associativeType.FullName);
      tw.WriteLine("{");
      tw.Indent++;
      tw.WriteLine("items.Add(obj.Reference, obj);");
      tw.Indent--;
      tw.WriteLine("}");
      tw.WriteLine();
    }

    void WriteFillObjectObjectCollection(IndentedTextWriter tw, Type type, PropertyInfo pi, Type collectionType)
    {
      Type itemType = collectionType.GetGenericArguments()[0];
      Type itemOwnerType = itemType.GetAfxImplementationRoot().BaseType.GetGenericArguments()[0];

      if (itemOwnerType == type) //Recursive Ownership
      {
        tw.WriteLine("if (dr[\"Owner\"] != System.DBNull.Value)");
        tw.WriteLine("{");
        tw.Indent++;
        tw.WriteLine("System.Guid oid = (System.Guid)dr[\"Owner\"];");
        tw.WriteLine("{0} owner = ({0})context.GetObject(oid);", itemOwnerType.FullName);
        tw.WriteLine("owner.{0}.Add(target);", pi.Name);
        tw.Indent--;
        tw.WriteLine("}");
        tw.WriteLine();
      }
      else
      {
        tw.WriteLine("foreach (var obj in RepositoryFor<{0}>().LoadObjects(target.Id))", itemType.FullName);
        tw.WriteLine("{");
        tw.Indent++;
        tw.WriteLine("target.{0}.Add(obj);", pi.Name);
        tw.Indent--;
        tw.WriteLine("}");
        tw.WriteLine();
      }
    }
  }
}
