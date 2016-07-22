using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public sealed class AfxProjectSqlDataLibrary : AfxProject
  {
    internal AfxProjectSqlDataLibrary(Project project)
      : base(project)
    {
    }

    AfxDataRepositoryFolder mAfxDataRepositoryFolder;
    public AfxDataRepositoryFolder AfxDataRepositoryFolder
    {
      get { return mAfxDataRepositoryFolder ?? (mAfxDataRepositoryFolder = new AfxDataRepositoryFolder(this)); }
    }

    public override IEnumerable<AfxFolder> Folders
    {
      get
      {
        yield return AfxObjectClassFolder;
        yield return AfxDataRepositoryFolder;
      }
    }
  }
}
