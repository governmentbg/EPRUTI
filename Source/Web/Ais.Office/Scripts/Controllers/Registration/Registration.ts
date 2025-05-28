import { rebindEvent, openKendoWindow, openKendoDialog, requestOptional, ShowConfirmDialogBeforeAction,createKendoDialog } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, getSelectedItemByTr, removeListViewItem, getGridBySearchQueryId, confirmDelete } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { MessageType } from "@microsoft/signalr";

function init() {
    rebindEvent("click", ".info-request-js", onOpenRequestInfo);
    rebindEvent("click", ".send-registration-request-js", onSendRegistrationRequest);
    rebindEvent("click", ".stop-registration-js", ShowConfirmDialogBeforeAction(onStopRegistration, getResource("StopRegistration"), getResource("StopRegistrationText")));
}

function onOpenRequestInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let id = sender.data("id")
    openKendoWindow(
        "Info",
        "Registration",
        {
            area: "",
            data: {
                requestId: id,
            }
        },
        {
            title: `${getResource("Info")} - ${sender.text()}`,
            resizable: true,
            modal: true,
            width: "500px",
        }
    )
}

function onStopRegistration(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget)
    let egn = sender.data("egn")
    requestOptional(
        "StopRegistration",
        "Registration",
        {
            area: "",
            type: "POST",
            data: {
                egn: egn
            },
            success: (data) => {
                window.location.href = `${sender.attr("href")}`
                displayMessage(getResource("SuccesfullyStoppedRequest"), messageType.success)
            }
        });
}

function onSendRegistrationRequest(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let form = sender.closest('form');

    let actions = [
        {
            text: getResource("Yes"),
            action: () => {
                form.submit();
            },
            primary: true
        },
        {
            text: getResource("No")
        }
    ];

    createKendoDialog(
        {
            title: getResource("SendRegistration"),
            content: getResource("AreYouSureYouWantToSendYourRegistration"),
            visible: true,
            actions: actions
        });

    //requestOptional(
    //    "StopRegistration",
    //    "Registration",
    //    {
    //        area: "",
    //        type: "POST",
    //        data: {
    //            egn: egn
    //        },
    //        success: (data) => {
    //            window.location.href = `${sender.attr("href")}`
    //            displayMessage(getResource("SuccesfullyStoppedRequest"), messageType.success)
    //        }
    //    });
}

init();
