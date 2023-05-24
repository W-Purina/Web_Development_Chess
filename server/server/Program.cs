using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Web;

namespace server
{
    //用户
    public class Player
    {
        public string Username { get; set; }

        public Player(string username)
        {
            Username = username;
        }
    }

    //游戏数据
    public class GameRecord
    {
        public Guid GameId { get; set; }
        public string GameState { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public List<Move> Player1LastMove { get; set; }
        public List<Move> Player2LastMove { get; set; }
        public string Authenticate { get; set; }



        public GameRecord(Guid gameId, string gameState, string player1, string player2, string player1LastMove, string player2LastMove,string Authentication)
        {
            GameId = gameId;
            GameState = gameState;
            Player1 = player1;
            Player2 = player2;
            Player1LastMove = new List<Move>();  // 初始化列表
            Player2LastMove = new List<Move>();  // 初始化列表
            Authenticate = Authentication;
        }
    }

    //移动数据-3个参数
    public class Move
    {
        public string Piece { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }

    //玩家移动数据
    public class Game
    {
        public List<Move> Player1LastMove { get; set; }
        public List<Move> Player2LastMove { get; set; }
    }

    public class Program
    {
        private static GameRecord currentGame = null;

        // 将gameList设为一个字典
        private static Dictionary<Guid, GameRecord> gameList = new Dictionary<Guid, GameRecord>();

        //用户名库
        private static List<string> availableUsernames = new List<string>
        {
            "Alice", "Bob", "Charlie", "Dave", "Eve", "Frank", "Grace", "Hank", "Ivy", "Jack","Ethan","Olivia",
            "Liam","Emma","Noah","Ava","Mason","Sophia","Lucas","Isabella","Oliver","Mia","Aiden","Charlotte","Elijah",
            "Amelia","James","Harper","Benjamin","Evelyn"
        };
        private static HashSet<string> registeredUsers = new HashSet<string>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Setting up server ...");


            //绑定socket到任意的可用IP地址和8000端口上
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, 8000));

            //让sokcet开始监听这个端口的链接请求，让队列中最多可以有100个待处理的请求
            listener.Listen(100);

            Console.WriteLine("Sever is listening http://localhost:8000");

            while (true)
            {
                var socket = listener.Accept();

                // 向客户端发送一个确认连接的消息
                string response = " ";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                socket.Send(responseBytes);

                Task.Run(async() => HandleRequest(socket));

            }

        }
   
        //处理请求
        private static void HandleRequest(Socket socket)
        {
            //保持连接
            bool alive = true;

            try
            {
                while(alive)
                {
                    StringBuilder request = new StringBuilder();
                    byte[] buffer = new byte[1024];

                    // 保持接收数据直到客户端关闭连接
                    while (true)
                    {
                        try
                        {
                            int bytesRead = socket.Receive(buffer);
                            request.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                            if (request.ToString().EndsWith("\r\n\r\n"))
                            {
                                break;
                            }

                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"{ex.Message}");
                        }

                    }

                    //把传来的request作转字符串处理
                    string requestString = request.ToString();
                    var requestLines = requestString.Split('\n');
                    if (requestLines.Length == 0)
                    {
                        Console.WriteLine("No request lines received.");
                        return;
                    }

                    var requestLine = requestLines[0].Split(" ");
                    if (requestLine.Length < 2)
                    {
                        Console.WriteLine("The request line does not contain enough elements.");
                        return;
                    }

                    //获取url
                    string url = requestLine[1];

                    // 处理请求
                    ///register端点的

                    if (url.StartsWith("/register"))
                    {
                        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} sent response to  {socket.RemoteEndPoint}  for /register");
                        HandleRegisterRequest(socket);

                    }

                    //请求进入游戏
                    else if (url.StartsWith("/trygame"))
                    {
                        // 从请求中获取用户名
                        var requestUri = new Uri("http://localhost:8000" + request.ToString().Split(' ')[1]);
                        var queryParameters = HttpUtility.ParseQueryString(requestUri.Query);
                        var username = queryParameters.Get("player");

                        // 处理 /trygame 请求
                        HandleTryGameRequest(socket, username);
                    }

                    //请求/pairme端点
                    else if (url.StartsWith("/pairme"))
                    {
                        //从请求中获取用户名
                        var requestUri = new Uri("http://localhost:8000" + request.ToString().Split(' ')[1]);
                        var queryParameters = HttpUtility.ParseQueryString(requestUri.Query);
                        var username = queryParameters.Get("player");

                        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} send response to {socket.RemoteEndPoint}  for /pairme?player={username} ");
                        HandlePairMeRequest(socket, username);
                    }

