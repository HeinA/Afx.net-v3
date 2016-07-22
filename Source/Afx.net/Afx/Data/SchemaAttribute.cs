using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  [AttributeUsage(AttributeTargets.Assembly)]
  public class SchemaAttribute : Attribute
  {
    public SchemaAttribute(string schemaName)
    {
      Guard.ThrowIfNullOrEmpty(schemaName, nameof(schemaName));
      SchemaName = schemaName;
    }

    public string SchemaName { get; private set; }
  }
}
