using Afx.Data;
using System;
using System.Collections.Generic;
using System.Data;
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
