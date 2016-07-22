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
  public abstract class AfxProject : IAfxAssembly
  {
    public Project Project { get; private set; }
    public string AssemblyId { get; private set; }
    public string Namespace { get; private set; }

    ObservableCollection<AfxType> mAfxTypes = new ObservableCollection<AfxType>();
    public ObservableCollection<AfxType> AfxTypes
    {
      get { return mAfxTypes; }
    }

    public string Path
    {
      get { return Project.FileName; }
    }

    public string Directory
    {
      get { return (string)Project.Properties.Item("FullPath").Value; }
    }

    ObservableCollection<AfxReference> mAfxReferences = new ObservableCollection<AfxReference>();
    public ObservableCollection<AfxReference> AfxReferences
    {
      get { return mAfxReferences; }
    }

    AfxFolderObject mAfxObjectsFolder;
    public AfxFolderObject AfxObjectsFolder
    {
      get { return mAfxObjectsFolder ?? (mAfxObjectsFolder = new AfxFolderObject(this)); }
    }

    public abstract IEnumerable<AfxFolder> Folders { get; }

    protected AfxProject(Project project)
    {
      Project = project;

      Namespace = (string)Project.Properties.Item("DefaultNamespace").Value;

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
      if (guids.Contains(ProjectFlavour.ClassLibrary.ProjectFactory.ClassLibraryProjectFactoryGuidString)) return new AfxProjectClassLibrary(project);
      if (guids.Contains(ProjectFlavour.SqlDataLibrary.ProjectFactory.SqlDataLibraryProjectFactoryGuidString)) return new AfxProjectSqlDataLibrary(project);
      return null;
    }

    public void Refresh()
    {
      AfxTypes.Clear();
      ProcessConstants(Project.ProjectItems);
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

    protected void ProcessConstants(ProjectItems items)
    {
      foreach (ProjectItem pi in items)
      {
        if (pi.Kind == "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}") //Physical File 
        {
          FileCodeModel fileCodeModel = pi.FileCodeModel;
          if (fileCodeModel != null && pi.FileNames[0].ToUpperInvariant().Contains("CONSTANTS.CS"))
          {
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
                    if (codeClass.Name == ApplicationStructure.ConstantsClass)
                    {
                      ProcessConstants(codeClass.Members.OfType<CodeVariable>());
                    }
                  }
                }
              }
            }
          }
        }
        else
        {
          ProcessConstants(pi.ProjectItems);
        }
      }
    }

    protected virtual void ProcessConstants(IEnumerable<CodeVariable> constants)
    {
      CodeVariable cv = constants.FirstOrDefault(cc => cc.Name == ApplicationStructure.AssemblyId);
      if (cv != null) this.AssemblyId = (string)cv.InitExpression;
    }

    public virtual bool ProcessClass(CodeClass codeClass, FileCodeModel fileCodeModel)
    {
      bool processed = false;

      if (codeClass.Name == ApplicationStructure.ConstantsClass)
      {
        ProcessConstants(codeClass.Members.OfType<CodeVariable>());
      }

      AfxType afxObjectClass = AfxTypes.FirstOrDefault(c => c.CodeClass.Equals(codeClass) && c.FileCodeModel.Equals(fileCodeModel));
      if (codeClass.IsDerivedFrom[ApplicationStructure.AfxObject] && !codeClass.FullName.Equals(ApplicationStructure.AfxObject))
      {
        if (afxObjectClass == null)
        {
          AfxTypes.Add((AfxType)new AfxObject(this, fileCodeModel, codeClass));
        }
        processed = true;
      }

      return processed;
    }

    public void ProcessFile(FileCodeModel fileCodeModel)
    {
      if (fileCodeModel == null) return;
      if (fileCodeModel.Parent.FileNames[0].ToUpperInvariant().Contains("GENERATED")) return;

      Collection<CodeClass> processed = new Collection<CodeClass>();
       
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
              if (ProcessClass(codeClass, fileCodeModel)) processed.Add(codeClass);
            }
          }
        }
      }

      var remove = AfxTypes.Where(t => t.IsProjectItem && t.FileCodeModel.Equals(fileCodeModel) && !processed.Contains(t.CodeClass)).ToArray();
      foreach (var t in remove)
      {
        t.OnRemove();
        AfxTypes.Remove(t);
      }

      foreach (AfxFolder folder in Folders)
      {
        folder.OnRefresh();
      }
    }

    public void ProcessReference(Reference reference)
    {
      AfxReference afxReference = AfxReferences.FirstOrDefault(r => r.Reference.Equals(reference));
      if (afxReference != null) return;

      if (reference.SourceProject != null)
      {
        if (!ApplicationStructure.IsAfxProject(reference.SourceProject)) return;
        AfxProject sourceProject = ApplicationStructure.GetAfxProject(reference.SourceProject);
        if (sourceProject == null) return;

        AfxReferences.Add(new AfxReference(this, sourceProject, reference));

        var vsProject = this.Project.Object as VSLangProj.VSProject;
        foreach (var reference1 in sourceProject.AfxReferences)
        {
          if (AfxReferences.Any(r => r.IsSameReference(reference1))) continue;

          if (reference1.IsProjectReference) vsProject.References.AddProject(((AfxProject)reference1.ReferencedAssembly).Project);
          else vsProject.References.Add(reference1.ReferencedAssembly.Path);
        }
      }
      else
      {
        AfxAssembly afxAssembly = ApplicationStructure.Instance.Assemblies.FirstOrDefault(a => a.Name.Equals(reference.Name));
        string path = reference.Path;
        if (!string.IsNullOrWhiteSpace(path))
        {
          if (afxAssembly == null)
          {
            Assembly assembly = Assembly.LoadFile(path);
            if (!ApplicationStructure.IsAfxAssembly(assembly)) return;
            afxAssembly = new AfxAssembly(assembly, path);
            ApplicationStructure.Instance.Assemblies.Add(afxAssembly);
            AfxReferences.Add(new AfxReference(this, afxAssembly, reference));
          }
        }
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

    protected void Reformat(CodeElement ce)
    {
      var objMovePt = ce.EndPoint.CreateEditPoint();
      var objEditPt = ce.StartPoint.CreateEditPoint();
      objEditPt.StartOfDocument();
      objMovePt.EndOfDocument();
      objMovePt.SmartFormat(objEditPt);
    }
  }
}
