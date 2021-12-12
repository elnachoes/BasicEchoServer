using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class ChatClientProgram
{
    static void Main()
    {
        string UserInput = string.Empty;
        string Data = string.Empty;
        byte[] ByteBuffer;
        int BytesTransferred;

        IPHostEntry Host = Dns.GetHostEntry("localhost");
        IPAddress IpAddress = Host.AddressList[0];
        IPEndPoint ServerEndPoint = new IPEndPoint(IpAddress, 11000);

        // Create a TCP/IP  socket.
        Socket ClientSocket = new Socket(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            ClientSocket.Connect(ServerEndPoint);
            Console.WriteLine("connected to the server...");
            ByteBuffer = new byte[1024];
            BytesTransferred = ClientSocket.Receive(ByteBuffer);
            Console.WriteLine($"{Encoding.ASCII.GetString(ByteBuffer)}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return;
        }



        Console.WriteLine("Type 'exit' when you are ready to exit");
        while (true)
        {
            ByteBuffer = new byte[1024];
            UserInput = Console.ReadLine();
            
            if (UserInput == "exit")
            {
                break;
            }

            BytesTransferred = ClientSocket.Send(Encoding.ASCII.GetBytes(UserInput));
            if (BytesTransferred == 0)
            {
                break;
            }
            BytesTransferred = ClientSocket.Receive(ByteBuffer);
            if (BytesTransferred == 0)
            {
                break;
            }

            Console.WriteLine($"{Encoding.ASCII.GetString(ByteBuffer)}");
        }

        ClientSocket.Shutdown(SocketShutdown.Both);
        ClientSocket.Close();
    }
}