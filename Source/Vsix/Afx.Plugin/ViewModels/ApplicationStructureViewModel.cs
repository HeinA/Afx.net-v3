using Afx.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.ViewModels
{
  public class ApplicationStructureViewModel
  {
    AfxPackage AfxPackage { get; set; }

    public ApplicationStructureViewModel(AfxPackage package)
    {
      AfxPackage = package;
    }

    public ObservableCollection<AfxProject> Projects
    {
      get { return ApplicationStructure.Instance.Projects; }
    }
  }
}
