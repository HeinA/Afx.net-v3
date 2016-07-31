using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  public class PropertyPath : IDisposable
  {
    public PropertyPath(string name)
    {
      mPathStack.Push(name);
    }

    static Stack<string> mPathStack = new Stack<string>();

    public static string GetFullName(string propertyName)
    {
      return string.Join(".", mPathStack.Union(new string[] { propertyName.Replace("[", string.Empty).Replace("]", string.Empty).ColumnName() }));
    }

    public static string GetPath()
    {
      return string.Join(".", mPathStack);
    }

    public static string GetPreviousPath()
    {
      if (mPathStack.Count <= 1)
      {
        return string.Empty;
      }
      string s = string.Join(".", mPathStack);
      return s.Substring(0, s.LastIndexOf('.') - 1);
    }

    public void Dispose()
    {
      mPathStack.Pop();
    }
  }
}
