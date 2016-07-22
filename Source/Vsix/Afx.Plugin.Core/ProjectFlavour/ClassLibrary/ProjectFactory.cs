using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.ProjectFlavour.ClassLibrary
{
  [Guid(ClassLibraryProjectFactoryGuidString)]
  public class ProjectFactory : FlavoredProjectFactoryBase
  {
    public const string ClassLibraryProjectFactoryGuidString = "2103ECAD-6261-486C-9D62-CBF61CF5D4F4";
    public static readonly Guid ClassLibraryProjectFactoryGuid = new Guid(ClassLibraryProjectFactoryGuidString);

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
