using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx
{
  public class StateSuppressor : IDisposable
  {
    public StateSuppressor()
    {
      IsSuppressed = true;
    }

    [ThreadStatic]
    static bool mIsSuppressed = false;
    public static bool IsSuppressed
    {
      get { return mIsSuppressed; }
      private set { mIsSuppressed = value; }
    }

    public void Dispose()
    {
      IsSuppressed = false;
    }
  }
}
