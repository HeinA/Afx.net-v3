﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public interface IObjectRepositoryBuilder
  {
    IObjectRepository BuildRepository(Type type);
  }
}
