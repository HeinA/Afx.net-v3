using Afx.Plugin.Utilities;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vsix.Utilities;
using VSLangProj;

namespace Afx.Plugin.Models
{
  public class ApplicationStructure : ViewModel
  {
    public const string AfxObject = "Afx.AfxObject";
    public const string AfxObjectRepository = "Afx.Data.ObjectRepository";
    public const string AfxAssemblyAttribute = "Afx.AfxAssemblyAttribute";
    public const string AfxPersistentAttribute = "Afx.Data.PersistentAttribute";

    public const string ConstantsClass = "Constants";
    public const string AssemblyId = "AssemblyId";
    public const string DataLibrary_ClassLibraryId = "ClassLibraryId";

    public const string AssociativeObject = "Afx.AssociativeObject";
    const string ServiceModel = "System.ServiceModel.ServiceContractAttribute";

    public static ApplicationStructure Instance { get; private set; }

    static ApplicationStructure()
    {
      Instance = new ApplicationStructure();
    }

    object mSelectedItem;
    public object SelectedItem
    {
      get { return mSelectedItem; }
      set { SetProperty<object>(ref mSelectedItem, value); }
    }

    ObservableCollection<AfxProject> mProjects = new ObservableCollection<AfxProject>();
    public ObservableCollection<AfxProject> Projects
    {
      get { return mProjects; }
    }

    ObservableCollection<AfxAssembly> mAssemblies = new ObservableCollection<AfxAssembly>();
    public ObservableCollection<AfxAssembly> Assemblies
    {
      get { return mAssemblies; }
    }

    public static AfxProject GetAfxProject(Project project)
    {
      return Instance.Projects.FirstOrDefault(p => p.Project.Equals(project));
    }

    public static AfxProject GetAfxProject(string assemblyId)
    {
      return Instance.Projects.FirstOrDefault(p => p.AssemblyId.Equals(assemblyId));
    }

    public static void ProcessFile(Project project, FileCodeModel fileCodeModel)
    {
      if (!IsAfxProject(project)) return;
      AfxProject afxProject = GetAfxProject(project);
      if (afxProject != null) afxProject.ProcessFile(fileCodeModel);
    }

    public static void RemoveProject(Project project)
    {
      AfxProject afxProject = GetAfxProject(project);
      if (afxProject != null) Instance.Projects.Remove(afxProject);
    }

    public static void AddProject(Project project)
    {
      AfxProject afxProject = AfxProject.CreateAfxProject(project);
      if (afxProject != null) Instance.Projects.Add(afxProject);
    }

    public static void AddReference(Reference reference)
    {
      AfxProject afxProject = GetAfxProject(reference.ContainingProject);
      if (afxProject == null) return;
      afxProject.ProcessReference(reference);
    }

    public static void RemoveReference(Reference reference)
    {
      AfxProject afxProject = GetAfxProject(reference.ContainingProject);
      if (afxProject == null) return;
      afxProject.RemoveReference(reference);
    }

    public static void Clear()
    {
      Instance.Projects.Clear();
    }

    public static bool IsAfxProject(Project project)
    {
      string guids = null;
      guids = VisualStudioHelper.GetProjectTypeGuids(project);
      if (guids != null
        && (guids.Contains(ProjectFlavour.ClassLibrary.ProjectFactory.ClassLibraryProjectFactoryGuidString)
        || guids.Contains(ProjectFlavour.ServiceLibrary.ProjectFactory.ServiceLibraryProjectFactoryGuidString)
        || guids.Contains(ProjectFlavour.SqlDataLibrary.ProjectFactory.SqlDataLibraryProjectFactoryGuidString)))
      {
        return true;
      }
      return false;
    }

    public static bool IsAfxAssembly(Assembly assembly)
    {
      if (assembly.GetCustomAttributes().Any(a => a.GetType().FullName.Equals(AfxAssemblyAttribute))) return true;
      return false;
    }
  }
}
