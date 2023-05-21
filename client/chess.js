//chess game move--bingo
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

let gameId = "";
let lastMove = "";

//server的服务端口号
const serverUrl = "ws://your_server_ip:your_server_port";
let socket;

//点击trygame进行第一次交互，server给出用户名
const Try_Game = () => {
    socket = new WebSocket(serverUrl);

    socket.onopen = (event)=>{
        socket.send(JSON.stringify({
            action:"login",          
        }))
    }

    socket.onmessage = (event) =>{
        const data = JSON.parse(event.data);
        Login(data);
    }
}

const Login = (data) =>{
    if(data.action == login){
        document.getElementById("userLogin").innerHTML == data.username;
        userName = data.username;
        loginState = true;
        window.alert("You have been login in")       
    }
}

const Send_my_move = () => {
    var url = "https://cws.auckland.ac.nz/gas/api/MyMove";
    let Mymove = {
        'gameID': gameId,
        'move': document.getElementById("board").innerHTML
    }
    if (document.getElementById("board").innerHTML === lastMove) {
        window.alert("You haven't move")
    }
    else {
        console.log(document.getElementById("board").innerHTML);
        fetch(url, {
            method: "POST",
            body: JSON.stringify(Mymove),
            headers: {
                'content-type': 'application/json; charset=utf-8',
                'Authorization': 'Basic ' + btoa(`${userName}:${password}`)
            }
        }).then(res => res.text())
        document.getElementById("Send_my_move").style.display = "none";
        document.getElementById("Get_their_move").style.display = "block";
        lastMove = document.getElementById("board").innerHTML;
    }
}

const Get_their_move = () => {
    var url = `https://cws.auckland.ac.nz/gas/api/TheirMove?gameId=${gameId}`
    fetch(url, {
        method: "GET",
        headers: {
            'Authorization': 'Basic ' + btoa(`${userName}:${password}`)
        }
    }).then(res => res.text())
        .then(data => {
            if (data === ("(no such gameId)")) {
                console.log("quite game")
            }
            else if (data === '') {
                window.alert("Your opponent has not moved yet.")
            }
            else {
                document.getElementById("board").innerHTML = data;
                document.getElementById("Send_my_move").style.display = "block";
                document.getElementById("Get_their_move").style.display = "none";
            }
            lastMove = document.getElementById("board").innerHTML;
        })
}
function Quit_Game() {
    var url = `https://cws.auckland.ac.nz/gas/api/QuitGame?gameId=${gameId}`
    fetch(url, {
        method: "GET",
        headers: {
            'content-type': 'application/json; charset=utf-8',
            'Authorization': 'Basic ' + btoa(`${userName}:${password}`)
        }
    }).then(function (res) {
        return res.text();
    }).then(function (data) {
        window.alert(data);
        document.getElementById("Get_their_move").style.display = "none";
        document.getElementById("Send_my_move").style.display = "none";
        document.getElementById("Quit_Game").style.display = "none";
        document.getElementById('Try_Game').style.display = 'block';
    })
}