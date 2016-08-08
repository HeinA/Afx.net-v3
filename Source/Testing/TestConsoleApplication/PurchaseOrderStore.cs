using Afx.Data;
using Afx.Data.MsSql;
using System.Linq;

namespace TestConsoleApplication
{
  public class PurchaseOrderStore : MsSqlAggregateObjectRepository<Test.Business.PurchaseOrder>
  {
    public override AggregateObjectQuery<Test.Business.PurchaseOrder> Query(string conditions)
    {
      return new MsSqlAggregateObjectQuery<Test.Business.PurchaseOrder>(this, conditions);
    }

    protected override string AggregateSelectsForObject
    {
      get
      {
        return string.Join("; ", typeof(Test.Business.PurchaseOrder).AfxSqlAggregateSelects(SelectionType.Id));
      }
    }

    protected override string AggregateSelectsForQuery
    {
      get
      {
        return string.Join("; ", typeof(Test.Business.PurchaseOrder).AfxSqlAggregateSelects(SelectionType.Query));
      }
    }

    protected override System.Collections.Generic.IEnumerable<Test.Business.PurchaseOrder> GetObjects(ObjectDataRowCollection rows)
    {
      foreach (var row in rows.Where(r => typeof(Test.Business.PurchaseOrder).IsAssignableFrom(r.Type)))
      {
        if (row.Instance == null) GetObjectDataConverter(row.Type).WriteObject(row, rows);
        yield return (Test.Business.PurchaseOrder)row.Instance;
      }
    }
  }
}
