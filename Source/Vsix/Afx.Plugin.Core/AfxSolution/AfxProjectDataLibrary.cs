using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.AfxSolution
{
  public abstract class AfxProjectDataLibrary : AfxProject
  {
    protected AfxProjectDataLibrary(Project project)
      : base(project)
    {
    }

    ObservableCollection<AfxObjectRepository> mRepositories = new ObservableCollection<AfxObjectRepository>();
    public ObservableCollection<AfxObjectRepository> Repositories
    {
      get { return mRepositories; }
    }

    public override IEnumerable<AfxFolder> Folders
    {
      get
      {
        foreach (var folder in base.Folders)
        {
          yield return folder;
        }
        yield return new AfxFolder("Object Repositories", Repositories);
      }
    }
  }
}
