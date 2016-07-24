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
    protected Document()
    {
    }

    protected Document(Guid id)
      : base(id)
    {
    }

    protected Document(string id)
      : base(id)
    {
    }

    [Persistent]
    public string DocumentNumber { get; set; }

    [Persistent]
    public DateTime DocumentDate { get; set; }
  }
}
