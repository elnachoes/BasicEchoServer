using System.Net.Sockets;

public class Connection
{
    private const int BYTE_BUFFER_SIZE = 1024;
    private const byte CARRIAGE_RETURN_DECIMAL = 13;
    private const string CARRIAGE_RETURN_STRING = "\r\n";

    private Socket _connectionSocket;

    private Action<Connection, string> _recieveCallback { get; set; }
    private Action<Connection> _connectionShutdownCallback { get; set; }

    private byte[] _buffer = new byte[BYTE_BUFFER_SIZE];
    private int _offset = 0;
    private int _size = BYTE_BUFFER_SIZE;

    public Connection(Socket newConnectionSocket, Action<Connection> connectionShutdownCallback, Action<Connection, string> receiveCallback)
    {
        _connectionSocket = newConnectionSocket;
        _connectionShutdownCallback = connectionShutdownCallback;
        _recieveCallback = receiveCallback;
    }

    public int Send(string data)
    {
        try
        {
            var dataBytes = System.Text.Encoding.UTF8.GetBytes($"{data}{CARRIAGE_RETURN_STRING}");

            int bytesTransferred = _connectionSocket.Send(dataBytes, dataBytes.Length, SocketFlags.None);
            if (bytesTransferred == 0)
                ShutDownSocket();
            return bytesTransferred;
        }
        catch (Exception)
        {
            ShutDownSocket();
            return 0;
        }
    }

    public void StartReceiving()
    {
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

            _offset += bytesReceived;
            _size = _buffer.Length - _offset;

            var carriageReturnPosition = FindCarriageReturn();

            if (carriageReturnPosition > 0)
            {
                var data = System.Text.ASCIIEncoding.UTF8.GetString(_buffer);
                _recieveCallback(this, data);

                _offset = 0;
                _size = _buffer.Length;
                Array.Clear(_buffer);
            }

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
        _connectionShutdownCallback(this);
    }

    //This just returns the position in a byte array for where the carriage return symbole is
    private int FindCarriageReturn()
    {
        for (var i = 0; i < _offset; i++)
        {
            if (_buffer[i] == CARRIAGE_RETURN_DECIMAL)
            {
                return i + 2;
            }
        }
        return -1;
    }
}
