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
    public AfxProject ContainingProject { get; private set; }
    AfxAssembly Referenced_Assembly { get; set; }
    AfxProject Referenced_Project { get; set; }

    public IAfxAssembly ReferencedAssembly
    {
      get { return (IAfxAssembly)Referenced_Assembly ?? Referenced_Project; }
    }

    public AfxReference(AfxProject containingProject, AfxProject referencedProject, Reference reference)
    {
      ContainingProject = containingProject;
      Referenced_Project = referencedProject;
      Reference = reference;
    }

    public AfxReference(AfxProject containingProject, AfxAssembly referencedAssembly, Reference reference)
    {
      ContainingProject = containingProject;
      Referenced_Assembly = referencedAssembly;
      Reference = reference;
    }

    public bool IsProjectReference
    {
      get { return Referenced_Project != null; }
    }
  }
}
