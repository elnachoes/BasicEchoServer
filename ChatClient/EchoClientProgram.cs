using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class EchoClientProgram
{
    static void Main()
    {
        string UserInput = string.Empty;
        string Data = string.Empty;
        byte[] ByteBuffer;
        int BytesTransferred;

        //set up socket
        IPHostEntry Host = Dns.GetHostEntry("localhost");
        IPAddress IpAddress = Host.AddressList[0];
        IPEndPoint ServerEndPoint = new IPEndPoint(IpAddress, 11000);
        Socket ClientSocket = new Socket(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            //connect to the server and recieve the welcome message
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

        //main loop that sends messages to the server and recieves them back
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

        //shutdown and close the socket
        ClientSocket.Shutdown(SocketShutdown.Both);
        ClientSocket.Close();

        //press any key to close the terminal
        Console.WriteLine("press any key to close...");
        Console.ReadKey();
    }
}