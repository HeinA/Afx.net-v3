using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Collections
{
  class Comparer2<T> : Comparer<T>
  {
    private readonly Comparison<T> _compareFunction;

    #region Constructors

    public Comparer2(Comparison<T> comparison)
    {
      if (comparison == null) throw new ArgumentNullException("comparison");
      _compareFunction = comparison;
    }

    #endregion

    public override int Compare(T arg1, T arg2)
    {
      return _compareFunction(arg1, arg2);
    }
  }

}
