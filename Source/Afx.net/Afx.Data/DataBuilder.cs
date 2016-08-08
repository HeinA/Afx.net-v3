﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class DataBuilder
  {
    public static void DoDataStructureValidation()
    {
      Guard.ThrowOperationExceptionIfNull(ConnectionScope.CurrentScope, Properties.Resources.NoConnectionScope);
      IDataBuilder builder = Afx.ExtensibilityManager.GetObject<IDataBuilder>(ConnectionScope.CurrentScope.Connection.GetType().AfxTypeName());
      builder.ValidateDataStructure();
    }

    public static void ValidateSystemObjects()
    {
      //foreach (var obj in Afx.ExtensibilityManager.GetObjects<IAfxObject>())
      //{
      //  ObjectRepository.RepositoryInterfaceFor(obj.GetType()).SaveObjectCore(obj, new SaveContext());
      //}
    }


    public static void BuildAndLoadRepositoriesInMemory()
    {
    }

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
  }
}
