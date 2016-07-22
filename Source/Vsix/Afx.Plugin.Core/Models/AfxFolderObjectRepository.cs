using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public class AfxFolderObjectRepository : AfxFolder
  {
    internal AfxFolderObjectRepository(AfxProjectDataLibrary project) 
      : base("Object Repositories", project)
    {
    }

    public IEnumerable<AfxObjectRepository> AfxObjectRepositories
    {
      get { return AfxProject.AfxTypes.OfType<AfxObjectRepository>().OrderBy(t => t.FullName); }
    }

    protected internal override void OnRefresh()
    {
      base.OnRefresh();
      OnPropertyChanged("AfxObjectRepositories");
    }
  }
}
