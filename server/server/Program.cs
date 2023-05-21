using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            int port = 8080;
            var server = new HttpServer(IPAddress.Any, port);
            Console.WriteLine($"Server is listening on port {port}");
            await server.StartAsync();
        }
    }
}
