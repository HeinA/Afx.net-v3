using Afx;
using Afx.Collections;
using Afx.Data;
using Afx.Data.MsSql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
        for (int ii = 0; ii < 100; ii++)
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

      DataScope.GetCollectionRepository<LedgerAccount>().Save(accounts);
    }


    static void Main(string[] args)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();

      DataScope.SetDefaultScope(LocalConnectionProvider.ConnectionName);

      //using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required))
      //using (new ConnectionScope())
      //{
      //  DataScope.CurrentScope.ValidateDataStructure();
      //  ts.Complete();
      //}

      //DataScope.CurrentScope.BuildRepositoriesRelease();
      //Console.WriteLine("RepositoryFactory.Build {0}", sw.ElapsedMilliseconds);
      //sw.Restart();

      var dr = DataScope.GetObjectRepository<Document>();
      Console.WriteLine("GetObjectRepository<Document>()  {0}", sw.ElapsedMilliseconds);
      sw.Restart();

      dr = DataScope.GetObjectRepository<Document>();
      Console.WriteLine("GetObjectRepository<Document>()  {0}", sw.ElapsedMilliseconds);
      sw.Restart();

      //var c = DataScope.CurrentScope.DataCache;

      var li = DataScope.GetObjects<LedgerAccount>().LastOrDefault();
      Console.WriteLine("GetObjects<LedgerAccount>()  {0}", sw.ElapsedMilliseconds);
      sw.Restart();

      li = DataScope.GetObjects<LedgerAccount>().FirstOrDefault();
      Console.WriteLine("GetObjects<LedgerAccount>()  {0}", sw.ElapsedMilliseconds);
      sw.Restart();


      //var obs = DataCache<LedgerAccount>.GetObjects<LedgerAccount>();


      //List<PurchaseOrder> list = new List<PurchaseOrder>();
      //PurchaseOrderStore pos = new PurchaseOrderStore();
      //LedgerAccountStore las = new LedgerAccountStore();

      using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
      using (var cs = new ConnectionScope())
      {
        try
        {
          //GenerateLedgerAccounts();
          //Console.WriteLine(sw.ElapsedMilliseconds);
          //sw.Restart();

          //ObjectCollection<LedgerAccount> col = DataScope.GetCollectionRepository<LedgerAccount>().LoadCollection();
          //col.RemoveAt(0);
          //DataScope.GetCollectionRepository<LedgerAccount>().Save(col);

          PurchaseOrder po = (PurchaseOrder)dr.Load(Guid.Parse("e90a63f6-ed19-4523-a282-ab325f185971"));
          po.SourceAccount = DataScope.GetObjects<LedgerAccount>().LastOrDefault();
          dr.Save(po);

          Console.WriteLine("PurchaseOrder Load  {0}", sw.ElapsedMilliseconds);
          sw.Restart();

          //po = pos.Load(Guid.Parse("e90a63f6-ed19-4523-a282-ab325f185971"));
          //Console.WriteLine(sw.ElapsedMilliseconds);
          //sw.Restart();

          //po.CustomerName = "Hein";
          //po.Items[0].Name = "Wheat";
          //pos.Save(po);


          var orders = DataScope.GetObjectRepository<PurchaseOrder>().Query("CustomerName != null & Items.Reference.Name starts with @p1")
                      .AddParameter("@p1", "wh")
                      .Submit();
          Console.WriteLine("Query {0}", sw.ElapsedMilliseconds);
          sw.Restart();

          //LedgerAccount[] acts = las.LoadCollection();
          //Console.WriteLine(sw.ElapsedMilliseconds);
          //sw.Restart();

          //acts[0].Accounts.RemoveAt(0);
          //las.Save(acts);
          //Console.WriteLine(sw.ElapsedMilliseconds);
          //sw.Restart();

          ts.Complete();
        }
        catch
        {
          throw;
        }
      }



      //DataScope.CurrentScope.BuildRepositories(true);
      //DataScope.CurrentScope.LoadRepositories();

      //DataScope.CurrentScope.BuildAndLoadRepositoriesInMemory();

      //using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
      //using (new ConnectionScope())
      //{
      //  //DataBuilder.ValidateSystemObjects();
      //  GenerateLedgerAccounts();
      //  ts.Complete();
      //}

      //Type t = typeof(InventoryItem).AfxAggregateType();

      //var dc = DataCache<LedgerAccount>.Get();
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
      //  //GenerateLedgerAccounts();
      //  var ii = new InventoryItem() { Name = "dsfg" };
      //  ObjectRepository<InventoryItem>.Get().SaveObject(ii);

      //  Guid id = Guid.Parse("{E90A63F6-ED19-4523-A282-AB325F185971}");
      //  PurchaseOrder po = new PurchaseOrder() { Id = id, CustomerName = "Piet", DocumentDate = DateTime.Now, DocumentNumber = "PO0002" };
      //  po.Items.Add(ii);
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
