using Afx.Plugin.Utilities;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public sealed class AfxProjectSqlDataLibrary : AfxProjectDataLibrary
  {
    internal AfxProjectSqlDataLibrary(Project project)
      : base(project)
    {
    }

    public override bool ProcessClass(CodeClass codeClass, FileCodeModel fileCodeModel)
    {
      bool processed = base.ProcessClass(codeClass, fileCodeModel);
      if (processed) return true;

      if (!codeClass.Bases.OfType<CodeClass>().Any(ci => ci.FullName.StartsWith(ApplicationStructure.AfxObjectRepository))) return false;
      AfxType afxObjectRepositoryClass = AfxTypes.FirstOrDefault(c => c.CodeClass.Equals(codeClass) && c.FileCodeModel.Equals(fileCodeModel));
      if (afxObjectRepositoryClass == null) AfxTypes.Add(new AfxObjectRepository(this, fileCodeModel, codeClass));
      AfxObjectRepositoriesFolder.OnRefresh();
      return true;
    }

    const string RepositoryIncludes = "using Afx.Data;\r\nusing System;\r\nusing System.Collections.Generic;\r\nusing System.Linq;\r\nusing System.Text;\r\nusing System.Threading.Tasks;\r\n\r\n";
    const string NamespaceDefinition = "namespace {0}\r\n{{\r\n{1}\r\n}}\r\n";
    const string RepositoryClassDefinition = "public partial class {0}Repository : Afx.Data.ObjectRepository<{1}>\r\n{{\r\n}}";
    const string RepositoryGeneratedClassDefinition = "public partial class {0}Repository\r\n{{\r\n{1}\r\n}}";

    public override void ProcessAfxObject(AfxObject obj)
    {
      if (!(obj.IsPublic && obj.IsPersistent)) return;

      string targetFilename = string.Format("{0}{1}SqlRepository.cs", Directory, obj.Name);
      string targetGenerationFilename = string.Format("{0}{1}SqlRepository.Generated.cs", Directory, obj.Name);

      ProjectItem classFileItem = Project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi => pi.Kind.Equals("{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}") && pi.FileNames[0].Equals(targetFilename));
      if (classFileItem == null)
      {
        if (!System.IO.File.Exists(targetFilename))
        {
          System.IO.File.WriteAllText(targetFilename, string.Format("{0}{1}",
            RepositoryIncludes,
            string.Format(NamespaceDefinition,
              Namespace,
              string.Format(RepositoryClassDefinition,
                obj.Name,
                obj.FullName))));
        }
        classFileItem = Project.ProjectItems.AddFromFile(targetFilename);
      }

      ProjectItem test = Project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi => pi.Kind.Equals("{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}") && pi.FileNames[0].Equals(targetFilename));

      ProjectItem classGeneratedFileItem = classFileItem.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi => pi.Kind.Equals("{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}") && pi.FileNames[0].Equals(targetGenerationFilename));
      if (classGeneratedFileItem != null)
      {        
        if (classGeneratedFileItem.IsOpen) classGeneratedFileItem.Save();
        classGeneratedFileItem.Remove();
        classGeneratedFileItem = null;
      }
      System.IO.File.WriteAllText(targetGenerationFilename, string.Format("{0}{1}",
          RepositoryIncludes,
          string.Format(NamespaceDefinition,
            Namespace,
            string.Format(RepositoryGeneratedClassDefinition,
              obj.Name,
              GenerateDataMethods(obj)))));
      classGeneratedFileItem = classFileItem.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi => pi.Kind.Equals("{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}") && pi.FileNames[0].Equals(targetGenerationFilename));
      classGeneratedFileItem = classFileItem.ProjectItems.AddFromFile(targetGenerationFilename);

      AfxObjectRepository or = AfxTypes.OfType<AfxObjectRepository>().FirstOrDefault(or1 => or1.FileCodeModel.Equals(classFileItem.FileCodeModel));
      if (or == null)
      {
        foreach (CodeElement ce in classFileItem.FileCodeModel.CodeElements)
        {
          if (ce.Kind == vsCMElement.vsCMElementNamespace)
          {
            var cn = ce as CodeNamespace;
            foreach (CodeElement ce1 in cn.Members)
            {
              if (ce1.Kind == vsCMElement.vsCMElementClass)
              {
                CodeClass codeClass = ce1 as CodeClass;
                if (codeClass.Name == string.Format("{0}Repository", obj.Name))
                {
                  AfxTypes.Add(new AfxObjectRepository(this, classFileItem.FileCodeModel, codeClass));
                  AfxObjectRepositoriesFolder.OnRefresh();
                }
              }
            }
          }
        }
      }

      foreach (CodeElement ce in classFileItem.FileCodeModel.CodeElements)
      {
        Reformat(ce);
      }
      foreach (CodeElement ce in classGeneratedFileItem.FileCodeModel.CodeElements)
      {
        Reformat(ce);
      }

      classFileItem.Save();
      classGeneratedFileItem.Save();
    }

    string GenerateDataMethods(AfxObject obj)
    {
      return string.Format("{0}\r\n\r\n{1}", GenerateLoadMethod(obj), GenerateSaveMethod(obj));
    }

    const string LoadMethod = "public override {0} LoadObject(Guid id)\r\n{{\r\n{1}\r\n}}";
    string GenerateLoadMethod(AfxObject obj)
    {
      return string.Format(LoadMethod,
        obj.FullName,
        string.Format("return new {0}(id);",
          obj.FullName));
    }

    const string SaveMethod = "public override {0} SaveObject({0} obj)\r\n{{\r\n{1}\r\n}}";
    string GenerateSaveMethod(AfxObject obj)
    {
      return string.Format(SaveMethod,
        obj.FullName,
        "return obj;");
    }
  }
}
