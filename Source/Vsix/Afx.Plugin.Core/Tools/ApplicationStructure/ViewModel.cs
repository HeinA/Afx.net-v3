using Afx.Plugin.Commands;
using Afx.Plugin.Models;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Tools.ApplicationStructure
{
  public class ViewModel : Afx.Plugin.ViewModel
  {
    public ViewModel() 
    {
    }

    public IEnumerable<FolderViewModel> Layers
    {
      get
      {
        yield return new FolderViewModel("Interface Layer", Afx.Plugin.AfxSolution.AfxSolution.Instance.AfxClassLibraryProjects);
        yield return new FolderViewModel("Data Layer", Afx.Plugin.AfxSolution.AfxSolution.Instance.AfxDataLibraryProjects);
      }
    }

  }
}
