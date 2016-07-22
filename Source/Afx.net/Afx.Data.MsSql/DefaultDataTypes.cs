using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.MsSql
{
  [Export("System.String, mscorlib", typeof(IDefaultDbText))]
  public class DefaultStringDataType : IDefaultDbText
  {
    public string DbText { get { return "varchar(200)"; } }
  }

  [Export("System.Guid, mscorlib", typeof(IDefaultDbText))]
  public class DefaultGuidDataType : IDefaultDbText
  {
    public string DbText { get { return "uniqueidentifier"; } }
  }

  [Export("System.DateTime, mscorlib", typeof(IDefaultDbText))]
  public class DefaultDateDataType : IDefaultDbText
  {
    public string DbText { get { return "datetime"; } }
  }

  [Export("System.Boolean, mscorlib", typeof(IDefaultDbText))]
  public class DefaultBoolDataType : IDefaultDbText
  {
    public string DbText { get { return "bit"; } }
  }
}

