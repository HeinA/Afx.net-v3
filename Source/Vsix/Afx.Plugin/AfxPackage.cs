//------------------------------------------------------------------------------
// <copyright file="AfxPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;
using Afx.Plugin.Models;
using System.Linq;
using VSLangProj;
using Afx.Plugin.Commands;

namespace Afx.Plugin
{
  [PackageRegistration(UseManagedResourcesOnly = true)]
  [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] 
  [ProvideMenuResource("Menus.ctmenu", 1)]
  [ProvideToolWindow(typeof(Tools.ApplicationStructure.ToolWindowPane))]
  [Guid(AfxPackage.PackageGuidString)]
  [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
  [ProvideProjectFactory(typeof(ProjectFlavour.ClassLibrary.ProjectFactory), "Class Library", null, null, null, null)]
  [ProvideProjectFactory(typeof(ProjectFlavour.ServiceLibrary.ProjectFactory), "Service Library", null, null, null, null)]
  [ProvideProjectFactory(typeof(ProjectFlavour.SqlDataLibrary.ProjectFactory), "Sql Data Library", null, null, null, null)]
  [ProvideToolWindow(typeof(Tools.ItemProperties.ToolWindowPane))]
  public sealed class AfxPackage : Package
  {
    public const string PackageGuidString = "46a7336b-bb6f-47e6-b518-b7175664d587";

    DTE mDte;
    SolutionEvents mSolutionEvents;
    DocumentEvents mDocumentEvents;
    ProjectItemsEvents mProjectItemsEvents;
    ReferencesEvents mReferencesEvents;

    public AfxPackage()
    {
    }

    #region Package Members

    protected override void Initialize()
    {
      ApplicationStructureCommand.Initialize(this);
      ItemPropertiesCommand.Initialize(this);

      this.RegisterProjectFactory(new ProjectFlavour.ClassLibrary.ProjectFactory(this));
      this.RegisterProjectFactory(new ProjectFlavour.ServiceLibrary.ProjectFactory(this));
      this.RegisterProjectFactory(new ProjectFlavour.SqlDataLibrary.ProjectFactory(this));

      IServiceContainer serviceContainer = this as IServiceContainer;
      mDte = serviceContainer.GetService(typeof(SDTE)) as DTE;

      mSolutionEvents = mDte.Events.SolutionEvents;
      mSolutionEvents.Opened += SolutionEvents_Opened;
      mSolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
      mSolutionEvents.ProjectRenamed += SolutionEvents_ProjectRenamed;
      mSolutionEvents.ProjectAdded += SolutionEvents_ProjectAdded;
      mSolutionEvents.ProjectRemoved += SolutionEvents_ProjectRemoved;

      mReferencesEvents = mDte.Events.GetObject("CSharpReferencesEvents") as ReferencesEvents;
      mReferencesEvents.ReferenceAdded += ReferencesEvents_ReferenceAdded;
      mReferencesEvents.ReferenceRemoved += ReferencesEvents_ReferenceRemoved;

      mProjectItemsEvents = mDte.Events.GetObject("CSharpProjectItemsEvents") as ProjectItemsEvents;
      mProjectItemsEvents.ItemAdded += ProjectItemsEvents_ItemAdded;
      mProjectItemsEvents.ItemRemoved += ProjectItemsEvents_ItemRemoved;
      mProjectItemsEvents.ItemRenamed += ProjectItemsEvents_ItemRenamed;

      mDocumentEvents = mDte.Events.DocumentEvents;
      mDocumentEvents.DocumentSaved += DocumentEvents_DocumentSaved;

      base.Initialize();
    }

    private void SolutionEvents_Opened()
    {
      AfxSolution.AfxSolution.ProcessQueue();
    }

    #region Solution

    private void SolutionEvents_AfterClosing()
    {
      //ApplicationStructure.Clear();
      AfxSolution.AfxSolution.Instance.Close();
    }

    #endregion

    #region Projects

    private void SolutionEvents_ProjectRenamed(Project Project, string OldName)
    {
      //AfxProject project = ApplicationStructure.Instance.Projects.FirstOrDefault(p => p.Project.Equals(Project));
      //if (project != null) project.OnProjectRenamed();
      AfxSolution.AfxProject proj = AfxSolution.AfxProject.GetProject(Project);
      if (proj != null) proj.OnRenamed();
    }

    private void SolutionEvents_ProjectRemoved(Project Project)
    {
      //ApplicationStructure.RemoveProject(Project);
      AfxSolution.AfxProject.RemoveProject(Project);
    }

    private void SolutionEvents_ProjectAdded(Project Project)
    {
      //ApplicationStructure.AddProject(Project);
      AfxSolution.AfxProject.AddProject(Project);
    }

    #endregion

    #region References

    private void ReferencesEvents_ReferenceRemoved(Reference pReference)
    {
      //ApplicationStructure.RemoveReference(pReference);
    }

    private void ReferencesEvents_ReferenceAdded(Reference pReference)
    {
      //ApplicationStructure.AddReference(pReference);
    }

    #endregion

    #region Files

    private void ProjectItemsEvents_ItemRenamed(ProjectItem ProjectItem, string OldName)
    {
      //ApplicationStructure.ProcessFile(ProjectItem.ContainingProject, ProjectItem.FileCodeModel);
      AfxSolution.AfxProject proj = AfxSolution.AfxProject.GetProject(ProjectItem.ContainingProject);
      if (proj != null) proj.RenameProjectItem(ProjectItem, OldName);
    }

    private void ProjectItemsEvents_ItemRemoved(ProjectItem ProjectItem)
    {
      //ApplicationStructure.ProcessFile(ProjectItem.ContainingProject, ProjectItem.FileCodeModel);
      AfxSolution.AfxProject proj = AfxSolution.AfxProject.GetProject(ProjectItem.ContainingProject);
      if (proj != null) proj.RemoveProjectItem(ProjectItem);
    }

    private void ProjectItemsEvents_ItemAdded(ProjectItem ProjectItem)
    {
      //ApplicationStructure.ProcessFile(ProjectItem.ContainingProject, ProjectItem.FileCodeModel);
      AfxSolution.AfxProject proj = AfxSolution.AfxProject.GetProject(ProjectItem.ContainingProject);
      if (proj != null) proj.AddProjectItem(ProjectItem, false);
    }

    private void DocumentEvents_DocumentSaved(Document Document)
    {
      AfxSolution.AfxProject proj = AfxSolution.AfxProject.GetProject(Document.ProjectItem.ContainingProject);
      if (proj != null) proj.ProcessFileCodeModel(Document.ProjectItem.FileCodeModel, false, null);
      //ApplicationStructure.ProcessFile(Document.ProjectItem.ContainingProject, Document.ProjectItem.FileCodeModel);
    }

    #endregion

    #endregion
  }
}
