﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public abstract class MsSqlAggregateCollectionRepository<T> : AggregateCollectionRepository<T>
    where T : class, IAfxObject
  {
  }
}
