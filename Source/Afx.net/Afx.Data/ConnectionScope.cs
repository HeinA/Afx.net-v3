using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data
{
  public class ConnectionScope : IDisposable
  {
    public ConnectionScope()
      : this(DataScope.CurrentScope)
    {
    }

    public ConnectionScope(string connectionName)
    {
      Guard.ThrowIfNullOrEmpty(connectionName, nameof(connectionName));

      ConnectionName = connectionName;

      Connection = Afx.ExtensibilityManager.GetObject<IDbConnection>(ConnectionName);
      Guard.ThrowIfNull(Connection, nameof(ConnectionName), Properties.Resources.ConnectionStringNotDefined);

      Connection.Open();
      ConnectionStack.Push(this);
    }

    public string ConnectionName { get; private set; }
    public IDbConnection Connection { get; private set; }

    public IDbCommand GetCommand()
    {
      string connectionType = Connection.GetType().AfxTypeName();
      IDbCommand cmd = Afx.ExtensibilityManager.GetObject<ICommandProvider>(connectionType).GetCommand();
      cmd.Connection = Connection;
      return cmd;
    }

    public IDbCommand GetCommand(string commandText)
    {
      IDbCommand cmd = GetCommand();
      cmd.CommandText = commandText;
      return cmd;
    }

    public void Dispose()
    {
      ConnectionStack.Pop();
      Connection.Close();
    }

    public static ConnectionScope CurrentScope
    {
      get { return ConnectionStack.Count > 0 ? ConnectionStack.Peek() : null; }
    }

    [ThreadStatic]
    static Stack<ConnectionScope> mConnectionStack;
    static Stack<ConnectionScope> ConnectionStack
    {
      get { return mConnectionStack ?? (mConnectionStack = new Stack<ConnectionScope>()); }
    }
  }
}
