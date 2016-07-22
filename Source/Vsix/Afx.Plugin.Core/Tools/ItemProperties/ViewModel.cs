using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Tools.ItemProperties
{
  public class ViewModel : Afx.Plugin.ViewModel
  {
    public Models.ApplicationStructure ApplicationStructure
    {
      get { return Models.ApplicationStructure.Instance; }
    }
  }
}
