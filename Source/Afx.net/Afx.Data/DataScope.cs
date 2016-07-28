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
      ScopeName = dataScopeName;
      ScopeStack.Push(this);
    }

    public void Dispose()
    {
      if (ScopeStack.Count > 0) ScopeStack.Pop();
    }

    public string ScopeName { get; private set; }

    //public void InitializeCache()
    //{
    //  DataCache.InitializeForDataScope();
    //}

    public static DataScope DefaultScope { get; private set; }

    public void BuildAndLoadRepositoriesInMemory()
    {
      RepositoryBuilder.GetForDataScope().BuildAndLoadRepositoriesInMemory();
    }
    public void LoadRepositories()
    {
      RepositoryBuilder.GetForDataScope().LoadRepositories();
    }
    public void BuildRepositories(bool debug)
    {
      RepositoryBuilder.GetForDataScope().BuildRepositories(debug);
    }

    public static void SetDefaultScope(string dataScopeName)
    {
      DefaultScope = new DataScope(dataScopeName);
    }

    [ThreadStatic]
    static Stack<DataScope> mScopeStack;
    static Stack<DataScope> ScopeStack
    {
      get { return mScopeStack ?? (mScopeStack = new Stack<DataScope>()); }
    }

    public static string CurrentScopeName
    {
      get { return ScopeStack.Count == 0 ? DefaultScope?.ScopeName  : ScopeStack.Peek().ScopeName; }
    }

    public static DataScope CurrentScope
    {
      get { return ScopeStack.Count == 0 ? DefaultScope : ScopeStack.Peek(); }
    }

    static Dictionary<string, DataCache> mDataCacheDictionary = new Dictionary<string, DataCache>();
  }
}
