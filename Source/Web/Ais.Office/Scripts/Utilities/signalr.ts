import * as signalR from "@microsoft/signalr";

let connectionStarted: boolean = false;
const connection = new signalR.HubConnectionBuilder()
    .withAutomaticReconnect()
    .withUrl("/signalr")
    .configureLogging(signalR.LogLevel.Warning)
    .build();

function init(): void {
    connection.start().then(x => {
        connectionStarted = true;
        const customEvent = new Event("signalRConnected", { "bubbles": false, "cancelable": true });
        document.dispatchEvent(customEvent);
    });
}

init();

export function attachToHub(func: (connection: signalR.HubConnection) => void): void {
    if (connectionStarted == true) {
        func(connection);
    } else {
        $(document).on("signalRConnected", () => {
            func(connection);
        });
    }
}