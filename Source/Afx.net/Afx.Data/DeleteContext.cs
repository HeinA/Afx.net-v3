using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class DeleteContext
  {
    public DeleteContext(Guid owner)
    {
      Owner = owner;
    }

    public DeleteContext()
    {
      Owner = Guid.Empty;
    }

    public Guid Owner { get; private set; }

    Collection<Guid> mActiveTargets = new Collection<Guid>();
    public Collection<Guid> ActiveTargets
    {
      get { return mActiveTargets; }
    }
  }
}
