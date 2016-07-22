using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSLangProj;

namespace Afx.Plugin.Models
{
  public class ReferencedAfxObjectClass
  {
    public AfxReference AfxReference { get; private set; }
    public AfxObjectClass AfxObjectClass { get; set; }
    public AfxProject AfxProject { get; private set; }

    public ReferencedAfxObjectClass(AfxObjectClass objectClass, AfxReference reference)
    {
      AfxProject = AfxReference.AfxProject;
      AfxReference = reference;
      AfxObjectClass = objectClass;
    }
  }
}
