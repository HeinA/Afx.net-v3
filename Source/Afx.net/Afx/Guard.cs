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

    public static void ThrowIfNull(object argument, string argumentName, string message)
    {
      if (argument == null) throw new ArgumentException(message, argumentName);
    }

    public static void ThrowOperationExceptionIfNull(object argument, string message)
    {
      if (argument == null) throw new InvalidOperationException(message);
    }

    public static void ThrowIfNullOrEmpty(string argument, string argumentName)
    {
      if (argument == null) throw new ArgumentNullException(argumentName);
      if (string.IsNullOrWhiteSpace(argument)) throw new ArgumentOutOfRangeException(argumentName);
    }
  }
}
