using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.ProjectFlavour.SqlDataLibrary
{
  [Guid(SqlDataLibraryProjectFactoryGuidString)]
  public class ProjectFactory : FlavoredProjectFactoryBase
  {
    public const string SqlDataLibraryProjectFactoryGuidString = "2FA2AEF0-3453-40FA-877B-1D08900C6C91";
    public static readonly Guid SqlDataLibraryProjectFactoryGuid = new Guid(SqlDataLibraryProjectFactoryGuidString);

    private Package Package { get; set; }

    public ProjectFactory(Package package)
      : base()
    {
      Package = package;
    }

    #region IVsAggregatableProjectFactory

    protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
    {
      ProjectFlavour newProject = new ProjectFlavour(Package);
      return newProject;
    }

    #endregion
  }
}
