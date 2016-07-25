using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public class MsSqlRepositoryBuilder : ObjectRepositoryBuilder, IObjectRepositoryBuilder
  {
    static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public void BuildRepositories()
    {
      Log.Info("Repository Validation Starting");

      List<TypeInfo> types = IdentifyPesistentTypes();
      BuildRepositories(types);

      Log.Info("Repository Validation Completed");
    }

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

    void BuildRepositories(List<TypeInfo> types)
    {
      foreach (var type in types)
      {
        BuildRepository(type);
      }
    }

    void BuildRepository(Type type)
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
        WriteColumns(tw, type);
        tw.WriteLine("\"; }");
        tw.Indent--;
        tw.WriteLine("}");
        tw.Indent--;
        tw.WriteLine("}");
        tw.Indent--;
        tw.WriteLine("}");

        string s = sw.ToString();
      }
    }

    private void WriteColumns(IndentedTextWriter tw, Type type)
    {
      List<string> columns = new List<string>();
      Type afxImplementationRoot = type.GetAfxImplementationRoot();
      if (type == afxImplementationRoot)
      {
        columns.Add(string.Format("{0}.[id]", type.AfxDbName()));
        columns.Add("[Afx].[RegisteredType].[FullName] as AssemblyFullName");
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
  }
}
