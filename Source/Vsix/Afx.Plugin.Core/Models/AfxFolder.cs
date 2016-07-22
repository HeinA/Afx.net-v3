using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin.Models
{
  public abstract class AfxFolder : INotifyPropertyChanged
  {
    public AfxProject AfxProject { get; private set; }
    public string Name { get; private set; }

    internal AfxFolder(string name, AfxProject project)
    {
      Name = name;
      AfxProject = project;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected internal virtual void OnRefresh()
    {
    }

    protected void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
