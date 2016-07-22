using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Vsix.Utilities;
using System.IO;
using Afx.Plugin.Models;

namespace Afx.Plugin.ProjectFlavour.SqlDataLibrary
{
  public class ProjectWizard : IWizard
  {
    ProjectWizardViewModel mViewModel = new ProjectWizardViewModel();

    public void BeforeOpeningFile(ProjectItem projectItem)
    {
    }

    public void ProjectFinishedGenerating(Project project)
    {
      var vsProject = project.Object as VSLangProj.VSProject;
      AfxProject classLibraryProject = ApplicationStructure.GetAfxProject(mViewModel.SelectedClassLibrary.AssemblyId);
      vsProject.References.AddProject(classLibraryProject.Project);

      AfxProjectSqlDataLibrary dataLibraryProject = (AfxProjectSqlDataLibrary)ApplicationStructure.GetAfxProject(project);
      dataLibraryProject.ProcessClassLibrary();
    }

    public void ProjectItemFinishedGenerating(ProjectItem projectItem)
    {
    }

    public void RunFinished()
    {
    }

    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
      ProjectWizardView ui = new ProjectWizardView();
      ui.DataContext = mViewModel;

      if (!VisualStudioHelper.ShowDialog(ui))
      {
        #region Cleanup Folders

        string path = replacementsDictionary["$destinationdirectory$"];
        if (Directory.EnumerateFileSystemEntries(path).Count() == 0)
        {
          Directory.Delete(path);
        }

        path = replacementsDictionary["$solutiondirectory$"];
        if (Directory.EnumerateFileSystemEntries(path).Count() == 0)
        {
          Directory.Delete(path);
        }

        #endregion

        throw new WizardCancelledException();
      }

      replacementsDictionary.Add("$servicingclasslibrary$", mViewModel.SelectedClassLibrary.AssemblyId);
      FileSystemInfo projectFolder = new DirectoryInfo(replacementsDictionary["$destinationdirectory$"]);
      FileSystemInfo solutionFolder = new DirectoryInfo(replacementsDictionary["$solutiondirectory$"]);
      string relativePath = projectFolder.GetRelativePathTo(solutionFolder);
      replacementsDictionary.Add("$relativeProjectPath$", relativePath);
    }

    public bool ShouldAddProjectItem(string filePath)
    {
      return true;
    }
  }
}
