﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  [AttributeUsage(AttributeTargets.Property)] //AttributeTargets.Class | 
  public class DataCacheAttribute : Attribute
  {
  }
}
