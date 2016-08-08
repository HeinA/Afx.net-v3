using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Business
{
  //[Persistent]
  public class Role : Afx.AfxObject
  {
    #region Constructors

    protected Role()
    {
    }

    protected Role(string id)
      : base(Guid.Parse(id))
    {
    }


    #endregion

    #region string Name

    public const string NameProperty = "Name";
    string mName;
    [Persistent]
    public string Name
    {
      get { return mName; }
      set { SetProperty<string>(ref mName, value); }
    }

    #endregion
  }
}
