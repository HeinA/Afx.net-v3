using Afx;
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
using Test.Business;

namespace TestConsoleApplication
{
  class Program
  {
    static void GenerateLedgerAccounts()
    {
      ObjectCollection<LedgerAccount> accounts = new ObjectCollection<LedgerAccount>();

      for (int i = 0; i < 10; i++)
      {
        LedgerAccount a = new LedgerAccount() { Name = string.Format("Root {0}", i) };
        accounts.Add(a);
        for (int ii = 0; ii < 10; ii++)
        {
          LedgerAccount a1 = new LedgerAccount() { Name = string.Format("Child {0}.{1}", i, ii) };
          a.Accounts.Add(a1);
          for (int iii = 0; iii < 10; iii++)
          {
            LedgerAccount a2 = new LedgerAccount() { Name = string.Format("Child {0}.{1}.{2}", i, ii, iii) };
            a1.Accounts.Add(a2);
          }
        }
      }

      ObjectRepository<LedgerAccount>.Get().SaveObjects(accounts);
    }


    static void Main(string[] args)
    {
      DataScope.SetDefaultScope(LocalConnectionProvider.ConnectionName);

      using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required))
      using (new ConnectionScope())
      {
        DataBuilder.DoDataStructureValidation();
        ts.Complete();
      }

      //DataScope.CurrentScope.BuildRepositories(true);
      //DataScope.CurrentScope.LoadRepositories();

      DataScope.CurrentScope.BuildAndLoadRepositoriesInMemory();

      //using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required))
      //using (new ConnectionScope())
      //{
      //  //DataBuilder.ValidateSystemObjects();
      //  GenerateLedgerAccounts();
      //  ts.Complete();
      //}

      var dc = DataCache<LedgerAccount>.Get();
      //dc.DataCacheUpdated += LedgerAccount_DataCacheUpdated;

      //Administrator a = Administrator.Instance;

      //using (new ConnectionScope())
      //{
      //  Administrator aa = ObjectRepository<Administrator>.Get().LoadObjects()[0];
      //  Console.WriteLine(a.Equals(aa));
      //}


      //using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
      //using (new ConnectionScope())
      //{
      //  ObjectCollection<LedgerAccount> accounts = ObjectRepository<LedgerAccount>.Instance().LoadObjects();
      //  ts.Complete();
      //}

      //var ong = DataCache.GetObject<LedgerAccount>(Guid.Parse("{cb56a582-7ec5-452d-a72f-bdfd91900a13}"));

      //using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required))
      //using (new ConnectionScope())
      //{
      //  GenerateLedgerAccounts();

      //  Guid id = Guid.Parse("{E90A63F6-ED19-4523-A282-AB325F185971}");
      //  PurchaseOrder po = new PurchaseOrder() { Id = id, CustomerName = "Piet", DocumentDate = DateTime.Now, DocumentNumber = "PO0002" };
      //  ObjectRepository<PurchaseOrder>.Get().SaveObject(po);
      //  po = ObjectRepository<PurchaseOrder>.Get().LoadObject(id);
      //  ts.Complete();
      //}

      //using (new ConnectionScope())
      //{
      //  ObjectRepository<LedgerAccount>.Get().DeleteObject(ong.Accounts[0]);
      //}

      Console.WriteLine("Waiting...");
      Console.ReadKey();
    }

    private static void LedgerAccount_DataCacheUpdated(object sender, EventArgs e)
    {
    }
  }
}
