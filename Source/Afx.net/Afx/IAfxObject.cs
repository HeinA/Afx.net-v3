using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx
{
  public interface IAfxObject : INotifyPropertyChanged //, INotifyDataErrorInfo
  {
    Guid Id { get; set; }
    IAfxObject Owner { get; set; }
    bool IsDirty { get; set; }
  }
}
