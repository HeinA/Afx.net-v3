using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class RegisteredType
  {
    #region Constructors

    public RegisteredType(int id, string typeName)
    {
      Id = id;
      Type = Type.GetType(typeName);
    }

    #endregion

    public int Id { get; private set; }
    public Type Type { get; private set; }
  }
}
