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
    #region Constructors

    public ConnectionScope()
      : this(DataScope.CurrentScope?.ScopeName, false)
    {
    }

    public ConnectionScope(bool forceNew)
      : this(DataScope.CurrentScope?.ScopeName, forceNew)
    {
    }

    public ConnectionScope(string connectionName, bool forceNew)
    {
      Guard.ThrowIfNullOrEmpty(connectionName, nameof(connectionName));

      ConnectionName = connectionName;
      Connection = ConnectionStack.FirstOrDefault(cs => cs.ConnectionName == ConnectionName)?.Connection;
      if (Connection == null || forceNew)
      {
        Forced = forceNew;
        var connectionProvider = Afx.ExtensibilityManager.GetObject<IConnectionProvider>(ConnectionName);
        Guard.ThrowIfNull(connectionProvider, nameof(ConnectionName), Properties.Resources.ConnectionStringNotDefined);
        connectionProvider.VerifyConnection();
        Connection = connectionProvider.GetConnection();
        Connection.Open();
      }

      ConnectionStack.Push(this);
    }

    #endregion

    public string ConnectionName { get; private set; }
    public IDbConnection Connection { get; private set; }
    bool Forced { get; set; }

    #region GetCommand()

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

    #endregion

    #region ConnectionScope CurrentScope

    public static ConnectionScope CurrentScope
    {
      get { return ConnectionStack.Count > 0 ? ConnectionStack.Peek() : null; }
    }

    #endregion

    #region Dispose()

    public void Dispose()
    {
      ConnectionScope cs = ConnectionStack.Pop();
      if (cs.Forced || !ConnectionStack.Any(cs1 => cs1.ConnectionName == ConnectionName))
      {
        Connection.Close();
      }
    }

    #endregion



    #region Stack<ConnectionScope> ConnectionStack

    [ThreadStatic]
    static Stack<ConnectionScope> mConnectionStack;
    static Stack<ConnectionScope> ConnectionStack
    {
      get { return mConnectionStack ?? (mConnectionStack = new Stack<ConnectionScope>()); }
    }

    #endregion
  }
}