                    //请求/gamestate端点
                    else if (url.StartsWith("/gamestate"))
                    {
                        //获取用户名
                        var requestUri = new Uri("http://localhost:8000" + request.ToString().Split(' ')[1]);
                        var queryParameters = HttpUtility.ParseQueryString(requestUri.Query);
                        var username = queryParameters.Get("player");
                        HandleGameStateRequest(socket, username);
                    }

                    //请求/mymove端点
                    else if (url.StartsWith("/mymove"))
                    {
                        //解析URL的参数
                        var uri = new Uri("http://localhost" + request.ToString().Split(' ')[1]);
                        var query = HttpUtility.ParseQueryString(uri.Query);


                        var player = query.Get("player");
                        string idString = query.Get("id");
                        var move = query.Get("move");

                        Guid id;
                        if (!string.IsNullOrEmpty(idString) && Guid.TryParse(idString, out id))
                        {
                            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} send response to {socket.RemoteEndPoint}  for /mymove?player={player}&id={idString}&move={move}");

                            // id 参数有效，可以使用 id 进行后续操作
                            HandleMyMoveRequest(socket, player, id, move);
                        }
                        else
                        {
                            // id 参数无效，返回错误信息或进行其他处理
                            Console.WriteLine("Invalid id parameter.");
                        }
                    }

                    //请求/GetTheirMove端点
                    else if (url.StartsWith("/theirmove"))
                    {
                        // 解析 URL 参数
                        var uri = new Uri("http://localhost" + request.ToString().Split(' ')[1]);
                        var query = HttpUtility.ParseQueryString(uri.Query);

                        var player = query.Get("player");
                        string idString = query.Get("id");

                        Guid id;
                        if (!string.IsNullOrEmpty(idString) && Guid.TryParse(idString, out id))
                        {
                            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} send response to {socket.RemoteEndPoint}  for /theirmove?player={player}&id={idString}");
                            // id 参数有效，可以使用 id 进行后续操作
                            HandleTheirMoveRequest(socket, player, id);
                        }
                        else
                        {
                            // id 参数无效，返回错误信息或进行其他处理
                            Console.WriteLine("Invalid id parameter.");
                        }

                    }

                    //处理Quit
                    else if (url.StartsWith("/quit"))
                    {
                        //解析URL的参数
                        var uri = new Uri("http://localhost" + request.ToString().Split(' ')[1]);
                        var query = HttpUtility.ParseQueryString(uri.Query);

                        var player = query.Get("player");
                        string idString = query.Get("id");

                        Guid id;
                        if (!string.IsNullOrEmpty(idString) && Guid.TryParse(idString, out id))
                        {
                            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} closing connection with {socket.RemoteEndPoint} and terminating");
                            // id 参数有效，可以使用 id 进行后续操作
                            HandleQuitRequest(socket, player, id);
                        }
                        else
                        {
                            // id 参数无效，返回错误信息或进行其他处理
                            Console.WriteLine("Invalid id parameter.");
                        }

                    }
                    // After handling the request
                    request.Clear();

                }
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally { socket.Close(); }                        
            
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

        //注册需求
        public static void HandleRegisterRequest(Socket socket)
        {
            if (availableUsernames.Count == 0)
            {
                // 如果没有可用的用户名，返回错误信息
                string jsonResponse = JsonSerializer.Serialize(new { status = "error", message = "No available usernames" });
                string response = MakeJsonResponse(jsonResponse);
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                socket.Send(responseBytes);
            }
            else
            {
                // 随机选择一个用户名并从列表中移除
                var random = new Random();
                int index = random.Next(availableUsernames.Count);
                string username = availableUsernames[index];
                availableUsernames.RemoveAt(index);

                // 将用户名添加到注册用户的集合中
                registeredUsers.Add(username);

                // 返回用户名
                string response = "HTTP/1.1 200 OK\r\n" +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS\r\n" +
                    "Access-Control-Allow-Headers: Content-Type\r\n" +
                    "Content-Length: " + username.Length + "\r\n\r\n" + username;
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                socket.Send(responseBytes);
            }
        }

        //tryGame进行检查
        public static void HandleTryGameRequest(Socket socket, string username)
        {
            string response;

            // 检查用户是否已注册
            if (!registeredUsers.Contains(username))
            {
                var jsonResponse = JsonSerializer.Serialize(new { status = "error", message = "User not registered" });
                response = MakeJsonResponse(jsonResponse);
            }
            else
            {
                var jsonResponse = JsonSerializer.Serialize(new { status = "success", message = "User is registered. You can start the game" });
                response = MakeJsonResponse(jsonResponse);
            }

            // 发送给客户端
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            socket.Send(responseBytes);
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

                    // 将currentGame的新状态更新到gameList中
                    gameList[currentGame.GameId] = currentGame; ;

                }
            }
            //没有游戏在等待玩家，创建新的游戏
            else
            {
                currentGame = new GameRecord(Guid.NewGuid(), "wait", username, null, null, null, username);
                var jsonResponse = JsonSerializer.Serialize(currentGame);
                response = MakeJsonResponse(jsonResponse);

                // 将新创建的currentGame添加到gameList中
                gameList.Add(currentGame.GameId, currentGame);

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

        //处理玩家发送move
        public static void HandleMyMoveRequest(Socket socket, string username, Guid gameId, string move)
        {
            string response;
            byte[] responseBytes;  // 这里声明了responseBytes变量

            // 解析move字符串成为Move列表对象
            List<Move> moveList = JsonSerializer.Deserialize<List<Move>>(move);

            // 检查游戏是否在进行并且游戏ID匹配
            if (currentGame != null && currentGame.GameState == "progress" && currentGame.GameId == gameId)
            {
                // 检查当前用户是否是Player round
                if (currentGame.Authenticate == username)
                {
                    // 遍历每个move
                    foreach (Move moveObj in moveList)
                    {
                        // 如果move是null，表示玩家没有移动
                        if (moveObj != null)
                        {
                            if (currentGame.Player1 == username)
                            {
                                currentGame.Player1LastMove.Add(moveObj);  // 添加移动到玩家1的移动列表
                                currentGame.Authenticate = currentGame.Player2; // 修改为下一个玩家
                            }
                            else if (currentGame.Player2 == username)
                            {
                                currentGame.Player2LastMove.Add(moveObj);  // 添加移动到玩家2的移动列表
                                currentGame.Authenticate = currentGame.Player1; // 修改为下一个玩家
                            }
                        }
                        else
                        {
                            var errorResponse = JsonSerializer.Serialize(new { status = "error", message = "No move made." });
                            response = MakeJsonResponse(errorResponse);
                            responseBytes = Encoding.UTF8.GetBytes(response);
                            socket.Send(responseBytes);
                            return;
                        }
                    }

                    // 返回更新后的游戏记录
                    var jsonResponse = JsonSerializer.Serialize(currentGame);
                    response = MakeJsonResponse(jsonResponse);
                }
                else
                {
                    // 如果当前用户不是Authenticate，返回错误
                    var errorResponse = JsonSerializer.Serialize(new { status = "error", message = "Not your turn." });
                    response = MakeJsonResponse(errorResponse);
                }
            }
            else
            {
                // 如果游戏没有进行或者查不到ID，返回错误
                var errorResponse = JsonSerializer.Serialize(new { status = "error", message = "Game not in progress or invalid game ID" });
                response = MakeJsonResponse(errorResponse);
            }

            // 发送响应
            responseBytes = Encoding.UTF8.GetBytes(response);
            socket.Send(responseBytes);
        }

        //处理玩家GetMove
        public static void HandleTheirMoveRequest(Socket socket, string username, Guid gameId)
        {
            string response;

            //检查游戏是否在进行并且游戏ID匹配
            if (currentGame != null && currentGame.GameState == "progress" && currentGame.GameId == gameId)
            {
                //检查当前用户是否是Authenticate,是user则可以查看
                if (currentGame.Authenticate == username)
                {
                    var jsonResponse = new string("");
                    List<Move> otherPlayerMoves;

                    //返回另一个玩家的移动
                    if (currentGame.Player1 == username)
                    {
                        otherPlayerMoves = currentGame.Player2LastMove;
                    }
                    else if (currentGame.Player2 == username)
                    {
                        otherPlayerMoves = currentGame.Player1LastMove;
                    }
                    else
                    {
                        var errorResponse = JsonSerializer.Serialize(new { status = "error", message = "Invalid player." });
                        response = MakeJsonResponse(errorResponse);
                        return;
                    }

                    // 如果对方没有移动
                    if (otherPlayerMoves.Count == 0)
                    {
                        jsonResponse = JsonSerializer.Serialize(new { status = "waiting", message = "Waiting for their move..." });
                    }
                    else
                    {
                        jsonResponse = JsonSerializer.Serialize(new { moves = otherPlayerMoves });

                        // 清除对方的移动记录，实现回合制
                        otherPlayerMoves.Clear();
                    }

                    response = MakeJsonResponse(jsonResponse);

                }
                else
                {
                    //如果当前用户不是Authenticate，返回错误
                    var errorResponse = JsonSerializer.Serialize(new { status = "error", message = "Not your turn." });
                    response = MakeJsonResponse(errorResponse);

                }
            }
            else
            {
                //如果游戏没有进行或者查不到ID，返回错误
                var errorResponse = JsonSerializer.Serialize(new { status = "error", message = "Game not in progress or invalid game ID" });
                response = MakeJsonResponse(errorResponse);

            }

            // 发送响应
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            socket.Send(responseBytes);

        }

        //处理玩家退出游戏
        public static void HandleQuitRequest(Socket socket, string username, Guid gameId)
        {
            // 找到该用户的游戏记录
            if (gameList.ContainsKey(gameId))
            {
                GameRecord gameToQuit = gameList[gameId];
                if (gameToQuit.Player1 == username || gameToQuit.Player2 == username)
                {
                    // 清除玩家信息并将游戏状态设为等待
                    if (gameToQuit.Player1 == username)
                    {
                        gameToQuit.Player1 = null;
                    }
                    else
                    {
                        gameToQuit.Player2 = null;
                    }

                    gameToQuit.GameState = "waiting";
                    gameToQuit.Authenticate = null;

                    // 发送响应
                    string jsonResponse = JsonSerializer.Serialize(new { status = "success", message = "Game quit successfully" });
                    string response = MakeJsonResponse(jsonResponse);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    socket.Send(responseBytes);
                }
                else
                {
                    // 如果玩家不是游戏的一部分，返回错误信息
                    string jsonResponse = JsonSerializer.Serialize(new { status = "error", message = "Player not part of game" });
                    string response = MakeJsonResponse(jsonResponse);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    socket.Send(responseBytes);
                }
            }
            else
            {
                // 如果游戏记录不存在，返回错误信息
                string jsonResponse = JsonSerializer.Serialize(new { status = "error", message = "Game not found or already ended" });
                string response = MakeJsonResponse(jsonResponse);
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                socket.Send(responseBytes);
            }
        }





    }
}
