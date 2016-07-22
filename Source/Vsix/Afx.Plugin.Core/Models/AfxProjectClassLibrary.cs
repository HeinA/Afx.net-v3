using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public sealed class AfxProjectClassLibrary : AfxProject
  {
    internal AfxProjectClassLibrary(Project project)
      : base(project)
    {
    }

    public override IEnumerable<AfxFolder> Folders
    {
      get
      {
        yield return AfxObjectsFolder;
      }
    }

    public IEnumerable<AfxProjectDataLibrary> DataLibraries
    {
      get { return ApplicationStructure.Instance.Projects.OfType<AfxProjectDataLibrary>().Where(l => l.ClassLibraryId.Equals(AssemblyId)); }
    }

    public override bool ProcessClass(CodeClass codeClass, FileCodeModel fileCodeModel)
    {
      bool processed = base.ProcessClass(codeClass, fileCodeModel);

      AfxObject afxObjectClass = AfxTypes.OfType<AfxObject>().FirstOrDefault(c => c.CodeClass.Equals(codeClass) && c.FileCodeModel.Equals(fileCodeModel));
      if (afxObjectClass == null) return processed;

      foreach (var dl in DataLibraries)
      {
        dl.ProcessAfxObject(afxObjectClass);
      }

      return processed;
    }
  }
}