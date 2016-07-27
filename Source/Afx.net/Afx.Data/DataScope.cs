using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class DataScope : IDisposable
  {
    public DataScope(string dataScopeName)
    {
      Guard.ThrowIfNullOrEmpty(dataScopeName, nameof(dataScopeName));

      ScopeStack.Push(dataScopeName);
    }

    public void Dispose()
    {
      if (ScopeStack.Count > 0) ScopeStack.Pop();
    }

    public static string DefaultScope { get; set; }

    [ThreadStatic]
    static Stack<string> mScopeStack;
    static Stack<string> ScopeStack
    {
      get { return mScopeStack ?? (mScopeStack = new Stack<string>()); }
    }

    public static string CurrentScope
    {
      get { return ScopeStack.Count == 0 ? DefaultScope  : ScopeStack.Peek(); }
    }

    static Dictionary<string, DataCache> mDataCacheDictionary = new Dictionary<string, DataCache>();
  }
}
