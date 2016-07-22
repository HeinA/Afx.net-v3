using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public class AfxObjectRepository : AfxType
  {
    public AfxObjectRepository(AfxProjectDataLibrary afxProject, FileCodeModel fileCodeModel, CodeClass codeClass)
      : base(afxProject, fileCodeModel, codeClass)
    {
      CodeClass ci = codeClass.Bases.OfType<CodeClass>().FirstOrDefault(ci1 => ci1.FullName.StartsWith(ApplicationStructure.AfxObjectRepository));
      string afxObjectName = ci.FullName.Split('<', '>')[1];
      TargetType = afxProject.ClassLibrary.AfxTypes.OfType<AfxObject>().FirstOrDefault(t => t.FullName.Equals(afxObjectName));
    }

    //public AfxObjectRepository(AfxAssembly assembly, Type type)
    //  : base(assembly, type)
    //{
    //}

    public AfxObject TargetType { get; private set; }
  }
}
