//------------------------------------------------------------------------------
// <copyright file="ApplicationStructureToolWindowPane.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Afx.Plugin.Tools.ApplicationStructure
{
  using System;
  using System.Runtime.InteropServices;
  using Microsoft.VisualStudio.Shell;
  using System.Collections.ObjectModel;
  using Models;

  [Guid("a415e81f-7e58-4190-8c83-546ebc439bc1")]
  public class ToolWindowPane : Microsoft.VisualStudio.Shell.ToolWindowPane
  {
    Control ApplicationStructureControl { get; set; }
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindowPane"/> class.
    /// </summary>
    public ToolWindowPane() : base(null)
    {
      this.Caption = "Afx Structure";

      // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
      // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
      // the object returned by the Content property.
    }

    protected override void OnCreate()
    {
      ApplicationStructureControl = new Control();
      this.Content = ApplicationStructureControl;
      ApplicationStructureControl.DataContext = new ViewModel();
      base.OnCreate();
    }

    public override void OnToolWindowCreated()
    {
      base.OnToolWindowCreated();
    }
  }
}
