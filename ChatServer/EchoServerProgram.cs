using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class EchoServerProgram
{
    const byte CARRIAGE_RETURN_DECIMAL = 13;
    const string CARRIAGE_RETURN_STRING = "\r\n";
    const int SERVER_PORT = 11000;
    const int LISTEN_COUNT = 99;
    const int BYTE_BUFFER_SIZE = 1024;

    //This just returns the position in a byte array for where the carriage return symbole is
    static int FindCarriageReturn(byte[] buffer, int size)
    {
        for(var i = 0; i < size; i++)
        {
            if(buffer[i] == CARRIAGE_RETURN_DECIMAL)
                return i + 2;
        }

        return -1;
    }

    // This function is ment to be running constantly as a backround thread
    // It will keep searching for new guests to connect to the server
    static void AcceptConnections(Socket serverSocket, List<Socket> activeConnections)
    {
        //mutex object
        object lockObject = new object();

        while (true)
        {
            //wait for someone to connect to the server
            Console.WriteLine("Waiting for a connection...");
            var clientSocket = serverSocket.Accept();
            Console.WriteLine("someone connected...");

            //every time a new client joins add the socket to the activeConnections
            lock (lockObject)
            {
                activeConnections.Add(clientSocket);
            }

            //send the welcome message out to the new client
            ServerSendMessage(clientSocket, "Welcome to the chat!");

            //create a new background thread for listening to the new client
            Thread listenToClients = new Thread(() => ListenToClients(clientSocket, activeConnections));
            listenToClients.IsBackground = true;
            listenToClients.Start();
        }
    }

    //this is a background thread that will listen to what clients send to the server
    static void ListenToClients(Socket clientSocket ,List<Socket> activeConnections)
    {
        //mutex object
        object lockObject = new object();

        //variables for transfering data
        byte[] ByteBuffer = new byte[BYTE_BUFFER_SIZE];
        int BytesTransferred;

        //main listening loop that listens for bytes that come from the client
        int offset = 0;
        while (true)
        {
            //waits for a message from the user
            BytesTransferred = clientSocket.Receive(ByteBuffer, offset, ByteBuffer.Length - offset, SocketFlags.None);
            offset += BytesTransferred;
            if (BytesTransferred == 0) 
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
                return;
            }

            //echo the input from one client to all users
            var carriageReturnPosition = FindCarriageReturn(ByteBuffer, offset);
            if (carriageReturnPosition > 0)
            {
                foreach (var client in activeConnections)
                {
                    if (client != clientSocket)
                    {
                        try
                        {
                            BytesTransferred = client.Send(ByteBuffer, carriageReturnPosition, SocketFlags.None);
                        }
                        catch (Exception)
                        {
                            return;
                        }

                        if (BytesTransferred == 0)
                        {
                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                            continue;
                        }
                    }
                }
                offset = 0;
                Array.Clear(ByteBuffer);
            }
        }
    }

    //send a message to all clients from the server
    static void ServerSendMessage(List<Socket> activeConnections, string message)
    {
        foreach (var client in activeConnections)
        {
            message += CARRIAGE_RETURN_STRING;
            var bytesTransferred = client.Send(Encoding.ASCII.GetBytes(message));
            if (bytesTransferred == 0)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                continue;
            }
        }
    }

    //send a message to a specific client
    static void ServerSendMessage(Socket clientSocket, string message)
    {
        message += CARRIAGE_RETURN_STRING;
        var bytesTransferred = clientSocket.Send(Encoding.ASCII.GetBytes(message));
        if (bytesTransferred == 0)
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            return;
        }
    }

    static void Main()
    {
        var activeConnections = new List<Socket>();

        //set up socket
        var ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //bind the socket and listen
        ServerSocket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
        ServerSocket.Listen(LISTEN_COUNT);

        //start a separate thread 
        Thread acceptConnectionsThread = new Thread(() => AcceptConnections(ServerSocket, activeConnections));
        acceptConnectionsThread.IsBackground = true;
        acceptConnectionsThread.Start();

        //main loop for recieving server commands
        while (true)
        {
            //if the user types in exit in the server terminal it will shutdown the server
            var userInput = Console.ReadLine();
            if (userInput == "exit")
            {
                ServerSendMessage(activeConnections, "Server shutting down");
                return;
            }
        }
    }
}