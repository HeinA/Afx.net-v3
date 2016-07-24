using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class ObjectTarget
  {
    public ObjectTarget(Guid id, int ix, string assemblyType)
    {
      Id = id;
      Ix = ix;
      AssemblyTypeName = assemblyType;
    }

    public Guid Id { get; private set; }
    public int Ix { get; private set; }
    public string AssemblyTypeName { get; private set; }

    public Type AssemblyType
    {
      get { return Type.GetType(AssemblyTypeName); }
    }
  }
}
