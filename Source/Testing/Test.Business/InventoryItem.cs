using Afx;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Business
{
  //[Persistent]
  [AggregateReferenceAttribute]
  public class InventoryItem : AfxObject
  {
    #region string Name

    public const string NameProperty = "Name";
    string mName;
    [Persistent]
    public string Name
    {
      get { return mName; }
      set { SetProperty<string>(ref mName, value); }
    }

    #endregion
  }
}
