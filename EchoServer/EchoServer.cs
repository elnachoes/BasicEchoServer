using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EchoServer
{
    public class Server
    {
        private const byte CARRIAGE_RETURN_DECIMAL = 13;
        private const string CARRIAGE_RETURN_STRING = "\r\n";
        private const string NEW_LINE_SEQUENCE = "\n</> ";
        private const int SERVER_PORT = 11000;
        private const int LISTEN_COUNT = 99;
        private const int BYTE_BUFFER_SIZE = 1024;

        //mutex object
        private static object _lockObject;

        //list of client socket connections
        //private static List<Socket> activeConnections;
        private static List<Connection> _activeConnections;

        private static Socket _serverSocket;

        public Server()
        {
            _activeConnections = new List<Connection>();
            _lockObject = new object();
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }


        //This just returns the position in a byte array for where the carriage return symbole is
        private static int FindCarriageReturn(byte[] buffer, int size)
        {
            for (var i = 0; i < size; i++)
            {
                if (buffer[i] == CARRIAGE_RETURN_DECIMAL)
                {
                    return i + 2;
                }
            }
            return -1;
        }

        // This function is ment to be running constantly as a backround thread
        // It will keep searching for new guests to connect to the server
        private static void AcceptConnections()
        {
            while (true)
            {
                //wait for someone to connect to the server
                Console.Write("Waiting for a connection..." + NEW_LINE_SEQUENCE);
                //var clientSocket = serverSocket.Accept();

                var newClientSocket = _serverSocket.Accept();

                var newClient = new Connection(newClientSocket, RemoveClient, ReceiveCallback);

                Console.Write("someone connected..." + NEW_LINE_SEQUENCE);
                AddClient(newClient);
            }
        }

        private static void ReceiveCallback(byte[] buffer, int bytesReceived, Connection connection)
        {
            connection._buffer = buffer;
            connection._offset += bytesReceived;
            connection._size = connection._buffer.Length - connection._offset;

            var carriageReturnPosition = FindCarriageReturn(connection._buffer, connection._offset);

            if (carriageReturnPosition > 0)
            {
                //mutex to protect activeConnections
                lock (_lockObject)
                {
                    foreach (var clientConnection in _activeConnections)
                    {
                        if (clientConnection != connection)
                        {
                            var bytesTransferred = clientConnection.Send(connection._buffer, connection._offset);
                            if (!connection._isConnected)
                            {
                                continue;
                            }
                        }
                    }
                }

                connection._offset = 0;
                Array.Clear(connection._buffer);
            }
        }

        //send a message to all clients from the server
        private static void ServerSendMessage(string message)
        {
            foreach (var client in _activeConnections)
            {
                message += CARRIAGE_RETURN_STRING;
                var sendResult = client.Send(Encoding.ASCII.GetBytes(message), message.Length);
            }
        }

        //send a message to a specific client
        private static void ServerSendMessage(Connection client, string message)
        {
            message += CARRIAGE_RETURN_STRING;
            var sendResult = client.Send(Encoding.ASCII.GetBytes(message), message.Length);
        }

        //adds a client socket and starts a thread to listen to that client
        private static void AddClient(Connection newClient)
        {
            //every time a new client joins add the socket to the activeConnections
            lock (_lockObject)
            {
                _activeConnections.Add(newClient);
            }

            //send the welcome message out to the new client
            ServerSendMessage(newClient, "Welcome to the chat!");

            var buffer = new byte[BYTE_BUFFER_SIZE];
            var offset = 0;

            newClient.StartReceiving(buffer, offset, buffer.Length);
        }

        //removes a client from the collection
        private static void RemoveClients(List<Connection> clientsToRemove)
        {
            foreach (var client in clientsToRemove)
            {
                if (_activeConnections.Contains(client))
                {
                    lock (_lockObject)
                    {
                        _activeConnections.Remove(client);
                    }
                }
            }
        }

        private static void RemoveClient(Connection client)
        {
            if (_activeConnections.Contains(client))
            {
                lock (_lockObject)
                {
                    _activeConnections.Remove(client);
                }
            }
        }

        public void HostServer()
        {
            //bind the socket and listen
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
            _serverSocket.Listen(LISTEN_COUNT);

            //server welcome message
            Console.Write("//-- echo server --//" + NEW_LINE_SEQUENCE);

            //start a separate thread for accepting clients
            var acceptConnectionsThread = new Thread(() => AcceptConnections());
            acceptConnectionsThread.IsBackground = true;
            acceptConnectionsThread.Start();

            //main loop for receiving server commands
            while (true)
            {
                //if the user types in exit in the server terminal it will shutdown the server
                var userInput = Console.ReadLine();
                if (userInput == "exit")
                {
                    ServerSendMessage("Server shutting down");
                    return;
                }
            }
        }
    }
}