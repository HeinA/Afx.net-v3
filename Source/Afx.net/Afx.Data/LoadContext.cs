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
      Target = Guid.Empty;
    }

    public Guid Target { get; set; }

    Dictionary<Guid, IAfxObject> mObjectDictionary = new Dictionary<Guid, IAfxObject>();
    public void RegisterObject(IAfxObject obj)
    {
      mObjectDictionary.Add(obj.Id, obj);
    }

    public IAfxObject GetObject(Guid id)
    {
      return mObjectDictionary[id];
    }
  }
}
