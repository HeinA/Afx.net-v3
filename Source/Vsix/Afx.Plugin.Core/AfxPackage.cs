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
using System.Collections.ObjectModel;
using System.Linq;
using Afx.Plugin.ProjectFlavour.ClassLibrary;
using Afx.Plugin.ProjectFlavour.ServiceLibrary;
using Afx.Plugin.ProjectFlavour.SqlDataLibrary;
using VSLangProj;
using System.Reflection;

namespace Afx.Plugin
{
  [PackageRegistration(UseManagedResourcesOnly = true)]
  [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] 
  [ProvideMenuResource("Menus.ctmenu", 1)]
  [ProvideToolWindow(typeof(ApplicationStructureToolWindowPane))]
  [Guid(AfxPackage.PackageGuidString)]
  [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
  [ProvideProjectFactory(typeof(ClassLibraryProjectFactory), "Class Library", null, null, null, null)]
  [ProvideProjectFactory(typeof(ServiceLibraryProjectFactory), "Service Library", null, null, null, null)]
  [ProvideProjectFactory(typeof(ProjectFactory), "Sql Data Library", null, null, null, null)]
  [ProvideToolWindow(typeof(Afx.Plugin.ItemProperties))]
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
      ApplicationStructureToolWindowPaneCommand.Initialize(this);

      this.RegisterProjectFactory(new ClassLibraryProjectFactory(this));
      this.RegisterProjectFactory(new ServiceLibraryProjectFactory(this));
      this.RegisterProjectFactory(new ProjectFactory(this));

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
        Afx.Plugin.ItemPropertiesCommand.Initialize(this);
    }

    #region Solution

    private void SolutionEvents_AfterClosing()
    {
      ApplicationStructure.Clear();
    }

    private void SolutionEvents_Opened()
    {
      //foreach (Project p in mDte.Solution.Projects)
      //{
      //  ApplicationStructure.AddProject(p);
      //}
    }

    #endregion

    #region Projects

    private void SolutionEvents_ProjectRenamed(Project Project, string OldName)
    {
      AfxProject project = ApplicationStructure.Instance.Projects.FirstOrDefault(p => p.Project.Equals(Project));
      if (project != null) project.OnProjectRenamed();
    }

    private void SolutionEvents_ProjectRemoved(Project Project)
    {
      ApplicationStructure.RemoveProject(Project);
    }

    private void SolutionEvents_ProjectAdded(Project Project)
    {
      ApplicationStructure.AddProject(Project);
    }

    #endregion

    #region References

    private void ReferencesEvents_ReferenceRemoved(Reference pReference)
    {
      ApplicationStructure.RemoveReference(pReference);
    }

    private void ReferencesEvents_ReferenceAdded(Reference pReference)
    {
      ApplicationStructure.AddReference(pReference);
    }

    #endregion

    #region Files

    private void ProjectItemsEvents_ItemRenamed(ProjectItem ProjectItem, string OldName)
    {
      ApplicationStructure.ProcessFile(ProjectItem.ContainingProject, ProjectItem.FileCodeModel);
    }

    private void ProjectItemsEvents_ItemRemoved(ProjectItem ProjectItem)
    {
      ApplicationStructure.ProcessFile(ProjectItem.ContainingProject, ProjectItem.FileCodeModel);
    }

    private void ProjectItemsEvents_ItemAdded(ProjectItem ProjectItem)
    {
      ApplicationStructure.ProcessFile(ProjectItem.ContainingProject, ProjectItem.FileCodeModel);
    }

    private void DocumentEvents_DocumentSaved(Document Document)
    {
      ApplicationStructure.ProcessFile(Document.ProjectItem.ContainingProject, Document.ProjectItem.FileCodeModel);
    }

    #endregion

    #endregion
  }
}
