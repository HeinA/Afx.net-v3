using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApplication
{
  [Export("System.String, mscorlib", typeof(IDbText))]
  public class DefaultStringDataType : IDbText
  {
    public string DbText
    {
      get { return "varchar(400)"; }
    }
  }

  [Export("[Test].[Document].[DocumentNumber]", typeof(IDbText))]
  public class Document_DocumentNumber_DbType : IDbText
  {
    public string DbText
    {
      get { return "nvarchar(100)"; }
    }
  }

  [Export("[Test].[Document].[DocumentNumber]", typeof(IColumnCreated))]
  public class Document_DocumentNumber_Created : IColumnCreated
  {
    public void OnColumnCreated()
    {
    }
  }
}
