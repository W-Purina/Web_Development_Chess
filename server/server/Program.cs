using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class Program
    {
        private static Socket _SeverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static int counter = 0;

        static void Main()
        {
            Console.WriteLine("Setting up server ...");
            _SeverSocket.Bind(new)
        }
    }
}
