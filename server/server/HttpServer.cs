using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    internal class HttpServer
    {
        private readonly TcpListener _listener;

        public HttpServer(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected");
                Task.Run(() => HandleClientAsync(client));
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (var gameServer = new GameServer(client))
            {
                await gameServer.HandleClientAsync();
            }
            client.Close();
        }
    }
}
