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
  public class InventoryItem : AfxObject
  {
    #region Constructors

    public InventoryItem()
    {
    }

    public InventoryItem(Guid id)
      : base(id)
    {
    }

    public InventoryItem(string id)
      : base(id)
    {
    }

    #endregion

    [Persistent]
    public string Name { get; set; }
  }
}
