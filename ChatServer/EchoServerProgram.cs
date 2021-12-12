using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class EchoServerProgram
{
    static void Main()
    {
        //set up socket
        IPHostEntry Host = Dns.GetHostEntry("localhost");
        IPAddress IpAddress = Host.AddressList[0];
        IPEndPoint ServerEndPoint = new IPEndPoint(IpAddress, 11000);
        Socket ServerSocket = new Socket(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        //bind the socket and listen
        ServerSocket.Bind(ServerEndPoint);
        ServerSocket.Listen(10);

        string Data = string.Empty;
        byte[] ByteBuffer;
        int BytesTransferred;

        //wait for someone to connect to the server
        Console.WriteLine("Waiting for a connection...");
        ServerSocket = ServerSocket.Accept();
        Console.WriteLine("someone connected...");

        //send a welcome message to the client
        Data = "send messages and the server will echo them back\n";
        ServerSocket.Send(Encoding.ASCII.GetBytes(Data));

        //main echo loop
        Data = string.Empty;
        while (true)
        {
            ByteBuffer = new byte[1024];

            BytesTransferred = ServerSocket.Receive(ByteBuffer);
            if (BytesTransferred == 0)
            {
                break;
            }

            BytesTransferred = ServerSocket.Send(ByteBuffer);
            if (BytesTransferred == 0)
            {
                break;
            }
        }

        //shutdown and close the socket
        ServerSocket.Shutdown(SocketShutdown.Both);
        ServerSocket.Close();

        //press any key to close the terminal
        Console.WriteLine("press any key to close...");
        Console.ReadKey();
    }
}