using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using Emgu.CV.Util;

namespace VideoStreamAppWpf
{
    public partial class UdpProvider
    {
        public delegate void ErrorHandler(string msg);
        public delegate void ReceiveHandler(MemoryStream ms);
        public event ErrorHandler ErrorEvent;
        public event ReceiveHandler ReceiveEvent;


        public void SendSync(byte[] data)
        {
            try
            {
                if (data.Length < 65535) 
                { 
                    _senderClient?.Send(data, data.Length, _remoteEndPoint);
                }
            }
            catch (Exception e)
            {
                ErrorEvent?.Invoke("Udp Provider: " + e.Message);
            }
        }

        public void SendSync(MemoryStream ms) 
        { 
            SendSync(ms.ToArray()); 
        }

        private async void ReceiveMessageAsyncProcess()
        {
            while(true)
            {
                try
                {
                    var result = await _listenerClient?.ReceiveAsync();
                    ReceiveEvent?.Invoke(new MemoryStream(result.Buffer));
                }
                catch(Exception e)
                {
                    ErrorEvent?.Invoke(e.Message);
                    this.Close();
                }
            }
        }

        public void Open()
        {
            try
            {
                _senderClient?.Close();
                _listenerClient?.Close();
                _listenerTask?.Dispose();

                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(RemoteIp), int.Parse(RemotePort));
                _senderClient = new UdpClient(AddressFamily);
                _listenerClient = new UdpClient(int.Parse(InputPort), AddressFamily);
                _listenerTask = new Task(ReceiveMessageAsyncProcess);
                _listenerTask.Start();

                _isRunning = true;
            }
            catch(Exception e) 
            {
                _isRunning = false;
                ErrorEvent?.Invoke(e.Message);
            }
            
        }

        public void Close()
        {
            _senderClient?.Close();
            _listenerClient?.Close();
            _listenerTask?.Dispose();
            
            _isRunning = true;
        }

        public bool IsRunning() { return _isRunning; }
   
        public AddressFamily AddressFamily { get; set; }
        public string RemoteIp { get; set; }
        public string RemotePort { get; set; }
        public string InputPort { get; set; }

        private bool _isRunning = false;
        private Task _listenerTask;
        private IPEndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
        private UdpClient _listenerClient = new UdpClient();
        private UdpClient _senderClient = new UdpClient();

        public UdpProvider() 
        {}

        ~UdpProvider()
        {
            this.Close();

            _listenerClient.Dispose();
            _listenerTask.Dispose();
            _senderClient.Dispose();
        }
    }
}
