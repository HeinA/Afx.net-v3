using Afx.Plugin.Utilities;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VSLangProj;

namespace Afx.Plugin.Models
{
  public class ApplicationStructure
  {
    public static ApplicationStructure Instance { get; private set; }

    static ApplicationStructure()
    {
      Instance = new ApplicationStructure();
    }

    ObservableCollection<AfxProject> mProjects = new ObservableCollection<AfxProject>();
    public ObservableCollection<AfxProject> Projects
    {
      get { return mProjects; }
    }

    public static void ProcessFile(Project project, FileCodeModel fileCodeModel)
    {
      if (!TypeHelper.IsAfxProject(project)) return;
      AfxProject afxProject = Instance.Projects.FirstOrDefault(p => p.Project.Equals(project));
      if (afxProject != null) afxProject.ProcessFile(fileCodeModel);
    }
    public static void RemoveProject(Project project)
    {
      AfxProject afxProject = Instance.Projects.FirstOrDefault(p => p.Project.Equals(project));
      if (afxProject != null) Instance.Projects.Remove(afxProject);
    }
    public static void AddProject(Project project)
    {
      Instance.Projects.Add(AfxProject.CreateAfxProject(project));
    }
    public static void AddReference(Reference reference)
    {
      AfxProject afxProject = Instance.Projects.FirstOrDefault(p => p.Project.Equals(reference.ContainingProject));
      if (afxProject == null) return;
      afxProject.ProcessReference(reference);
    }
    public static void RemoveReference(Reference reference)
    {
      AfxProject afxProject = Instance.Projects.FirstOrDefault(p => p.Project.Equals(reference.ContainingProject));
      if (afxProject == null) return;
      afxProject.RemoveReference(reference);
    }
    public static void Clear()
    {
      Instance.Projects.Clear();
    }
  }
}
