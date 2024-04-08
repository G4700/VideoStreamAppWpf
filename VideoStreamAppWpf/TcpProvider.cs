using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace VideoStreamAppWpf
{

    public partial class TcpProvider
    {
        public class Server
        {
            public delegate void ErrorHandler(string msg);
            public delegate void NotificationHandler(string msg);
            public event ErrorHandler ErrorEvent;
            public event NotificationHandler Notification;

            private TcpListener _tcpListener;
            private List<Client> _clients = new List<Client>();

            protected internal void RemoveConnection(string id)
            {
                Client client = _clients.FirstOrDefault(c => c.Id == id);
                if (client != null) _clients.Remove(client);
                client?.Close();
            }

            protected internal async Task ListenAsync()
            {
                try
                {
                    _tcpListener.Start();

                    while (true)
                    {
                        TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
                        Client clientObject = new Client(tcpClient, this);
                        _clients.Add(clientObject);
                        Task.Run(clientObject.ProcessAsync);
                    }
                }
                catch (Exception ex) { ErrorEvent?.Invoke($"Tcp Provider: {ex.Message}"); }
                finally { Disconnect(); }
            }

            protected internal async Task BroadcastMessageAsync(string message, string id)
            {
                foreach (var client in _clients)
                {
                    if (client.Id != id)
                    {
                        await client._writer.WriteLineAsync(message);
                        await client._writer.FlushAsync();
                    }
                }
            }

            protected internal void Disconnect()
            {
                foreach (var client in _clients) client.Close();
                _tcpListener.Stop(); 
            }

            public Server() { }
        }
        
        public class Client 
        {
            public async Task ProcessAsync()
            {
                try
                {
                    string userName = await _reader.ReadLineAsync();
                    string message = $"{userName} вошел в чат";
                    await _server.BroadcastMessageAsync(message, Id);
                    Console.WriteLine(message);

                    while (true)
                    {
                        try
                        {
                            message = await _reader.ReadLineAsync();
                            if (message == null) continue;
                            message = $"{userName}: {message}";
                            Console.WriteLine(message);
                            await _server.BroadcastMessageAsync(message, Id);
                        }
                        catch
                        {
                            message = $"{userName} покинул чат";
                            Console.WriteLine(message);
                            await _server.BroadcastMessageAsync(message, Id);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    _server.RemoveConnection(Id);
                }
            }


            public Client(TcpClient client, Server server) 
            {
                _client = client;
                _server = server;
                var stream = client.GetStream();
                _reader = new StreamReader(stream);
                _writer = new StreamWriter(stream);
            }

            public void Close()
            {
                _writer?.Close();
                _writer?.Dispose();

                _reader?.Close();
                _reader?.Dispose();

                _client?.Close();
                _client?.Dispose();
            }

            public string Id { get; } = Guid.NewGuid().ToString();
            protected internal StreamWriter _writer { get; }
            protected internal StreamReader _reader { get; }
            private TcpClient   _client;
            private Server      _server;
        }
        
    }
}
