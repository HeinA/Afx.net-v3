using Afx.Collections;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Business
{
  public class PurchaseOrder : Document
  {
    #region bool IsComplete

    public const string IsCompleteProperty = "IsComplete";
    bool mIsComplete;
    [Persistent]
    public bool IsComplete
    {
      get { return mIsComplete; }
      set { SetProperty<bool>(ref mIsComplete, value); }
    }

    #endregion

    #region string CustomerName

    public const string CustomerNameProperty = "CustomerName";
    string mCustomerName;
    [Persistent]
    [Required(ErrorMessage = "required")]
    public string CustomerName
    {
      get { return mCustomerName; }
      set { SetProperty<string>(ref mCustomerName, value); }
    }

    #endregion


    AssociativeCollection<InventoryItem, PurchaseOrderItem> mItems;
    public AssociativeCollection<InventoryItem, PurchaseOrderItem> Items { get { return this.GetAssociativeCollection<InventoryItem, PurchaseOrderItem>(ref mItems); } }
  }
}
