using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.ProjectFlavour.ClassLibrary
{
  [Guid(ClassLibraryProjectGuidString)]
  public class ClassLibraryProjectFlavour : FlavoredProjectBase 
  {
    public const string ClassLibraryProjectGuidString = "46AB4897-FB54-4F65-839C-C12909CE7753";
    AfxPackage Package { get; set; }

    public ClassLibraryProjectFlavour(AfxPackage package)
    {
      Package = package;
      base.serviceProvider = Package;
    }
  }
}
