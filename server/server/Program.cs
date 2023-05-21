using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Web;

namespace server
{

    public class Player
    {
        public string Username { get; set; }

        public Player(string username)
        {
            Username = username;
        }
    }

    public class GameRecord
    {
        public Guid GameId { get; set; }
        public string GameState { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public string Player1LastMove { get; set; }
        public string Player2LastMove { get; set; }

        public GameRecord(Guid gameId, string gameState, string player1, string player2, string player1LastMove, string player2LastMove)
        {
            GameId = gameId;
            GameState = gameState;
            Player1 = player1;
            Player2 = player2;
            Player1LastMove = player1LastMove;
            Player2LastMove = player2LastMove;
        }
    }

    public class Program
    {
        //创建一个sockert实例，实例作为服务器servre
        private static Socket _SeverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static int counter = 0;
        private static GameRecord currentGame = null;

        static void Main()
        {
            Console.WriteLine("Setting up server ...");
            //绑定socket到任意的可用IP地址和8000端口上
            _SeverSocket.Bind(new IPEndPoint(IPAddress.Any, 8000));

            //让sokcet开始监听这个端口的链接请求，让队列中最多可以有10个待处理的请求
            _SeverSocket.Listen(10);

            //异步接受链接请求
            _SeverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            Console.WriteLine("Sever is listening");

            //阻塞主线程，防止应用程序立即关闭
            while (true) { }

        }

        //前端连接处理请求的brige
        private static void AcceptCallback(IAsyncResult AR)
        {
            //结束接收连接请求，返回新的socket用于和客户端通信
            Socket socket = _SeverSocket.EndAccept(AR);

            byte[] buffer = new byte[socket.ReceiveBufferSize];
            
            //接收来自客户端的数据
            int received = socket.Receive(buffer,SocketFlags.None);
            if (received <= 0) return;

            //将接收到的字节转换成字符串
            string request = Encoding.UTF8.GetString(buffer,0,received);

            //检查请求是否是对/register端点的
            if(request.StartsWith("GET /register"))
            {
                //生成一个用户名
                string username = "user" +  counter++;
                Console.WriteLine(username);

                //创建响应 HTTP 版本（HTTP/1.1），状态码（200），以及状态文本（OK）
                //Content-Length 表示后面的主体部分（即响应的实际内容）的长度
                //username：这是响应的主体，即实际返回给请求者的数据
                string response = "HTTP/1.1 200 OK\r\n" +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS\r\n" +
                    "Access-Control-Allow-Headers: Content-Type\r\n" +
                    "Content-Length: " + username.Length + "\r\n\r\n" + username;
                //将响应转换成字节数并发送给客户端
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                socket.Send(responseBytes);
            }

            //请求/pairme端点
            else if (request.StartsWith("GET /pairme"))
            {
                //从请求中获取用户名
                var requestUri = new Uri("http://localhost:8000" + request.Split(' ')[1]);
                var queryParameters = HttpUtility.ParseQueryString(requestUri.Query);
                var username = queryParameters.Get("player");
                HandlePairMeRequest(socket, username);
            }

            //请求/gamestate端点
            else if(request.StartsWith("GET /gamestate"))
            {
                //获取用户名
                var requestUri = new Uri("http://localhost:8000" + request.Split(' ')[1]);
                var queryParameters = HttpUtility.ParseQueryString(requestUri.Query);
                var username = queryParameters.Get("player");
                Console.WriteLine(username + " requested game state");
                HandleGameStateRequest(socket, username);
            }

            //关闭和客户端的连接，开始接收新的连接请求
            socket.Close();
            _SeverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        //请求的heading
        public static string MakeJsonResponse(string jsonContent)
        {
            return "HTTP/1.1 200 OK\r\n" +
                   "Access-Control-Allow-Origin: *\r\n" +
                   "Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS\r\n" +
                   "Access-Control-Allow-Headers: Content-Type\r\n" +
                   "Content-Type: application/json\r\n" +
                   "Content-Length: " + jsonContent.Length + "\r\n\r\n" + jsonContent;
        }

        //进行匹配
        public static void HandlePairMeRequest(Socket socket, string username)
        {
            string response;
            //如果已有游戏在等待玩家
            if (currentGame != null && currentGame.GameState == "wait")
            {

                //检查是否是同一玩家
                if (currentGame.Player1 == username)
                {
                    var jsonResponse = JsonSerializer.Serialize(new { status = "inline" });
                    response = MakeJsonResponse(jsonResponse);

                }
                else
                {
                    //将新玩家加入游戏并将游戏状态改为"progress"
                    currentGame.Player2 = username;
                    currentGame.GameState = "progress";
                    var jsonResponse = JsonSerializer.Serialize(currentGame);
                    response = MakeJsonResponse(jsonResponse);

                }
            }
            //没有游戏在等待玩家，创建新的游戏
            else
            {
                currentGame = new GameRecord(Guid.NewGuid(), "wait", username, null, null, null);
                var jsonResponse = JsonSerializer.Serialize(currentGame);
                response = MakeJsonResponse(jsonResponse);

            }

            //发送给客户端
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            socket.Send(responseBytes);
        }

        //实时检查匹配
        public static void HandleGameStateRequest(Socket socket, string username)
        {
            string response;

            // 检查是否有游戏正在进行
            if (currentGame != null)
            {
                // 检查请求的用户名是否是游戏的其中一个玩家
                if (currentGame.Player1 == username || currentGame.Player2 == username)
                {
                    var jsonResponse = JsonSerializer.Serialize(currentGame);
                    response = MakeJsonResponse(jsonResponse);
                }
                else
                {
                    // 用户不是当前游戏的玩家
                    var jsonResponse = JsonSerializer.Serialize(new { status = "not in game" });
                    response = MakeJsonResponse(jsonResponse);
                }
            }
            else
            {
                // 没有正在进行的游戏
                var jsonResponse = JsonSerializer.Serialize(new { status = "no game" });
                response = MakeJsonResponse(jsonResponse);
            }

            // 发送响应
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            socket.Send(responseBytes);
        }
    }
}
