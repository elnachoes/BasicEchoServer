using System;

namespace EchoServer
{
    public class Program
    {
        private static void Main()
        {
            var server = new Server();
            server.HostServer();
        }
    }
}
