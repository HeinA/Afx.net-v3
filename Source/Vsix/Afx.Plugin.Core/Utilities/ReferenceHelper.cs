using Afx.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Utilities
{
  public static class ReferenceHelper
  {
    public static bool IsSameReference(this AfxReference source, AfxReference target)
    {
      return source.ReferencedAssembly.AssemblyId == target.ReferencedAssembly.AssemblyId;
    }
  }
}
