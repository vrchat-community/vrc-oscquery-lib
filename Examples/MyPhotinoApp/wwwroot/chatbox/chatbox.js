// Would be nice to generate these from somewhere
let commands = {
    startChatbox:       "startChatbox",
    sendChatMessage:    "sendChatMessage",
    sendChatTyping:     "sendChatTyping",
    connectToServer:    "connectToServer",
    response:           "response"
};

function sendChatMessage(e){
    e.preventDefault();

    sendWebCommand(commands.sendChatMessage, inputField.value);

    inputField.value="";
}

function onReceiveMessage(message){
    const commandMessage = JSON.parse(message);

    switch (commandMessage.command)
    {
        case commands.connectToServer:
            let info = JSON.parse(commandMessage.data);
            headerText.textContent = `Sending to ${info.name} at ${info.address}:${info.port}`;
            break;
        case commands.response:
            console.log(commandMessage.data)
    }
}

// Convenience function for packing up message and sending it
function sendWebCommand(command, data){
    window.external.sendMessage(JSON.stringify(
        {
            "command": command,
            "data": data
        }
    ))
}

let headerText = document.getElementById("HeaderText");
let inputField = document.getElementById("InputField");
let inputForm = document.getElementById("FormInputField");

inputForm.addEventListener("submit", sendChatMessage);

// Send '...' updates when input field updated
inputField.addEventListener("input", ()=> sendWebCommand(commands.sendChatTyping, "true"))

window.external.receiveMessage(onReceiveMessage);

// Start up the Service
sendWebCommand(commands.startChatbox);