using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EchoServer
{
    public class Server
    {
        private const string NEW_LINE_SEQUENCE = "\n</> ";
        private const int SERVER_PORT = 11000;
        private const int LISTEN_COUNT = 99;

        //mutex object
        private object _lockObject = new();

        //list of client socket connections
        private List<Connection> _activeConnections = new();

        private Socket _serverSocket = null;

        public Server()
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        // This function is ment to be running constantly as a backround thread
        // It will keep searching for new guests to connect to the server
        private void AcceptConnections()
        {
            while (true)
            {
                //wait for someone to connect to the server
                Console.Write("Waiting for a connection..." + NEW_LINE_SEQUENCE);

                var newClientSocket = _serverSocket.Accept();

                var newClient = new Connection(newClientSocket, RemoveClient, ReceiveCallback);
                newClient.StartReceiving();

                //send the welcome message out to the new client
                SendMessageToClient(newClient, "Welcome to the chat!");

                Console.Write($"someone connected from: {newClientSocket.RemoteEndPoint}{NEW_LINE_SEQUENCE}");
                AddClient(newClient);
            }
        }

        private void ReceiveCallback(Connection connection, string data)
        {
            Console.WriteLine($"Server Received: {data}");

            //mutex to protect activeConnections
            lock (_lockObject)
            {
                foreach (var clientConnection in _activeConnections)
                {
                    if (clientConnection != connection)
                        clientConnection.Send(data);
                }
            }
        }

        //send a message to all clients from the server
        private void BroadcastMessage(string message)
        {
            var connectionCopy = new List<Connection>();
            lock (_lockObject)
            {
                connectionCopy.AddRange(_activeConnections);
            }

            foreach (var client in connectionCopy)
            {
                client.Send(message);
            }
        }

        //send a message to a specific client
        private static void SendMessageToClient(Connection client, string message)
        {
            client.Send(message);
        }

        //adds a client socket and starts a thread to listen to that client
        private void AddClient(Connection newClient)
        {
            //every time a new client joins add the socket to the activeConnections
            lock (_lockObject)
            {
                _activeConnections.Add(newClient);
            }
        }

        private void RemoveClient(Connection client)
        {
            lock (_lockObject)
            {
                _activeConnections.Remove(client);
            }
        }

        public void HostServer()
        {
            //bind the socket and listen
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
            _serverSocket.Listen(LISTEN_COUNT);

            //server welcome message
            Console.Write($"//-- echo server --//{NEW_LINE_SEQUENCE}");

            //start a separate thread for accepting clients
            var acceptConnectionsThread = new Thread(() => AcceptConnections());
            acceptConnectionsThread.IsBackground = true;
            acceptConnectionsThread.Start();

            //main loop for receiving server commands
            while (true)
            {
                //if the user types in exit in the server terminal it will shutdown the server
                var userInput = Console.ReadLine();
                if (userInput.ToLower().Trim() == "exit")
                {
                    BroadcastMessage("Server shutting down");
                    return;
                }
                else
                {
                    BroadcastMessage($"ServerBroadcast: {userInput}");
                }
            }
        }
    }
}