using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class ObjectDataRowCollection : IEnumerable<ObjectDataRow>
  {
    Dictionary<Guid, List<ObjectDataRow>> mRowByOwner = new Dictionary<Guid, List<ObjectDataRow>>();

    #region Constructors

    public ObjectDataRowCollection(ObjectDataRow[] rows)
    {
      foreach (var row in rows)
      {
        Guid owner = row.Owner ?? Guid.Empty;
        List<ObjectDataRow> list = null;
        if (!mRowByOwner.ContainsKey(owner))
        {
          list = new List<ObjectDataRow>();
          mRowByOwner.Add(owner, list);
        }
        else
        {
          list = mRowByOwner[owner];
        }
        list.Add(row);
      }
    }

    #endregion

    #region GetOwnedObjects()

    public ObjectDataRow[] GetOwnedObjects(Guid? owner)
    {
      Guid o = owner ?? Guid.Empty;
      if (!mRowByOwner.ContainsKey(o)) return new ObjectDataRow[0];
      return mRowByOwner[o].ToArray();
    }

    #endregion


    #region IEnumerable

    public IEnumerator<ObjectDataRow> GetEnumerator()
    {
      return ((IEnumerable<ObjectDataRow>)GetOwnedObjects(null)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetOwnedObjects(null).GetEnumerator();
    }

    #endregion
  }
}
