//------------------------------------------------------------------------------
// <copyright file="ItemProperties.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Afx.Plugin.Tools.ItemProperties
{
  using System;
  using System.Runtime.InteropServices;
  using Microsoft.VisualStudio.Shell;

  /// <summary>
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
  [Guid("1eb0b079-ec20-4a66-9666-f0e0929cdec8")]
  public class ToolWindowPane : Microsoft.VisualStudio.Shell.ToolWindowPane
  {
    Control ItemPropertiesControl { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindowPane"/> class.
    /// </summary>
    public ToolWindowPane() : base(null)
    {
      this.Caption = "Afx Properties";

      // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
      // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
      // the object returned by the Content property.
      ItemPropertiesControl = new Control();
      this.Content = ItemPropertiesControl;
    }

    protected override void OnCreate()
    {
      ItemPropertiesControl.DataContext = new ViewModel();
      base.OnCreate();
    }
  }
}
