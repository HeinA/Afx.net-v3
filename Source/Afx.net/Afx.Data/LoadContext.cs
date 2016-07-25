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
      Id = Guid.Empty;
    }


    public Guid Owner { get; set; }
    public Guid Id { get; set; }

    Dictionary<Guid, IAfxObject> mObjectDictionary = new Dictionary<Guid, IAfxObject>();
    public void RegisterObject(IAfxObject obj)
    {
      mObjectDictionary.Add(obj.Id, obj);
    }

    public IAfxObject GetObject(Guid id)
    {
      return mObjectDictionary[id];
    }

    //Collection<ObjectTarget> mLoadTargets = new Collection<ObjectTarget>();
    //public Collection<ObjectTarget> LoadTargets
    //{
    //  get { return mLoadTargets; }
    //}

    //public void LoadObjectTargets(IEnumerable<ObjectTarget> targets)
    //{
    //  mLoadTargets = new Collection<ObjectTarget>(targets.ToList());
    //}
  }
}
