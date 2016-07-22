using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace Vsix.Utilities
{
  public static class VisualStudioHelper
  {

    public static bool ShowDialog(System.Windows.Window dialog)
    {
      var dte = (DTE)Package.GetGlobalService(typeof(SDTE));
      var hwnd = dte.MainWindow.HWnd;
      var helper = new WindowInteropHelper(dialog);
      helper.Owner = new IntPtr(hwnd);
      var dialogResult = dialog.ShowDialog();
      return dialogResult.HasValue && dialogResult.Value;
    }

    public static string GetProjectTypeGuids(Project proj)
    {
      string projectTypeGuids = "";
      object service = null;
      Microsoft.VisualStudio.Shell.Interop.IVsSolution solution = null;
      Microsoft.VisualStudio.Shell.Interop.IVsHierarchy hierarchy = null;
      Microsoft.VisualStudio.Shell.Interop.IVsAggregatableProject aggregatableProject = null;
      int result = 0;
      service = GetService(proj.DTE, typeof(Microsoft.VisualStudio.Shell.Interop.IVsSolution));
      solution = (Microsoft.VisualStudio.Shell.Interop.IVsSolution)service;

      result = solution.GetProjectOfUniqueName(proj.UniqueName, out hierarchy);

      if (result == 0)
      {
        aggregatableProject = hierarchy as Microsoft.VisualStudio.Shell.Interop.IVsAggregatableProject;
        if (aggregatableProject != null)
        {
          result = aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);
        }
      }

      return projectTypeGuids;
    }

    public static object GetService(object serviceProvider, System.Type type)
    {
      return GetService(serviceProvider, type.GUID);
    }

    public static object GetService(object serviceProviderObject, System.Guid guid)
    {
      object service = null;
      Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = null;
      IntPtr serviceIntPtr;
      int hr = 0;
      Guid SIDGuid;
      Guid IIDGuid;

      SIDGuid = guid;
      IIDGuid = SIDGuid;
      serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)serviceProviderObject;
      hr = serviceProvider.QueryService(ref SIDGuid, ref IIDGuid, out serviceIntPtr);

      if (hr != 0)
      {
        System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
      }
      else if (!serviceIntPtr.Equals(IntPtr.Zero))
      {
        service = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(serviceIntPtr);
        System.Runtime.InteropServices.Marshal.Release(serviceIntPtr);
      }

      return service;
    }
  }
}
