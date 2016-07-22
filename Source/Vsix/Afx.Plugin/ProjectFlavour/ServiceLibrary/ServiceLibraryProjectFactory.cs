using Microsoft.VisualStudio.Shell.Flavor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.ProjectFlavour.ServiceLibrary
{
  [Guid(ServiceLibraryProjectFactoryGuidString)]
  public class ServiceLibraryProjectFactory : FlavoredProjectFactoryBase
  {
    public const string ServiceLibraryProjectFactoryGuidString = "880389B4-B814-4796-844B-F0E1678C31D1";
    public static readonly Guid ServiceLibraryProjectFactoryGuid = new Guid(ServiceLibraryProjectFactoryGuidString);

    private AfxPackage Package { get; set; }

    public ServiceLibraryProjectFactory(AfxPackage package)
      : base()
    {
      Package = package;
    }

    #region IVsAggregatableProjectFactory

    protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
    {
      ServiceLibraryProjectFlavour newProject = new ServiceLibraryProjectFlavour(Package);
      return newProject;
    }

    #endregion
  }
}
