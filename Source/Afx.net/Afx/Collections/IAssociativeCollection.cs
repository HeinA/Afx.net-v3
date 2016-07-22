using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Collections
{
  public interface IAssociativeCollection : IDictionary, IObjectCollection
  {
    Type AssociativeType { get; }
  }
}
