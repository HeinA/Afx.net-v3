using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx
{
  public static class Guard
  {
    public static void ThrowIfNull(object argument, string argumentName)
    {
      if (argument == null) throw new ArgumentNullException(argumentName);
    }

    public static void ThrowIfNull(object argument, string argumentName, string message, params object[] args)
    {
      if (argument == null) throw new ArgumentException(string.Format(message, args), argumentName);
    }

    public static void ThrowOperationExceptionIfNull(object argument, string message, params object[] args)
    {
      if (argument == null) throw new InvalidOperationException(string.Format(message, args));
    }

    public static void ThrowIfNullOrEmpty(string argument, string argumentName)
    {
      if (argument == null) throw new ArgumentNullException(argumentName);
      if (string.IsNullOrWhiteSpace(argument)) throw new ArgumentOutOfRangeException(argumentName);
    }
  }
}
