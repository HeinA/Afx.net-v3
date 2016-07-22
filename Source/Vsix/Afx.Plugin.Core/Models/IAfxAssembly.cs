using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public interface IAfxAssembly
  {
    string AssemblyId { get; }
    string Name { get; }
    string Path { get; }
    ObservableCollection<AfxType> AfxTypes { get; }
  }
}
