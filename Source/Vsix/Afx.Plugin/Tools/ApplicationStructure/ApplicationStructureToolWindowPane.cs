//------------------------------------------------------------------------------
// <copyright file="ApplicationStructureToolWindowPane.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Afx.Plugin
{
  using System;
  using System.Runtime.InteropServices;
  using Microsoft.VisualStudio.Shell;
  using System.Collections.ObjectModel;
  using Models;
  using ViewModels;/// <summary>
                   /// This class implements the tool window exposed by this package and hosts a user control.
                   /// </summary>
                   /// <remarks>
                   /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
                   /// usually implemented by the package implementer.
                   /// <para>
                   /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
                   /// implementation of the IVsUIElementPane interface.
                   /// </para>
                   /// </remarks>
  [Guid("a415e81f-7e58-4190-8c83-546ebc439bc1")]
  public class ApplicationStructureToolWindowPane : ToolWindowPane
  {
    AfxPackage AfxPackage { get; set; }
    ApplicationStructureToolWindowPaneControl ApplicationStructureToolWindowPaneControl { get; set; }
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationStructureToolWindowPane"/> class.
    /// </summary>
    public ApplicationStructureToolWindowPane() : base(null)
    {
      this.Caption = "Afx Structure";

      AfxPackage = (AfxPackage)this.Package;

      // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
      // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
      // the object returned by the Content property.
      ApplicationStructureToolWindowPaneControl = new ApplicationStructureToolWindowPaneControl();
      this.Content = ApplicationStructureToolWindowPaneControl;
    }

    protected override void OnCreate()
    {
      ApplicationStructureToolWindowPaneControl.DataContext = new ApplicationStructureViewModel(AfxPackage);
      base.OnCreate();
    }
  }
}
