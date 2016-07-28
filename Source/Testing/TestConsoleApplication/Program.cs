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

      ObjectRepository<LedgerAccount>.Instance().SaveObjects(accounts);
    }


    static void Main(string[] args)
    {
      DataScope.DefaultScope = LocalConnectionProvider.ConnectionName;

      using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required))
      using (new ConnectionScope())
      {
        DataBuilder.DoDataStructureValidation();
        //RepositoryBuilder.GetForConnectionType().BuildRepositories(true);
        //RepositoryBuilder.GetForConnectionType().LoadRepositories();
        RepositoryBuilder.GetForConnectionType().BuildAndLoadRepositoriesInMemory();

        ts.Complete();
      }

      DataCache.InitializeForDataScope();

      DataCache<LedgerAccount>.Get().DataCacheUpdated += LedgerAccount_DataCacheUpdated;

      //using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
      //using (new ConnectionScope())
      //{
      //  ObjectCollection<LedgerAccount> accounts = ObjectRepository<LedgerAccount>.Instance().LoadObjects();
      //  ts.Complete();
      //}

      var ong = DataCache.GetObject(Guid.Parse("{cb56a582-7ec5-452d-a72f-bdfd91900a13}"));


      Console.WriteLine("Waiting...");
      Console.ReadKey();
    }

    private static void LedgerAccount_DataCacheUpdated(object sender, EventArgs e)
    {
    }
  }
}
