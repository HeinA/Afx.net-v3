using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Business;
using Afx.Data;
using System.Collections.ObjectModel;

namespace TestConsoleApplication
{
  public class LedgerAccountStore : MsSqlAggregateCollectionRepository<LedgerAccount>
  {
    protected override string AggregateSelectsForObjects
    {
      get
      {
        return string.Join("; ", typeof(LedgerAccount).AfxSqlAggregateSelects(SelectionType.All));
      }
    }

    protected override IEnumerable<LedgerAccount> GetObjects(ObjectDataRowCollection rows)
    {
      foreach (var row in rows.Where(r => typeof(LedgerAccount).IsAssignableFrom(r.Type)).OrderBy(r => r.DataRow["Name"]))
      {
        if (row.Instance == null) GetObjectDataConverter(row.Type).WriteObject(row, rows);
        yield return (LedgerAccount)row.Instance;
      }
    }
  }
}
