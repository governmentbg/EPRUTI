////import * as signalR from "@microsoft/signalr";

////let connectionStarted: boolean = false;
////const connection = new signalR.HubConnectionBuilder()
////    .withAutomaticReconnect()
////    .withUrl("/signalr")
////    .configureLogging(signalR.LogLevel.Warning)
////    .build();

////function init(): void {
////    connection.start().then(x => {
////        connectionStarted = true;
////        let event = document.createEvent("Event");
////        event.initEvent("signalRConnected", false, true);
////        document.dispatchEvent(event);
////    });
////}

////init();

////export function attachToHub(func: Function): void {
////    if (connectionStarted == true) {
////        func();
////    } else {
////        $(document).on("signalRConnected", function () {
////            func();
////        });
////    }
////}