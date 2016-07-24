using Afx.Collections;
using Afx.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace TestConsoleApplication
{
  class Program
  {
    static void Main(string[] args)
    {
      using (new DataScope(LocalDataProvider.ConnectionName))
      {
        using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required))
        using (new ConnectionScope())
        {
          DataBuilder.DoRepositoryValidation();

          ts.Complete();
        }

        using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
        using (new ConnectionScope())
        {
          ObjectCollection<LedgerAccount> accounts = new ObjectCollection<LedgerAccount>();
          //for (int i = 0; i < 100; i++)
          //{
          //  LedgerAccount a = new LedgerAccount() { Name = string.Format("Root {0}", i) };
          //  accounts.Add(a);
          //  for (int ii = 0; ii < 10; ii++)
          //  {
          //    LedgerAccount a1 = new LedgerAccount() { Name = string.Format("Child {0}.{1}", i, ii) };
          //    a.Accounts.Add(a1);
          //    for (int iii = 0; iii < 10; iii++)
          //    {
          //      LedgerAccount a2 = new LedgerAccount() { Name = string.Format("Child {0}.{1}.{2}", i, ii, iii) };
          //      a1.Accounts.Add(a2);
          //    }
          //  }
          //}
          Stopwatch sw = new Stopwatch();
          sw.Start();
          accounts = ObjectRepository.GetRepository<LedgerAccount>().LoadObjects();
          Console.WriteLine(sw.ElapsedMilliseconds);
          sw.Restart();
          ObjectRepository.GetRepository<LedgerAccount>().SaveObjects(accounts);
          Console.WriteLine(sw.ElapsedMilliseconds);
          //accounts = ObjectRepository.GetRepository<LedgerAccount>().LoadObjects();

          //PurchaseOrder po = ObjectRepository.GetRepository<PurchaseOrder>().LoadObject(Guid.Parse("{7DCB388B-E72C-42D6-B290-89D6EEE7BC4B}"));
          //po.DocumentNumber = "PO0001";
          ////po.Items.Add(new InventoryItem("42231f4c-7180-4b36-8012-778f0d098335"));
          ////po.Items.Clear();

          //ObjectRepository.GetRepository<Document>().SaveObject(po);

          //Document d = ObjectRepository.GetRepository<Document>().LoadObject(po.Id);

          ts.Complete();
        }

        using (new ConnectionScope())
        {
          IDbCommand cmd = ConnectionScope.CurrentScope.GetCommand("SELECT 1");
          object o = cmd.ExecuteScalar();
        }
      }

      Console.WriteLine("Waiting...");
      Console.ReadKey();
    }
  }
}
