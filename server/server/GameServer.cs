using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace server
{
        public class GameServer : IDisposable
        {
            private readonly TcpClient _client;
            private WebSocket _webSocket;

            public GameServer(TcpClient client)
            {
                _client = client;
            }

            public async Task HandleClientAsync()
            {
                using (var stream = _client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string requestLine;
                    while (!string.IsNullOrEmpty(requestLine = await reader.ReadLineAsync()))
                    {
                        Console.WriteLine(requestLine);
                    }
                    _webSocket = new WebSocket(stream);
                    await _webSocket.HandshakeAsync();

                    while (true)
                    {
                        // Read and process WebSocket messages here
                    }
                }
            }

            public void Dispose()
            {
                _webSocket?.Dispose();
            }
        }
    }