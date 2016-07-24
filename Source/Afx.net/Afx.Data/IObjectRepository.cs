using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public interface IObjectRepository
  {
    Type TargetType { get; }

    LoadContext GetInstance(Guid id);
    LoadContext GetInstances(Guid owner);
    bool IsNew(Guid id);

    IAfxObject LoadObjectCore(LoadContext context);
    IAfxObject[] LoadObjectsCore(LoadContext context);

    void SaveObjectCore(IAfxObject target, SaveContext context);

    void DeleteObjectsCore(DeleteContext context);
  }
}
