using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public class AfxDataRepositoryFolder : AfxFolder
  {
    internal AfxDataRepositoryFolder(AfxProjectSqlDataLibrary project) 
      : base("Data Repository Classes", project)
    {
    }

    //public ObservableCollection<AfxObjectClass> AfxObjectClasses
    //{
    //  get { return AfxProject.AfxObjectClasses; }
    //}
  }
}
