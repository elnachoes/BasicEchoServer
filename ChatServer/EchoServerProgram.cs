using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class EchoServerProgram
{
    const byte CARRIAGE_RETURN = 13;
    public static int FindCarriageReturn(byte[] buffer, int size)
    {
        for(var i = 0; i < size; i++)
        {
            if(buffer[i] == CARRIAGE_RETURN)
                return i;
        }

        return -1;
    }

    static void Main()
    {
        //set up socket
        var ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //bind the socket and listen
        ServerSocket.Bind(new IPEndPoint(IPAddress.Any, 11000));
        ServerSocket.Listen(99);

        byte[] ByteBuffer = new byte[1024];
        int BytesTransferred;

        while(true)
        {
            //wait for someone to connect to the server
            Console.WriteLine("Waiting for a connection...");
            var clientSocket = ServerSocket.Accept();
            Console.WriteLine("someone connected...");

            //send a welcome message to the client
            clientSocket.Send(Encoding.ASCII.GetBytes("send messages and the server will echo them back\r\n"));

            //main echo loop
            int offset = 0;
            while (true)
            {
                BytesTransferred = clientSocket.Receive(ByteBuffer, offset, ByteBuffer.Length - offset, SocketFlags.None);
                offset += BytesTransferred;
                if (BytesTransferred == 0)
                {
                    break;
                }

                var carriageReturnPosition = FindCarriageReturn(ByteBuffer, offset);
                if (carriageReturnPosition > 0)
                {
                    BytesTransferred = clientSocket.Send(ByteBuffer, carriageReturnPosition, SocketFlags.None);
                    if (BytesTransferred == 0)
                    {
                        break;
                    }

                    offset = 0;
                    Array.Clear(ByteBuffer);
                }
            }
        }

        //////shutdown and close the socket
        ////ServerSocket.Shutdown(SocketShutdown.Both);
        ////ServerSocket.Close();

        //////press any key to close the terminal
        ////Console.WriteLine("press any key to close...");
        ////Console.ReadKey();
    }
}