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

let gameId = "";
let lastMove = "";

//server的服务端口号
//点击trygame进行第一次交互，server给出用户名
const Try_Game = () => {
    fetch("http://localhost/register").then(response =>{
        //判断是否收到回复
        if(response.ok){
            return response.text();
        }
        else{
            throw new Error("Error: "+response.statusText);
        }
    })
    .then(username =>{
        //把返回的username先打印在控制台
        console.log(username);
    })
    .catch(error =>{
        //如果有错误，打印错误
        console.log(error)
    })
}

