// 定义一个数组来记录所有的移动
let moveHistory = [];
let piecePositions = {};
let moveRecord = {};

// 在开始时，设置锁为false
let isGameActive = false;
const move = (event) => {

    // 如果锁是false，就返回并不执行后续的代码
    if (!isGameActive) {
        return;
    }

    // 在开始拖动时，记录棋子的初始位置
    const piece = event.target.id;
    const position = event.target.parentElement.id;
    event.dataTransfer.setData("text/plain", piece);
    event.dataTransfer.setData("text/initial_position", position);
}

const drop = (event) => {
    if (event.dataTransfer !== null) {
        const piece = event.dataTransfer.getData("text/plain");
        const initialPosition = event.dataTransfer.getData("text/initial_position");
        const targetID = event.target.id;

        event.target.appendChild(document.getElementById(piece));
        // 更新棋子的位置
        piecePositions[piece] = targetID;

        // 记录这个移动
        moveRecord = { Piece: piece, From: initialPosition, To: targetID };
        moveHistory.push(moveRecord);
    }
}

const over = (event) => {
    event.preventDefault();
}

const remove = (event) => {
    if (event.dataTransfer !== null) {
        const piece = event.dataTransfer.getData("text/plain");
        document.getElementById(piece).remove()
    }
}


let currentUser = null;
let currentGameId = null;

//server的服务端口号
//点击trygame进行第一次交互，检查用户是否注册
const Try_Game = () => {
    const username = document.getElementById('username_input').value;
    fetch(`http://localhost:8000/trygame?player=${encodeURIComponent(username)}`).then(response => {
        //判断是否收到回复
        if (response.ok) {
            return response.json();
        }
        else {
            throw new Error("Error: " + response.statusText);
        }
    })
        .then(data => {
            if (data.status === 'error') {
                //如果有错误，打印错误信息
                console.log(data.message)
                window.alert(data.message);
            }
            else if (data.status === 'success') {
                console.log(data.message);
                window.alert(data.message);
                currentUser = username; //保存用户名
                document.getElementById('Try_Game').style.display = 'none';
                document.getElementById('Pair_player').style.display = 'block';
                document.getElementById('Send_my_move').style.display = 'block';
                document.getElementById('Get_their_move').style.display = 'block';
                document.getElementById('Quit_Game').style.display = 'block';
            }
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
                // 在匹配到对手之后，将锁设为true
                isGameActive = true;
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
                isGameActive = true;
                document.getElementById('Pair_player').style.display = 'none';
                if (gameRecord.Player1 != currentUser) {
                    document.getElementById('Send_my_move').style.display = 'none';
                    document.getElementById('Get_their_move').style.display = 'block';
                    messageElement.innerText = "Game is in progress with player: " + gameRecord.Player1;
                }
                else {
                    document.getElementById('Send_my_move').style.display = 'block';
                    document.getElementById('Get_their_move').style.display = 'none';
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
    }, 5000); // 每2秒检查一次
};

// 处理 Send_My_Move
const Send_my_move = (username, move) => {
    // 检查 lastMove 是否已定义
    if (moveHistory.length == 0) {
        window.alert("Please make a move before sending");
        return;
    }
    const moveString = JSON.stringify(moveHistory);
    fetch(`http://localhost:8000/mymove?player=${currentUser}&id=${currentGameId}&move=${encodeURIComponent(moveString)}`)
        .then(response => response.json())
        .then(gameRecord => {
            console.log(gameRecord)
            round = gameRecord.Authenticate;
            const message = gameRecord.message; // 获取错误消息

            //检查当前player的LastMove是否是null
            if (currentUser != round && moveString != null) {
                window.alert("You have moved")
            }
            else if (message == "No move made.") {
                window.alert("You haven't moved")
            }
            else if (message == "Not your turn.") {
                window.alert("It is not your round")
            }

            document.getElementById('Send_my_move').style.display = 'none';
            document.getElementById('Get_their_move').style.display = 'block';

            // 清空 moveHistory
            moveHistory = [];
        })
        .catch(error => console.log(error));

}

//处理get_their_move
const Get_their_move = () => {
    fetch(`http://localhost:8000/theirmove?player=${(currentUser)}&id=${(currentGameId)}`)
        .then(response => {
            if (response.ok) {
                return response.json();
            }
            else {
                throw new Error("Error: " + response.statusText);
            }
        })
        .then(gameRecord => {
            // 如果请求成功，更新显示对手的最后一步
            console.log(gameRecord.moves)
            if (gameRecord.moves) {
                gameRecord.moves.forEach(move => {
                    // 对每一步棋进行处理
                    movePiece(move.Piece, move.From, move.To);
                    document.getElementById('Send_my_move').style.display = 'block';
                    document.getElementById('Get_their_move').style.display = 'none';
                });
            }
            // 如果对手还未进行移动，显示提示信息
            else if (gameRecord.message === "Game not in progress or invalid game ID") {
                document.getElementById('gameStateMessage').textContent = "You opponent has quit the game";
            }
            else if(gameRecord.message == "Not your turn."){
                document.getElementById('gameStateMessage').textContent = "You opponent hasn't move yet";
            }
            else {
                console.log(gameRecord.message);
            }
        })
        .catch(error => {
            console.log(error)
        })
}

//处理返回棋子信息的移动
function movePiece(Piece, From, To) {
    //根据pieceId获取棋子元素
    var pieceElement = document.getElementById(Piece);
    //根据to获得目标位置
    var toPositionElement = document.getElementById(To);

    // 将棋子从原位置移除
    var fromPositionElement = document.getElementById(From);
    fromPositionElement.removeChild(pieceElement);

    // 将棋子添加到新位置
    toPositionElement.appendChild(pieceElement);

}

//处理用户退出
const Quit_Game = () => {
    fetch(`http://localhost:8000/quit?player=${encodeURIComponent(currentUser)}&id=${encodeURIComponent(currentGameId)}`)
        .then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error("Error: " + response.statusText);
            }
        })
        .then(data => {
            if (data.status === 'success') {
                isGameActive = false;
                document.getElementById('Try_Game').style.display = 'block';
                document.getElementById('Pair_player').style.display = 'none';
                document.getElementById('Send_my_move').style.display = 'none';
                document.getElementById('Get_their_move').style.display = 'none';
                document.getElementById('Quit_Game').style.display = 'none';
                // 重置棋盘
                resetBoard();
                document.getElementById('gameStateMessage').innerText = "Game quit successfully";
            } else {
                alert(data.message);
            }
        })
        .catch(error => {
            console.log(error);
        });
};

