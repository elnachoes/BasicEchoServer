using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class ChatServerProgram
{
    static void Main()
    {
        //set up socket
        IPHostEntry Host = Dns.GetHostEntry("localhost");
        IPAddress IpAddress = Host.AddressList[0];
        IPEndPoint ServerEndPoint = new IPEndPoint(IpAddress, 11000);
        Socket ServerSocket = new Socket(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        //
        ServerSocket.Bind(ServerEndPoint);
        ServerSocket.Listen(10);

        //
        string Data = string.Empty;
        byte[] ByteBuffer;
        int BytesTransferred;

        //
        Console.WriteLine("Waiting for a connection...");
        ServerSocket = ServerSocket.Accept();
        Console.WriteLine("someone connected...");

        //
        Data = "send messages and the server will echo them back\n";
        ServerSocket.Send(Encoding.ASCII.GetBytes(Data));

        //
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

        //
        ServerSocket.Shutdown(SocketShutdown.Both);
        ServerSocket.Close();

        //
        Console.WriteLine("press any key to close...");
        Console.ReadKey();
    }
}