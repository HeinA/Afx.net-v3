using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class LoadContext
  {
    public LoadContext()
    {
      Owner = Guid.Empty;
    }

    public LoadContext(Guid owner)
    {
      Owner = owner;
    }

    public Guid Owner { get; private set; }

    Collection<ObjectTarget> mLoadTargets = new Collection<ObjectTarget>();
    public Collection<ObjectTarget> LoadTargets
    {
      get { return mLoadTargets; }
    }

    public void LoadObjectTargets(IEnumerable<ObjectTarget> targets)
    {
      mLoadTargets = new Collection<ObjectTarget>(targets.ToList());
    }
  }
}
