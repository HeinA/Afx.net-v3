using Afx.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Business
{
  public class Invoice : Document
  {
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
  }
}
