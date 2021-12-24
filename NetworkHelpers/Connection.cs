using System.Net.Sockets;

public class Connection
{
    public delegate void ReceiveCallback(byte[] buffer, int bytesReceived, Connection connection);
    public delegate void ConnectionShutdownCallback(Connection connection);

    private Socket _connectionSocket;

    public bool _isConnected { get; private set; }

    private ReceiveCallback _recieveCallback { get; set; }
    private ConnectionShutdownCallback _connectionShutdownCallback { get; set; }

    public byte[] _buffer { get; set; }
    public int _offset { get; set; }
    public int _size { get; set; }

    public Connection(Socket newConnectionSocket, ConnectionShutdownCallback connectionShutdownCallback, ReceiveCallback receiveCallback)
    {
        _connectionSocket = newConnectionSocket;
        _isConnected = true;
        _connectionShutdownCallback = connectionShutdownCallback;
        _recieveCallback = receiveCallback;
    }

    //public delegate void EndRec

    public int Send(byte[] newBuffer, int newSize)
    {
        try
        {
            int bytesTransferred = _connectionSocket.Send(newBuffer, newSize, SocketFlags.None);
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

    public int Receive(byte[] newBuffer, int newOffset, int newSize)
    {
        try
        {
            int bytesTransferred = _connectionSocket.Receive(newBuffer, newOffset, newSize, SocketFlags.None);
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

    public void StartRecieving(byte[] newBuffer, int newOffset, int newSize)
    {
        _buffer = newBuffer;
        _offset = newOffset;
        _size =  newSize;

        BeginReceive();
    }

    private IAsyncResult BeginReceive()
    {
        return _connectionSocket.BeginReceive(_buffer, _offset, _size, SocketFlags.None, EndReceive, null);
    }

    private void EndReceive(IAsyncResult asyncResult)
    {
        try
        {
            var bytesReceived = _connectionSocket.EndReceive(asyncResult);
            if (bytesReceived == 0)
            {
                ShutDownSocket();
                return;
            }
            _recieveCallback(_buffer, bytesReceived, this);
            BeginReceive();
        }
        catch (Exception)
        {
            ShutDownSocket();
            return;
        }
    }

    public void ShutDownSocket()
    {
        _connectionSocket.Shutdown(SocketShutdown.Both);
        _connectionSocket.Close();
        _isConnected = false;
        _connectionShutdownCallback(this);
    }
}
