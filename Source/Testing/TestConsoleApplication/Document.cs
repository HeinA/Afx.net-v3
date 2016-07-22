using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  [Persistent]
  public abstract class Document : Afx.AfxObject
  {
    [Persistent]
    public string DocumentNumber { get; set; }

    [Persistent]
    public DateTime DocumentDate { get; set; }

    //[Persistent]
    //public ZZZ ZZZ { get; set; }
  }
}
