using Afx.Plugin.Commands;
using Afx.Plugin.Utilities;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Vsix.Utilities;
using VSLangProj;

namespace Afx.Plugin.Models
{
  public abstract class AfxProject
  {
    public Project Project { get; private set; }

    ObservableCollection<AfxObjectClass> mAfxClasses = new ObservableCollection<AfxObjectClass>();
    public ObservableCollection<AfxObjectClass> AfxObjectClasses
    {
      get { return mAfxClasses; }
    }

    ObservableCollection<AfxReference> mAfxReferences = new ObservableCollection<AfxReference>();
    public ObservableCollection<AfxReference> AfxReferences
    {
      get { return mAfxReferences; }
    }

    AfxObjectClassFolder mAfxObjectClassFolder;
    public AfxObjectClassFolder AfxObjectClassFolder
    {
      get { return mAfxObjectClassFolder ?? (mAfxObjectClassFolder = new AfxObjectClassFolder(this)); }
    }

    public abstract IEnumerable<AfxFolder> Folders { get; }

    protected AfxProject(Project project)
    {
      Project = project;

      var vsProject = project.Object as VSLangProj.VSProject;
      // note: you could also try casting to VsWebSite.VSWebSite

      foreach (Reference reference in vsProject.References)
      {
        ProcessReference(reference);
      }

      Refresh();
    }

    public static AfxProject CreateAfxProject(Project project)
    {
      string guids = VisualStudioHelper.GetProjectTypeGuids(project);
      //if (guids.Contains(ProjectFlavour.ServiceLibrary.ServiceLibraryProjectFactory.ServiceLibraryProjectFactoryGuidString)) ProjectType = AfxProjectType.Service;
      if (guids.Contains(ProjectFlavour.ClassLibrary.ClassLibraryProjectFactory.ClassLibraryProjectFactoryGuidString)) return new AfxProjectClassLibrary(project);
      if (guids.Contains(ProjectFlavour.SqlDataLibrary.SqlDataLibraryProjectFactory.SqlDataLibraryProjectFactoryGuidString)) return new AfxProjectSqlDataLibrary(project);
      return null;
    }

    public void Refresh()
    {
      AfxObjectClasses.Clear();
      ProcessProjectItems(Project.ProjectItems);
    }

    void ProcessProjectItems(ProjectItems items)
    {
      foreach (ProjectItem pi in items)
      {
        if (pi.Kind == "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}") //Physical File 
        {
          FileCodeModel fileCodeModel = pi.FileCodeModel;
          if (fileCodeModel != null)
          {
            ProcessFile(fileCodeModel);
          }
        }
        else
        {
          ProcessProjectItems(pi.ProjectItems);
        }
      }
    }

    public string Name
    {
      get { return Project.Name; }
    }

    public virtual void OnProjectRenamed()
    {
    }

    public void ProcessClass(CodeClass codeClass, FileCodeModel fileCodeModel)
    {
      if (codeClass.Name == "Constants")
      {
        foreach (CodeVariable ce in codeClass.Members.OfType< CodeVariable>())
        {
          string mn = ce.Name;
          object o = ce.InitExpression;
        }
      }

      AfxObjectClass afxObjectClass = AfxObjectClasses.FirstOrDefault(c => c.CodeClass.Equals(codeClass) && c.FileCodeModel.Equals(fileCodeModel));
      if (codeClass.IsDerivedFrom[TypeHelper.AfxObject] && !codeClass.FullName.Equals(TypeHelper.AfxObject))
      {
        if (afxObjectClass == null)
        {
          AfxObjectClasses.Add(new AfxObjectClass(this, fileCodeModel, codeClass));
        }
      }
      else
      {
        if (afxObjectClass != null)
        {
          AfxObjectClasses.Remove(afxObjectClass);
        }
        else
        {
          foreach (CodeElement ce in codeClass.ImplementedInterfaces)
          {
            CodeInterface ci = ce as CodeInterface;
            if (ci.FullName.StartsWith(TypeHelper.DataRepository))
            {
              string arguments = ci.FullName.Split('<', '>')[1];
            }
          }
        }
      }
    }

    public void ProcessFile(FileCodeModel fileCodeModel)
    {
      //Collection<CodeClass> processedClasses = new Collection<CodeClass>();

      foreach (CodeElement ce in fileCodeModel.CodeElements)
      {
        if (ce.Kind == vsCMElement.vsCMElementNamespace)
        {
          var cn = ce as CodeNamespace;
          foreach (CodeElement ce1 in cn.Members)
          {
            if (ce1.Kind == vsCMElement.vsCMElementClass)
            {
              CodeClass codeClass = ce1 as CodeClass;
              ProcessClass(codeClass, fileCodeModel);
              //processedClasses.Add(codeClass);
            }
          }
        }
      }
    }

    public void ProcessReference(Reference reference)
    {
      if (AfxReferences.Any(r => r.Reference.Equals(reference))) return;

      if (reference.SourceProject != null)
      {
        if (!TypeHelper.IsAfxProject(reference.SourceProject)) return;
        AfxReferences.Add(new AfxReference(this, reference));
      }
      else
      {
        Assembly assembly = Assembly.LoadFile(reference.Path);
        if (!TypeHelper.IsAfxAssembly(assembly)) return;
        AfxReferences.Add(new AfxReference(this, reference));
      }
    }

    public void RemoveReference(Reference reference)
    {
      var afxReference = AfxReferences.FirstOrDefault(r => r.Reference.Equals(reference));
      if (afxReference == null) return;

      AfxReferences.Remove(afxReference);
    }

    public ICommand ItemActivated
    {
      get { return new DelegateCommand<AfxProject>(OnItemActivated); }
    }

    private void OnItemActivated(AfxProject item)
    {
    }
  }
}
