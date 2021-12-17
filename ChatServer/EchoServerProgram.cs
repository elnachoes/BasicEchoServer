using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class EchoServerProgram
{
    const byte CARRIAGE_RETURN_DECIMAL = 13;
    const string CARRIAGE_RETURN_STRING = "\r\n";
    const string NEW_LINE_SEQUENCE = "\n</> ";
    const int SERVER_PORT = 11000;
    const int LISTEN_COUNT = 99;
    const int BYTE_BUFFER_SIZE = 1024;

    //mutex object
    static object lockObject = new object();

    //list of client socket connections
    static List<Socket> activeConnections = new List<Socket>();

    //This just returns the position in a byte array for where the carriage return symbole is
    private static int FindCarriageReturn(byte[] buffer, int size)
    {
        for(var i = 0; i < size; i++)
        {
            if(buffer[i] == CARRIAGE_RETURN_DECIMAL)
            {
                return i + 2;
            }
        }
        return -1;
    }

    // This function is ment to be running constantly as a backround thread
    // It will keep searching for new guests to connect to the server
    private static void AcceptConnections(Socket serverSocket)
    {
        while (true)
        {
            //wait for someone to connect to the server
            Console.Write("Waiting for a connection..." + NEW_LINE_SEQUENCE);
            var clientSocket = serverSocket.Accept();
            Console.Write("someone connected..." + NEW_LINE_SEQUENCE);
            AddClient(clientSocket);
        }
    }

    //this is a background thread that will listen to what clients send to the server
    private static void ListenToClients(Socket clientSocket)
    {
        //variables for transfering data
        byte[] byteBuffer = new byte[BYTE_BUFFER_SIZE];
        int bytesTransferred;

        //main listening loop that listens for bytes that come from the client
        int offset = 0;
        while (true)
        {
            var socketsToRemove = new List<Socket>();

            //waits for a message from the user
            //if the socket is disconnected then add them to the socket removal list and remove them
            try
            {
                bytesTransferred = clientSocket.Receive(byteBuffer, offset, byteBuffer.Length - offset, SocketFlags.None);
                offset += bytesTransferred;
                if (bytesTransferred == 0) 
                {
                    socketsToRemove.Add(clientSocket);
                    RemoveClients(socketsToRemove);
                    return;
                }
            }
            catch (Exception)
            {
                socketsToRemove.Add(clientSocket);
                RemoveClients(socketsToRemove);
                return;
            }

            var carriageReturnPosition = FindCarriageReturn(byteBuffer, offset);

            //echo the input from one client to all users
            if (carriageReturnPosition > 0)
            {
                socketsToRemove.Clear();

                //mutex to protect activeConnections
                lock (lockObject)
                {
                    foreach (var client in activeConnections)
                    {
                        //this if statement prevents sending the message out to the client that sent the message
                        if (client != clientSocket)
                        {
                            //if the socket is disconnected then add them to the socket removal list and remove them
                            try
                            {
                                bytesTransferred = client.Send(byteBuffer, carriageReturnPosition, SocketFlags.None);
                                if (bytesTransferred == 0)
                                {
                                    socketsToRemove.Add(client);
                                }
                            }
                            catch (Exception)
                            {
                                socketsToRemove.Add(client);
                            }
                        }
                    }
                }
                RemoveClients(socketsToRemove);
                offset = 0;
                Array.Clear(byteBuffer);
            }
        }
    }

    //send a message to all clients from the server
    private static void ServerSendMessage(string message)
    {
        var socketsToRemove = new List<Socket>();

        foreach (var client in activeConnections)
        {
            message += CARRIAGE_RETURN_STRING;

            //if the socket is disconnected then add them to the socket removal list and remove them
            try
            {
                var bytesTransferred = client.Send(Encoding.ASCII.GetBytes(message));
                if (bytesTransferred == 0)
                {
                    socketsToRemove.Add(client);
                }
            }
            catch (Exception)
            {
                socketsToRemove.Add(client);
            }            
        }
        RemoveClients(socketsToRemove);
    }

    //send a message to a specific client
    private static void ServerSendMessage(Socket clientSocket,string message)
    {
        message += CARRIAGE_RETURN_STRING;

        var socketsToRemove = new List<Socket>();

        //if the socket is disconnected then add them to the socket removal list and remove them
        try
        {
            var bytesTransferred = clientSocket.Send(Encoding.ASCII.GetBytes(message));
            if (bytesTransferred == 0)
            {
                socketsToRemove.Add(clientSocket);
            }
        }
        catch (Exception)
        {
            socketsToRemove.Add(clientSocket);
        }
        RemoveClients(socketsToRemove);
    }

    //adds a client socket and starts a thread to listen to that client
    private static void AddClient(Socket clientSocket)
    {
        //every time a new client joins add the socket to the activeConnections
        lock (lockObject)
        {
            activeConnections.Add(clientSocket);
        }

        //send the welcome message out to the new client
        ServerSendMessage(clientSocket, "Welcome to the chat!");

        //create a new background thread for listening to the new client
        Thread listenToClients = new Thread(() => ListenToClients(clientSocket));
        listenToClients.IsBackground = true;
        listenToClients.Start();
    }

    //removes a client from the collection
    private static void RemoveClients(List<Socket> clientsToRemove)
    {
        foreach (Socket client in clientsToRemove)
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

    private static void Main()
    {
        //set up socket
        var ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //bind the socket and listen
        ServerSocket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
        ServerSocket.Listen(LISTEN_COUNT);

        //start a separate thread for accepting clients
        Thread acceptConnectionsThread = new Thread(() => AcceptConnections(ServerSocket));
        acceptConnectionsThread.IsBackground = true;
        acceptConnectionsThread.Start();

        //server welcome message
        Console.Write("//-- echo server --//" + NEW_LINE_SEQUENCE);
        
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