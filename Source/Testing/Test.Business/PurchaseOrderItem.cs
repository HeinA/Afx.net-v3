﻿using Afx;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Business
{
  //[Persistent]
  //[CompositeReference]
  public class PurchaseOrderItem : AssociativeObject<PurchaseOrder, InventoryItem>
  {
  }
}
