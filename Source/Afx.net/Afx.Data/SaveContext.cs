using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class SaveContext
  {
    public SaveContext()
    {
      OnlyProcessDirty = true;
      Merge = false;
    }

    public SaveContext(bool merge)
      : this()
    {
      Merge = merge;
    }

    public SaveContext(bool merge, bool onlyProcessDirty)
      : this(merge)
    {
      OnlyProcessDirty = onlyProcessDirty;
    }

    public bool Merge { get; private set; }
    public bool OnlyProcessDirty { get; private set; }

    public bool ShouldProcess(IAfxObject target)
    {
      return (!OnlyProcessDirty || (OnlyProcessDirty && target.IsDirty));
    }

    Collection<IAfxObject> mSavedObjects = new Collection<IAfxObject>();
    public Collection<IAfxObject> SavedObjects
    {
      get { return mSavedObjects; }
    }

    Collection<IAfxObject> mDeletedObjects = new Collection<IAfxObject>();
    public Collection<IAfxObject> DeletedObjects
    {
      get { return mDeletedObjects; }
    }
  }
}
