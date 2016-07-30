using Afx.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Business
{
  [Persistent]
  [OrderBy("DocumentNumber DESC")]
  public abstract class Document : Afx.AfxObject
  {
    #region string DocumentNumber

    public const string DocumentNumberProperty = nameof(DocumentNumber);
    string mDocumentNumber;
    [Persistent]
    public string DocumentNumber
    {
      get { return mDocumentNumber; }
      set { SetProperty<string>(ref mDocumentNumber, value); }
    }

    #endregion

    #region DateTime DocumentDate

    public const string DocumentDateProperty = nameof(DocumentDate);
    DateTime mDocumentDate;
    [Persistent]
    public DateTime DocumentDate
    {
      get { return mDocumentDate; }
      set { SetProperty<DateTime>(ref mDocumentDate, value); }
    }

    #endregion
  }
}
