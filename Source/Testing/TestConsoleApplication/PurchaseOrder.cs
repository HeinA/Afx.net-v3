using Afx.Collections;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  public class PurchaseOrder : Document
  {
    #region Constructors

    public PurchaseOrder()
    {
    }

    public PurchaseOrder(Guid id)
      : base(id)
    {
    }

    public PurchaseOrder(string id)
      : base(id)
    {
    }

    #endregion

    [Persistent]
    public bool IsComplete { get; set; }

    [Persistent]
    [Required(ErrorMessage = "required")]
    public string CustomerName { get; set; }


    AssociativeCollection<InventoryItem, PurchaseOrderItem> mItems;
    public AssociativeCollection<InventoryItem, PurchaseOrderItem> Items { get { return this.GetAssociativeCollection<InventoryItem, PurchaseOrderItem>(ref mItems); } }
  }
}
