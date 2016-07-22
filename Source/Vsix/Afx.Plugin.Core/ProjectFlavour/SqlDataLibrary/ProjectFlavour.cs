using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.ProjectFlavour.SqlDataLibrary
{
  [Guid(SqlDataLibraryProjectGuidString)]
  public class ProjectFlavour : FlavoredProjectBase
  {
    public const string SqlDataLibraryProjectGuidString = "08DDCC21-D5E5-438C-AF00-FE7CEEA407EE";
    Package Package { get; set; }

    public ProjectFlavour(Package package)
    {
      Package = package;
      base.serviceProvider = Package;
    }
  }
}
