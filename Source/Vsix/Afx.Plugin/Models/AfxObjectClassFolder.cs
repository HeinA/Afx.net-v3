using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public sealed class AfxObjectClassFolder : AfxFolder
  {
    internal AfxObjectClassFolder(AfxProject project) 
      : base("Object Classes", project)
    {
    }

    public ObservableCollection<AfxObjectClass> AfxObjectClasses
    {
      get { return AfxProject.AfxObjectClasses; }
    }
  }
}
