using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
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
  public class ProjectFlavour : FlavoredProjectBase 
  {
    public const string ClassLibraryProjectGuidString = "46AB4897-FB54-4F65-839C-C12909CE7753";
    Package Package { get; set; }

    public ProjectFlavour(Package package)
    {
      Package = package;
      base.serviceProvider = Package;
    }
  }
}
