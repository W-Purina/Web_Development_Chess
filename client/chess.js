//chess move --ok
const move = (event) => {
    event.dataTransfer.setData("text/plain", event.target.id)
}
const drop = (event) => {
    if (event.dataTransfer !== null) {
        const data = event.dataTransfer.getData("text/plain")
        const targetID = event.target.id;
        console.log(targetID)
        if (targetID) {
        }
        else {
            event.target.appendChild(document.getElementById(data))
        }
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
            window.alert(username + " Login");
        })
        .catch(error => {
            //如果有错误，打印错误
            console.log(error)
        })
}

//点击pairme进行一个匹配机制
const Pair_player = (username) => {
    fetch(`http://localhost:8000/pairme?player=${username}`)
        .then(response =>{
            if(response.ok){
                return response.json();
            }
            else{
                throw new Error(response.statusText);
            }
        })
        .then(gameRecord =>{
            //gameRecord需要包含gameId,gameState,player1,player2,player1LastMove, player2LastMove
            console.log(gameRecord);
            if(gameRecord.gameState === "wait"){
                window.alert("Waiting for another player to join...");
            }
            else if(gameRecord.gameState === "progress"){
                window.alert("Game is in progress with player: "+gameRecord.player2);
            }        
        })
        .catch(error => console.log(error));
}
