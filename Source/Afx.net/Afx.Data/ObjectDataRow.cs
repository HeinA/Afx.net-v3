using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class ObjectDataRow
  {
    public ObjectDataRow(DataRow dr)
    {
      Id = (Guid)dr["id"];
      if (dr.Table.Columns.Contains("Owner") && dr["Owner"] != DBNull.Value) Owner = (Guid)dr["Owner"];
      Type = DataScope.DefaultScope.RegisteredTypes.Where(rt => rt.Id.Equals(dr["RegisteredType"])).First().Type;
      DataRow = dr;
    }

    public Guid Id { get; private set; }
    public Guid? Owner { get; private set; }
    public Type Type { get; private set; }
    public DataRow DataRow { get; private set; }
    public IAfxObject Instance { get; set; }
  }
}
