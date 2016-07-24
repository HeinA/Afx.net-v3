using Afx;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  [Persistent]
  [OwnedReference]
  public class PurchaseOrderItem : AssociativeObject<PurchaseOrder, InventoryItem>
  {
    #region Constructors

    public PurchaseOrderItem()
    {
    }

    public PurchaseOrderItem(Guid id)
      : base(id)
    {
    }

    public PurchaseOrderItem(string id)
      : base(id)
    {
    }

    #endregion
  }
}
