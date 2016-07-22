using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public class AfxAssembly : IAfxAssembly
  {
    public string AssemblyId { get; private set; }
    public Assembly Assembly { get; private set; }

    public AfxAssembly(Assembly assembly, string path)
    {
      Assembly = assembly;
      Path = path;
    }

    public string Name
    {
      get { return Assembly.FullName; }
    }

    public string Path { get; private set; }

    ObservableCollection<AfxType> mAfxTypes = new ObservableCollection<AfxType>();
    public ObservableCollection<AfxType> AfxTypes
    {
      get { return mAfxTypes; }
    }

  }
}