//记录棋子的初始位置-用于退出重置棋盘
const pieceInitialPositionMap = {
    "img-1a": "1a", "img-1b": "1b", "img-1c": "1c", "img-1d": "1d", "img-1e": "1e", "img-1f": "1f", "img-1g": "1g", "img-1h": "1h", "img-1i": "1i",
    "img-2a": "2a", "img-2b": "2b", "img-2c": "2c", "img-2d": "2d", "img-2e": "2e", "img-2f": "2f", "img-2g": "2g", "img-2h": "2h", "img-2i": "2i",
    "img-3i": "3i",
    "img-4i": "4i",
    "img-5i": "5i",
    "img-6i": "6i",
    "img-7a": "7a", "img-7b": "7b", "img-7c": "7c", "img-7d": "7d", "img-7e": "7e", "img-7f": "7f", "img-7g": "7g", "img-7h": "7h", "img-7i": "7i",
    "img-8a": "8a", "img-8b": "8b", "img-8c": "8c", "img-8d": "8d", "img-8e": "8e", "img-8f": "8f", "img-8g": "8g", "img-8h": "8h", "img-8i": "8i",
};

// 这个对象将棋子ID映射到棋子的图片URL
const pieceIdToImageUrlMap = {
    "img-1a": "https://cws.auckland.ac.nz/gas/images/Rw.svg",
    "img-1b": "https://cws.auckland.ac.nz/gas/images/Nw.svg",
    "img-1c": "https://cws.auckland.ac.nz/gas/images/Bw.svg",
    "img-1d": "https://cws.auckland.ac.nz/gas/images/Qw.svg",
    "img-1e": "https://cws.auckland.ac.nz/gas/images/Kw.svg",
    "img-1f": "https://cws.auckland.ac.nz/gas/images/Bw.svg",
    "img-1g": "https://cws.auckland.ac.nz/gas/images/Nw.svg",
    "img-1h": "https://cws.auckland.ac.nz/gas/images/Rw.svg",
    "img-1i": "https://cws.auckland.ac.nz/gas/images/Qw.svg",
    "img-2a": "https://cws.auckland.ac.nz/gas/images/Pw.svg",
    "img-2b": "https://cws.auckland.ac.nz/gas/images/Pw.svg",
    "img-2c": "https://cws.auckland.ac.nz/gas/images/Pw.svg",
    "img-2d": "https://cws.auckland.ac.nz/gas/images/Pw.svg",
    "img-2e": "https://cws.auckland.ac.nz/gas/images/Pw.svg",
    "img-2f": "https://cws.auckland.ac.nz/gas/images/Pw.svg",
    "img-2g": "https://cws.auckland.ac.nz/gas/images/Pw.svg",
    "img-2h": "https://cws.auckland.ac.nz/gas/images/Pw.svg",
    "img-2i": "https://cws.auckland.ac.nz/gas/images/Qw.svg",
    "img-3i": "https://cws.auckland.ac.nz/gas/images/Qw.svg",
    "img-4i": "https://cws.auckland.ac.nz/gas/images/Qw.svg",
    "img-5i": "https://cws.auckland.ac.nz/gas/images/Qb.svg",
    "img-6i": "https://cws.auckland.ac.nz/gas/images/Qb.svg",
    "img-7a": "https://cws.auckland.ac.nz/gas/images/Pb.svg",
    "img-7b": "https://cws.auckland.ac.nz/gas/images/Pb.svg",
    "img-7c": "https://cws.auckland.ac.nz/gas/images/Pb.svg",
    "img-7d": "https://cws.auckland.ac.nz/gas/images/Pb.svg",
    "img-7e": "https://cws.auckland.ac.nz/gas/images/Pb.svg",
    "img-7f": "https://cws.auckland.ac.nz/gas/images/Pb.svg",
    "img-7g": "https://cws.auckland.ac.nz/gas/images/Pb.svg",
    "img-7h": "https://cws.auckland.ac.nz/gas/images/Pb.svg",
    "img-7i": "https://cws.auckland.ac.nz/gas/images/Qb.svg",
    "img-8a": "https://cws.auckland.ac.nz/gas/images/Rb.svg",
    "img-8b": "https://cws.auckland.ac.nz/gas/images/Nb.svg",
    "img-8c": "https://cws.auckland.ac.nz/gas/images/Bb.svg",
    "img-8d": "https://cws.auckland.ac.nz/gas/images/Qb.svg",
    "img-8e": "https://cws.auckland.ac.nz/gas/images/Kb.svg",
    "img-8f": "https://cws.auckland.ac.nz/gas/images/Bb.svg",
    "img-8g": "https://cws.auckland.ac.nz/gas/images/Nb.svg",
    "img-8h": "https://cws.auckland.ac.nz/gas/images/Rb.svg",
    "img-8i": "https://cws.auckland.ac.nz/gas/images/Qb.svg"
};


// 重置棋盘
const resetBoard = () => {
    // 遍历棋盘上的所有位置
    for (let i = 1; i <= 8; i++) {
        for (let j = 0; j < 8; j++) {
            let positionId = `${i}${String.fromCharCode(97 + j)}`;
            let positionElement = document.getElementById(positionId);
            // 清除该位置上的棋子
            positionElement.innerHTML = "";
        }
    }

    // 重新创建棋子并将他们放到他们的初始位置
    for (let pieceId in pieceInitialPositionMap) {
        let initialPositionId = pieceInitialPositionMap[pieceId];
        console.log(initialPositionId);
        let positionElement = document.getElementById(initialPositionId);
        // 用pieceIdToImageUrlMap来获取棋子的图片URL
        let imageUrl = pieceIdToImageUrlMap[pieceId];
        positionElement.innerHTML = `<img id="${pieceId}" src="${imageUrl}" ondragstart="move(event)" draggable="true" width="50" />`;
    }

    // 清空移动历史记录
    moveHistory = [];
}
