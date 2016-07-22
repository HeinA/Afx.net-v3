using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vsix.Utilities;

namespace Afx.Plugin.Utilities
{
  public static class TypeHelper
  {
    public const string AfxObject = "Afx.AfxObject";
    public const string DataRepository = "Afx.Data.ObjectRepository";
    public const string AfxAssemblyAttribute = "Afx.AfxAssemblyAttribute";

    public const string AssociativeObject = "Afx.AssociativeObject";
    const string ServiceModel = "System.ServiceModel.ServiceContractAttribute";


    public static bool IsAfxProject(Project project)
    {
      string guids = null;
      guids = VisualStudioHelper.GetProjectTypeGuids(project);
      if (guids != null
        && (guids.Contains(ProjectFlavour.ClassLibrary.ClassLibraryProjectFactory.ClassLibraryProjectFactoryGuidString)
        || guids.Contains(ProjectFlavour.ServiceLibrary.ServiceLibraryProjectFactory.ServiceLibraryProjectFactoryGuidString)
        || guids.Contains(ProjectFlavour.SqlDataLibrary.SqlDataLibraryProjectFactory.SqlDataLibraryProjectFactoryGuidString)))
      {
        return true;
      }
      return false;
    }

    public static bool IsAfxAssembly(Assembly assembly)
    {
      if (assembly.GetCustomAttributes().Any(a => a.GetType().FullName.Equals(AfxAssemblyAttribute))) return true;
      return false;
    }
  }
}
