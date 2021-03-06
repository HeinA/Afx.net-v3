﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public abstract class DataException : Exception
  {
    protected DataException()
    {
    }

    protected DataException(string message)
      : base(message)
    {
    }

    protected DataException(string message, Exception innerException)
      : base (message, innerException)
    {
    }
  }
}
