using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Afx.Plugin.Behaviors
{
  public static class WindowExtension
  {
    public static readonly DependencyProperty DialogResultProperty =
        DependencyProperty.RegisterAttached(
            "DialogResult",
            typeof(bool?),
            typeof(WindowExtension),
            new PropertyMetadata(DialogResultChanged));

    private static void DialogResultChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
      var window = d as Window;
      if (window != null)
        window.DialogResult = e.NewValue as bool?;
    }

    public static void SetDialogResult(Window target, bool? value)
    {
      target.SetValue(DialogResultProperty, value);
    }
  }
}
