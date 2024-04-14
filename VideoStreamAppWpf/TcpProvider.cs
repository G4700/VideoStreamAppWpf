using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace VideoStreamAppWpf
{

    public partial class TcpProvider : NetProvider
    {
        public event ReceiveHandler ReceiveEvent;
        public event ErrorHandler ErrorEvent;
        public event NotificationHandler NotificationEvent;

        private IPEndPoint _ipEndPoint;
        private TcpListener _tcpListener;
        private TcpClient _tcpClient;
        private NetworkStream _tcpClientNetworkStream;

        public AddressFamily AddressFamily { get; set; }
        public string InputPort { get; set; }
        public string RemoteIp { get; set; }
        public string RemotePort { get; set; }


        public async void Open()
        {
            _ipEndPoint = new IPEndPoint(IPAddress.Parse(RemoteIp), int.Parse(RemotePort));

            try
            {
                _tcpClient = new TcpClient(AddressFamily);
                _tcpListener = new TcpListener(IPAddress.Any, int.Parse(InputPort));
                await _tcpClient.ConnectAsync(IPAddress.Parse(RemoteIp), int.Parse(RemotePort));
                _tcpClientNetworkStream = _tcpClient.GetStream();
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke(ex.Message);
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                _tcpClientNetworkStream?.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke(ex.Message);
            }
        }

        public void Close()
        {
            _tcpClient.Close();
            _tcpClient.Dispose();
        }

    
    }
}
