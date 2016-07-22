using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public interface IObjectRepository<T>
    where T : IAfxObject
  {
    T LoadObject(Guid id);
    T SaveObject(T target);
  }
}
