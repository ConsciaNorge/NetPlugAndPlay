using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libterminal
{
    public class ConnectionManager
    {
        public List<Connection> Connections { get; set; } = new List<Connection>();

        public static ConnectionManager Instance = new ConnectionManager();

        public ConnectionManager()
        {
        }

        public void AddConnection(Connection connection)
        {
            // TODO : Clear old dead connections to the same destination
            lock (this) Connections.Add(connection);
        }

        public Connection ConnectionByUri(Uri destination)
        {
            Connection result = null;
            lock (this) result = Connections.Where(x => x.Destination == destination).FirstOrDefault();
            return result;
        }

        public void RemoveConnection(Connection connection)
        {
            lock(this)
            {
                Connections.Remove(connection);
                // TODO : make connection disposable
            }
        }

        public Connection Get(Guid id)
        {
            Connection result = null;
            lock (this) result = Connections.Where(x => x.Id == id).FirstOrDefault();

            return result;
        }

        public Connection ConnectTo(Uri destination)
        {
            var newConnection = new Connection();
            newConnection.Connect(destination);

            if(newConnection.IsConnected)
                return newConnection;

            return null;
        }
    }
}
