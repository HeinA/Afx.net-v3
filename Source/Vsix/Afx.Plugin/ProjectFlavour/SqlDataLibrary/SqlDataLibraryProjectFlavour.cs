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
  public class SqlDataLibraryProjectFlavour : FlavoredProjectBase
  {
    public const string SqlDataLibraryProjectGuidString = "08DDCC21-D5E5-438C-AF00-FE7CEEA407EE";
    AfxPackage Package { get; set; }

    public SqlDataLibraryProjectFlavour(AfxPackage package)
    {
      Package = package;
      base.serviceProvider = Package;
    }
  }
}
