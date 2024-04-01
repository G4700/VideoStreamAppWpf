using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VideoStreamAppWpf
{
    public class UdpListener
    {
        public UdpClient UdpClient;

        public delegate void OnDataReceivedHandler(byte[] data);
        public event OnDataReceivedHandler DataReceivedEvent;

        public async void Process()
        {
            while(true)
            {
                var data = await UdpClient.ReceiveAsync();
                DataReceivedEvent?.Invoke(data.Buffer);
            }
           
        }

        public UdpListener(int port) 
        {
            UdpClient = new UdpClient(port);
            Task.Run(() => Process());
        }

        ~UdpListener() 
        {
            UdpClient.Close();
        }
    }
}
