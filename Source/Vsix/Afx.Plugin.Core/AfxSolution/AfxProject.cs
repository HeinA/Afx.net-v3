using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vsix.Utilities;

namespace Afx.Plugin.AfxSolution
{
  public abstract class AfxProject : Model
  {
    #region Constructors

    protected AfxProject(Project project)
    {
      Project = project;
      DefaultNamespace = (string)Project.Properties.Item("DefaultNamespace").Value;
    }

    #endregion

    #region Properties

    public Project Project { get; private set; }
    public string DefaultNamespace { get; private set; }
    public string Name
    {
      get { return Project.Name; }
    }
    public Guid AssemblyGuid
    {
      get { return Guid.Parse((string)Project.Properties.Item("AssemblyGuid").Value); }
    }
    public string FileName
    {
      get { return Project.FileName; }
    }
    public string FullPath
    {
      get { return (string)Project.Properties.Item("FullPath").Value; }
    }

    ObservableCollection<AfxBusinessClass> mBusinessClasses = new ObservableCollection<AfxBusinessClass>();
    public ObservableCollection<AfxBusinessClass> BusinessClasses
    {
      get { return mBusinessClasses; }
    }

    public virtual IEnumerable<AfxFolder> Folders
    {
      get
      {
        yield return new AfxFolder("Business Classes", BusinessClasses);
      }
    }

    #endregion

    #region Event Handlers

    protected virtual void OnAdded()
    {
      ProcessProjectItems(Project.ProjectItems, true);
    }

    protected virtual void OnRemoved()
    {
    }

    public virtual void OnRenamed()
    {
      OnPropertyChanged(nameof(Name));
    }

    public bool AddProjectItem(ProjectItem projectItem, bool isLoading)
    {
      string itemKind = projectItem.Kind;
      if (itemKind != ProjectItemKinds.PhysicalFile) return false;
      FileCodeModel fileCodeModel = projectItem.FileCodeModel;
      if (fileCodeModel == null) return false;
      ProcessFileCodeModel(fileCodeModel, isLoading, null);
      return true;
    }


    public void RenameProjectItem(ProjectItem projectItem, string oldName)
    {
      string itemKind = projectItem.Kind;
      if (itemKind != ProjectItemKinds.PhysicalFile) return;
      FileCodeModel fileCodeModel = projectItem.FileCodeModel;
      if (fileCodeModel == null) return;
      ProcessFileCodeModel(fileCodeModel, false, oldName);
    }

    public void RemoveProjectItem(ProjectItem projectItem)
    {
      string itemKind = projectItem.Kind;
      if (itemKind != ProjectItemKinds.PhysicalFile) return; //TODO: Check folders
      OnRemoveProjectItem(projectItem);
    }

    public void ProcessFileCodeModel(FileCodeModel fileCodeModel, bool isLoading, string oldName)
    {
      ProjectItem pi = fileCodeModel.Parent.Collection.Parent as ProjectItem;
      if (pi != null)
      {
        if (pi.Kind == ProjectItemKinds.PhysicalFile) return; // Only process if file is a top level file
      }

      OnProcessFileCodeModel(fileCodeModel, isLoading, oldName);
    }

    protected virtual void OnProcessFileCodeModel(FileCodeModel fileCodeModel, bool isLoading, string oldName)
    {
      AfxBusinessClass c = AfxBusinessClass.GetBusinessClass(fileCodeModel.Parent);
      if (c != null) c.Refresh();
      else
      {
        AfxSolution.EnqueueUnprocessed(this, fileCodeModel);
        AfxSolution.ProcessQueue();
      }
    }

    protected virtual void OnRemoveProjectItem(ProjectItem projectItem)
    {
      AfxBusinessClass c = AfxBusinessClass.GetBusinessClass(projectItem);
      if (c != null) c.Remove();
    }

    #endregion


    #region Statics

    #region GetProject()

    public static AfxProject GetProject(Project project)
    {
      AfxProject afxProject = AfxSolution.Instance.AfxClassLibraryProjects.FirstOrDefault(p => p.Project.Equals(project));
      if (afxProject == null) afxProject = AfxSolution.Instance.AfxDataLibraryProjects.FirstOrDefault(p => p.Project.Equals(project));
      return afxProject;
    }

    public static AfxProject GetProject(Guid assemblyGuid)
    {
      AfxProject afxProject = AfxSolution.Instance.AfxClassLibraryProjects.FirstOrDefault(p => p.AssemblyGuid.Equals(assemblyGuid));
      if (afxProject == null) afxProject = AfxSolution.Instance.AfxDataLibraryProjects.FirstOrDefault(p => p.AssemblyGuid.Equals(assemblyGuid));
      return afxProject;
    }

    #endregion

    #region AddProject()

    public static AfxProject AddProject(Project project)
    {
      AfxProject afxProject = AfxSolution.Instance.AfxClassLibraryProjects.FirstOrDefault(p => p.Project.Equals(project));
      if (afxProject != null) return afxProject;

      string guids = VisualStudioHelper.GetProjectTypeGuids(project);

      if (guids.Contains(ProjectFlavour.ClassLibrary.ProjectFactory.ClassLibraryProjectFactoryGuidString))
      {
        var afxCLProject = new AfxProjectClassLibrary(project);
        AfxSolution.Instance.AddAfxClassLibraryProject(afxCLProject);
        afxCLProject.OnAdded();
        return afxCLProject;
      }

      //if (guids.Contains(ProjectFlavour.ServiceLibrary.ServiceLibraryProjectFactory.ServiceLibraryProjectFactoryGuidString)) ProjectType = AfxProjectType.Service;

      if (guids.Contains(ProjectFlavour.SqlDataLibrary.ProjectFactory.SqlDataLibraryProjectFactoryGuidString))
      {
        var afxSQLProject = new AfxProjectSqlDataLibrary(project);
        AfxSolution.Instance.AddAfxDataLibraryProject(afxSQLProject);
        afxSQLProject.OnAdded();
        return afxSQLProject;
      }

      return null;
    }

    #endregion

    #region RemoveProject()

    public static void RemoveProject(Project project)
    {
      AfxProject afxProject = AfxSolution.Instance.AfxClassLibraryProjects.FirstOrDefault(p => p.Project.Equals(project));
      if (afxProject == null) afxProject = AfxSolution.Instance.AfxDataLibraryProjects.FirstOrDefault(p => p.Project.Equals(project));
      if (afxProject == null) return;
      AfxSolution.Instance.RemoveAfxProject(afxProject);
      afxProject.OnRemoved();
    }

    #endregion

    #endregion

    #region Private

    void ProcessProjectItems(ProjectItems items, bool isLoading)
    {
      foreach (ProjectItem pi in items)
      {
        if (!AddProjectItem(pi, isLoading))
        {
          ProcessProjectItems(pi.ProjectItems, isLoading);
        }
      }
    }

    #endregion
  }
}
