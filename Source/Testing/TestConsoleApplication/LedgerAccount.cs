using Afx.Collections;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  [Persistent]
  public class LedgerAccount : Afx.AfxObject<LedgerAccount>
  {
    #region Constructors

    public LedgerAccount()
    {
    }

    public LedgerAccount(Guid id)
      : base(id)
    {
    }

    public LedgerAccount(string id)
      : base(id)
    {
    }

    #endregion

    [Persistent]
    public string Name { get; set; }

    public ObjectCollection<LedgerAccount> mAccounts;
    public ObjectCollection<LedgerAccount> Accounts
    {
      get { return GetObjectCollection<LedgerAccount>(ref mAccounts); }
    }
  }
}
