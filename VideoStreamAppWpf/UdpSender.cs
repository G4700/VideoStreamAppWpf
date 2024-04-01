using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VideoStreamAppWpf
{
    public class UdpSender
    {
        public IPEndPoint    DestinationEndPoint;
        public UdpClient     UdpClient;

        public UdpSender(IPAddress destinationIp, int port) 
        {
            DestinationEndPoint = new IPEndPoint(destinationIp, port);
            UdpClient = new UdpClient(DestinationEndPoint);
        }

        public void Send(byte[] data) 
        {
            UdpClient.Send(data, data.Length, DestinationEndPoint);
        }   

        ~UdpSender() { UdpClient.Close(); }
    }
}
