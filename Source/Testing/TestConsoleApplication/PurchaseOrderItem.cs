﻿using Afx;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  [Persistent]
  [CompositeReference]
  public class PurchaseOrderItem : AssociativeObject<PurchaseOrder, InventoryItem>
  {
  }
}
