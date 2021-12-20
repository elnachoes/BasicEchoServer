using System.Net.Sockets;

namespace EchoServer
{
    public class Connection
    {
        private const int SERVER_PORT = 11000;
        private const int LISTEN_COUNT = 99;
        //private readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //private Socket serverSocket;
        private Socket clientSocket;

        public bool isConnected { get; private set; }

        public Connection(Socket serverSocket)
        {
            //serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //serverSocket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
            //serverSocket.Listen(LISTEN_COUNT);

            isConnected = false;
            clientSocket = serverSocket.Accept();
            isConnected = true;
        }

        public int Send(byte[] package, int size)
        {
            try
            {
                int bytesTransferred = clientSocket.Send(package, size, SocketFlags.None);
                if (bytesTransferred == 0)
                {
                    ShutDownSocket();
                    return 0;
                }
                return bytesTransferred;
            }
            catch (Exception)
            {
                ShutDownSocket();
                return 0;
            }
        }

        public int Receive(byte[] package,int offset, int size)
        {
            try
            {
                int bytesTransferred = clientSocket.Receive(package, offset, size, SocketFlags.None);
                if (bytesTransferred == 0)
                {
                    ShutDownSocket();
                    return 0;
                }
                return bytesTransferred;
            }
            catch (Exception)
            {
                ShutDownSocket();
                return 0;
            }
        }

        public void ShutDownSocket()
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            isConnected = false;
        }
    }
}