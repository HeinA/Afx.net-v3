using Afx.Plugin.Utilities;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public abstract class AfxProjectDataLibrary : AfxProject
  {
    protected AfxProjectDataLibrary(Project project)
      : base(project)
    {
    }

    public string ClassLibraryId { get; private set; }


    AfxFolderObjectRepository mAfxObjectRepositoriesFolder;
    public AfxFolderObjectRepository AfxObjectRepositoriesFolder
    {
      get { return mAfxObjectRepositoriesFolder ?? (mAfxObjectRepositoriesFolder = new AfxFolderObjectRepository(this)); }
    }

    public override IEnumerable<AfxFolder> Folders
    {
      get
      {
        yield return AfxObjectsFolder;
        yield return AfxObjectRepositoriesFolder;
      }
    }

    protected override void ProcessConstants(IEnumerable<CodeVariable> constants)
    {
      base.ProcessConstants(constants);

      CodeVariable cv = constants.FirstOrDefault(cc => cc.Name == ApplicationStructure.DataLibrary_ClassLibraryId);
      if (cv != null) this.ClassLibraryId = (string)cv.InitExpression;
    }

    public AfxProjectClassLibrary ClassLibrary
    {
      get { return ApplicationStructure.Instance.Projects.OfType<AfxProjectClassLibrary>().FirstOrDefault(l => l.AssemblyId.Equals(ClassLibraryId)); }
    }

    public void ProcessClassLibrary()
    {
      foreach (var obj in ClassLibrary.AfxTypes.OfType<AfxObject>())
      {
        ProcessAfxObject(obj);
      }
    }

    public abstract void ProcessAfxObject(AfxObject obj);
  }
}
