using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VSLangProj;

namespace Afx.Plugin.Models
{
  public class AfxReference
  {
    public Reference Reference { get; private set; }
    public AfxProject AfxProject { get; private set; }

    public AfxReference(AfxProject project, Reference reference)
    {
      AfxProject = project;
      Reference = reference;
    }
  }
}
