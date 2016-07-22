using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx
{
  public interface IAssociativeObject : IAfxObject
  {
    IAfxObject Reference { get; set; }
  }
}
