using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  [AttributeUsage(AttributeTargets.Class)]
  public class OrderByAttribute : Attribute
  {
    public OrderByAttribute(params string[] properties)
    {
      foreach (var p in properties)
      {
        mProperties.Add(p);
      }
    }

    List<string> mProperties = new List<string>();
    public IEnumerable<string> Properties
    {
      get { return mProperties.Cast<string>(); }
    }
  }
}
