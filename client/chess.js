//chess move --ok
const move = (event) => {
    event.dataTransfer.setData("text/plain", event.target.id)
}

// 使用一个对象来保存棋子的位置
let piecePositions = {};

const drop = (event) => {
    if (event.dataTransfer !== null) {
        const data = event.dataTransfer.getData("text/plain")
        const targetID = event.target.id;
        console.log(targetID)

        event.target.appendChild(document.getElementById(data))

        //更新棋子的位           
        piecePositions[data] = targetID;
        lastMove = { piece: data, position: targetID }
    }
}

const over = (event) => {
    event.preventDefault();
}
const remove = (event) => {
    if (event.dataTransfer !== null) {
        const data = event.dataTransfer.getData("text/plain")
        document.getElementById(data).remove()
    }
}

let lastMove = "";
let currentUser = null;
let currentGameId = null;

//server的服务端口号
//点击trygame进行第一次交互，server给出用户名
const Try_Game = () => {
    fetch("http://localhost:8000/register").then(response => {
        //判断是否收到回复
        if (response.ok) {
            document.getElementById('Try_Game').style.display = 'none';
            document.getElementById('Pair_player').style.display = 'block';
            document.getElementById('Send_my_move').style.display = 'block';
            document.getElementById('Get_their_move').style.display = 'block';
            document.getElementById('Quit_Game').style.display = 'block';
            return response.text();
        }
        else {
            throw new Error("Error: " + response.statusText);
        }
    })
        .then(username => {
            //把返回的username先打印在控制台
            console.log(username);
            currentUser = username; // 新增：保存用户名
            window.alert(username + " Login");
        })
        .catch(error => {
            //如果有错误，打印错误
            console.log(error)
        })
}

//点击pairme进行一个匹配机制
const Pair_player = (username) => {
    fetch(`http://localhost:8000/pairme?player=${currentUser}`)
        .then(response => {
            if (response.ok) {
                return response.json();
            }
            else {
                throw new Error(response.statusText);
            }
        })
        .then(gameRecord => {
            //gameRecord需要包含gameId,gameState,player1,player2,player1LastMove, player2LastMove
            console.log(gameRecord);
            // 开始定时查询游戏状态
            startPolling(currentUser);
            if (gameRecord.GameState === "wait") {
                window.alert("Waiting for another player to join...");
            }
            else if (gameRecord.status === "inline") {
                window.alert("You have alread in the line");
            }
            else if (gameRecord.GameState === "progress") {
                window.alert(gameRecord.Player1 + " is playing with " + gameRecord.Player2);
            }
        })
        .catch(error => console.log(error));
}

// 保存定时器的 ID，以便之后可以取消它
let pollingIntervalId = null;
// 检查游戏状态
const checkGameState = (username) => {
    fetch(`http://localhost:8000/gamestate?player=${currentUser}`)
        .then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error(response.statusText);
            }
        })
        .then(gameRecord => {
            console.log(gameRecord)
            // 在这里更新GameId
            currentGameId = gameRecord.GameId;
            const messageElement = document.getElementById('gameStateMessage');
            if (gameRecord.GameState === "wait") {
                messageElement.innerText = "Waiting for another player to join...";
            } else if (gameRecord.GameState === "progress") {
                document.getElementById('Pair_player').style.display = 'none';
                if (gameRecord.Player1 != currentUser) {
                    messageElement.innerText = "Game is in progress with player: " + gameRecord.Player1;
                }
                else {
                    messageElement.innerText = "Game is in progress with player: " + gameRecord.Player2;
                }
                clearInterval(pollingIntervalId)
            }
        })
        .catch(error => console.log(error));
};

// 轮询服务器，检查游戏状态
const startPolling = (username) => {
    pollingIntervalId = setInterval(() => {
        checkGameState(username);
    }, 2000); // 每2秒检查一次
};

// 处理 Send_My_Move
const Send_my_move = (username, move) => {

    // 检查 lastMove 是否已定义
    if (!lastMove) {
        window.alert("Please make a move before sending");
        return;
    }
    const moveString = JSON.stringify(lastMove);

    fetch(`http://localhost:8000/mymove?player=${currentUser}&id=${currentGameId}&move=${encodeURIComponent(moveString)}`)
        .then(response => response.json())
        .then(gameRecord => {
            console.log(gameRecord)
            round = gameRecord.Authenticate;
            const message = gameRecord.message; // 获取错误消息

            //检查当前player的LastMove是否是null
            if (currentUser != round && lastMove != null) {
                window.alert("You have moved")
            }
            else if (message == "No move made.") {
                window.alert("You haven't moved")
            }
            else if (message == "Not your turn.") {
                window.alert("It is not your round")
            }
        })
        .catch(error => console.log(error));

}
