using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  [Export("System.Data.SqlClient.SqlConnection, System.Data", typeof(IRegisteredTypeLoader))]
  public class MsSqlRegisteredTypeLoader : IRegisteredTypeLoader
  {
    #region LoadTypes

    public RegisteredType[] LoadTypes()
    {
      List<RegisteredType> list = new List<RegisteredType>();
      using (var cmd = ConnectionScope.CurrentScope.GetCommand("SELECT * FROM [Afx].[RegisteredType]"))
      {
        DataSet ds = ExecuteDataSet(cmd);
        foreach (DataRow dr in ds.Tables[0].Rows)
        {
          list.Add(new RegisteredType((int)dr["id"], (string)dr["AssemblyFullName"]));
        }
      }
      return list.ToArray();
    }

    #endregion

    #region ExecuteDataSet

    public static DataSet ExecuteDataSet(IDbCommand cmd)
    {
      System.Data.DataSet ds = new System.Data.DataSet();
      ds.EnforceConstraints = false;
      ds.Locale = CultureInfo.InvariantCulture;
      using (IDataReader r = cmd.ExecuteReader())
      {
        ds.Load(r, LoadOption.OverwriteChanges, string.Empty);
        r.Close();
      }

      return ds;
    }

    #endregion
  }
}
