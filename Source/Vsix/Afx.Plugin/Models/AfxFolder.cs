using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public abstract class AfxFolder
  {
    public AfxProject AfxProject { get; private set; }
    public string Name { get; private set; }

    internal AfxFolder(string name, AfxProject project)
    {
      Name = name;
      AfxProject = project;
    }
  }
}
