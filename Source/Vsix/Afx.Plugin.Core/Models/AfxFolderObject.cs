using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public sealed class AfxFolderObject : AfxFolder
  {
    internal AfxFolderObject(AfxProject project) 
      : base("Objects", project)
    {
    }

    public IEnumerable<AfxObject> AfxObjects
    {
      get { return AfxProject.AfxTypes.OfType<AfxObject>().OrderBy(t => t.FullName); }
    }

    protected internal override void OnRefresh()
    {
      base.OnRefresh();
      OnPropertyChanged("AfxObjects");
    }
  }
}
