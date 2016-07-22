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

      //if (ConnectionDictionary.ContainsKey(ConnectionName))
      //{
      //  ConnectionStack.Push(this);
      //  return;
      //}

      Connection = Afx.ExtensibilityManager.GetObject<IDbConnection>(ConnectionName);
      Guard.ThrowIfNull(Connection, nameof(ConnectionName), Properties.Resources.ConnectionStringNotDefined);

      Connection.Open();
      //ConnectionDictionary.Add(ConnectionName, Connection);
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
      //if (!ConnectionStack.Any(cs => cs.ConnectionName.Equals(ConnectionName))) 
      //{
      //  IDbConnection connection = ConnectionDictionary[ConnectionName];
      //  ConnectionDictionary.Remove(ConnectionName);
      //  connection.Close();
      //}
    }

    public static ConnectionScope CurrentScope
    {
      get { return ConnectionStack.Count > 0 ? ConnectionStack.Peek() : null; }
    }

    //public static IDbConnection GetConnection(string connectionName)
    //{
    //  Guard.ThrowIfNullOrEmpty(connectionName, nameof(connectionName));
    //  if (!ConnectionDictionary.ContainsKey(connectionName)) throw new ArgumentException(Properties.Resources.NoSuchConnectionScope, nameof(connectionName));
    //  return ConnectionDictionary[connectionName];
    //}

    //[ThreadStatic]
    //static Dictionary<string, IDbConnection> mConnectionDictionary;
    //static Dictionary<string, IDbConnection> ConnectionDictionary
    //{
    //  get { return mConnectionDictionary ?? (mConnectionDictionary = new Dictionary<string, IDbConnection>()); }
    //}

    [ThreadStatic]
    static Stack<ConnectionScope> mConnectionStack;
    static Stack<ConnectionScope> ConnectionStack
    {
      get { return mConnectionStack ?? (mConnectionStack = new Stack<ConnectionScope>()); }
    }
  }
}
