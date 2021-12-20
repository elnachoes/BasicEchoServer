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
        private static object lockObject;

        //list of client socket connections
        //private static List<Socket> activeConnections;
        private static List<Connection> activeConnections;

        private static Socket serverSocket;

        public Server()
        {
            activeConnections = new List<Connection>();
            lockObject = new object();
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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

                var newClient = new Connection(serverSocket);

                Console.Write("someone connected..." + NEW_LINE_SEQUENCE);
                AddClient(newClient);
            }
        }

        //this is a background thread that will listen to what clients send to the server
        private static void ListenToClient(Connection client)
        {
            //variables for transfering data
            byte[] byteBuffer = new byte[BYTE_BUFFER_SIZE];
            int bytesTransferred;

            //main listening loop that listens for bytes that come from the client
            int offset = 0;
            while (true)
            {
                var socketsToRemove = new List<Connection>();

                bytesTransferred = client.Receive(byteBuffer, offset, byteBuffer.Length - offset);
                if (bytesTransferred == 0)
                {
                    lock (lockObject)
                    {
                        activeConnections.Remove(client);
                    }
                    return;
                }
                offset += bytesTransferred;

                var carriageReturnPosition = FindCarriageReturn(byteBuffer, offset);

                //echo the input from one client to all users
                if (carriageReturnPosition > 0)
                {
                    //mutex to protect activeConnections
                    lock (lockObject)
                    {
                        foreach (var clientConnection in activeConnections)
                        {
                            if (clientConnection != client)
                            {
                                bytesTransferred = clientConnection.Send(byteBuffer, offset);
                                if (bytesTransferred == 0)
                                {
                                    activeConnections.Remove(client);
                                    continue;
                                }

                            }

                        }
                    }

                    //RemoveClients(socketsToRemove);
                    offset = 0;
                    Array.Clear(byteBuffer);
                }
            }
        }

        //send a message to all clients from the server
        private static void ServerSendMessage(string message)
        {
            foreach (var client in activeConnections)
            {
                message += CARRIAGE_RETURN_STRING;
                var sendResult = client.Send(Encoding.ASCII.GetBytes(message), message.Length);
                if (sendResult == 0)
                {
                    lock (lockObject)
                    {
                        activeConnections.Remove(client);
                    }
                }
            }
        }

        //send a message to a specific client
        private static void ServerSendMessage(Connection client, string message)
        {
            message += CARRIAGE_RETURN_STRING;
            var sendResult = client.Send(Encoding.ASCII.GetBytes(message), message.Length);
            if (sendResult == 0)
            {
                activeConnections.Remove(client);
            }
        }

        //adds a client socket and starts a thread to listen to that client
        private static void AddClient(Connection newClient)
        {
            //every time a new client joins add the socket to the activeConnections
            lock (lockObject)
            {
                activeConnections.Add(newClient);
            }

            //send the welcome message out to the new client
            ServerSendMessage(newClient, "Welcome to the chat!");

            //create a new background thread for listening to the new client
            Thread listenToClients = new Thread(() => ListenToClient(newClient));
            listenToClients.IsBackground = true;
            listenToClients.Start();
        }

        //removes a client from the collection
        private static void RemoveClients(List<Connection> clientsToRemove)
        {
            foreach (var client in clientsToRemove)
            {
                if (activeConnections.Contains(client))
                {
                    lock (lockObject)
                    {
                        activeConnections.Remove(client);
                    }
                }
            }
        }

        public void HostServer()
        {
            //bind the socket and listen
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
            serverSocket.Listen(LISTEN_COUNT);

            //server welcome message
            Console.Write("//-- echo server --//" + NEW_LINE_SEQUENCE);

            //start a separate thread for accepting clients
            Thread acceptConnectionsThread = new Thread(() => AcceptConnections());
            acceptConnectionsThread.IsBackground = true;
            acceptConnectionsThread.Start();

            //main loop for recieving server commands
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