using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.AfxSolution
{
  public class AfxProjectEventArgs : EventArgs
  {
    public AfxProject AfxProject { get; private set; }

    public AfxProjectEventArgs(AfxProject project)
    {
      AfxProject = project;
    }
  }
}
