using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  [Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(IObjectWriterLoader))]
  public class MsSqlObjectWriterLoader : IObjectWriterLoader
  {
    public ObjectWriter[] LoadObjectWriters()
    {
      List<ObjectWriter> types = new List<ObjectWriter>();
      Afx.ExtensibilityManager.PreLoadDeployedAssemblies(); //TODO: MsSql bin folder
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        try
        {
          if (assembly.GetName().Name == "TestConsoleApplication")
          {
          }

          foreach (var type in assembly.GetTypes())
          {
            var writer = type.GetGenericSubClass(typeof(MsSqlObjectWriter<>));
            if (writer != null && !type.IsAbstract)
            {
              try
              {
                types.Add((ObjectWriter)Activator.CreateInstance(type));
              }
              catch
              {
                throw;
              }
            }
          }
        }
        catch
        {
          throw;
        }
      }
      return types.ToArray();
    }
  }
}
