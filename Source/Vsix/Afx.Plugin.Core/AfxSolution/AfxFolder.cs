using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.AfxSolution
{
  public class AfxFolder : Afx.Plugin.Model
  {
    public AfxFolder(string text, IList items)
    {
      Text = text;
      Items = items;
    }

    public string Text { get; private set; }
    public IList Items { get; private set; }
  }
}
