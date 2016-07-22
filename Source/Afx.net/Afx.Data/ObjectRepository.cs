using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  [AfxBaseType]
  public abstract class ObjectRepository<T> : IObjectRepository<T>
    where T : IAfxObject
  {
    public abstract T LoadObject(Guid id);
    public abstract T SaveObject(T target);
  }
}
