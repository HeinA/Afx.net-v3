using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Collections
{
  public interface IObjectCollection : ICollection, IEditNotifier, INotifyCollectionChanged
  {
    Type ItemType { get; }
    void Add(IAfxObject item);
  }
}
