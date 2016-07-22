using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Plugin
{
  public class Model : INotifyPropertyChanged
  {
    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
      if (typeof(T).IsValueType && Object.Equals(storage, value)) return false;
      else if ((object)storage == (object)value) return false;

      storage = value;
      this.OnPropertyChanged(propertyName);

      return true;
    }

    #endregion
  }
}
