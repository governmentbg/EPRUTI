import { rebindEvent, openKendoWindow, openKendoDialog, requestOptional, ShowConfirmDialogBeforeAction } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, getSelectedItemByTr, removeListViewItem, getGridBySearchQueryId, confirmDelete } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { MessageType } from "@microsoft/signalr";

function init() {
    rebindEvent("click", ".edit-request-js", editRequest);
    rebindEvent("click", ".info-request-js", onOpenRequestInfo);
}

function getRejectedGUID() {
    return "275aee8e-c414-4031-b1e8-eafcdb91e429";
}
function getWaitingGUID() {
    return "e30a839b-381f-4770-b132-d49f5b130a6e";
}

function editRequest(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);

    let id, item, statusId;
    let grid = getGridBySearchQueryId(sender.data("searchqueryid"));
    if (!grid) {
        return;
    }
    item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }
    id = item.get("Id");
    statusId = item.get("RequestStatusId");
    if (statusId === getWaitingGUID()) {
        openKendoWindow(
            "Upsert",
            "RegistrationRequests",
            {
                area: "Admin",
                data: {
                    id: id,
                    searchQueryId: sender.data("searchqueryid")
                }
            },
            {
                title: getResource("ManageRegistrationRequest"),
                width: "500px",
                resizable: true,
                modal: true,
            }
        )
    }
    else {
        displayMessage(getResource("CannotEditRequest"), messageType.warning)
    }
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
        "RegistrationRequests",
        {
            area: "Admin",
            data: {
                requestId: id,
            }
        },
        {
            title: `${getResource("Info")} - ${sender.text()}`,
            width: "500px",
            resizable: true,
            modal: true,
        }
    )
}
init();

export function rejectRequest(e: kendo.ui.DropDownListSelectEvent): void {
    e.preventDefault();
    let selectedValue = e.sender.value();
    let disputeContainer = $("#rejection-js");

    let statusCodeContainer = $("#Status_Code");
    
    if (String(selectedValue).trim() === getRejectedGUID()) {
        statusCodeContainer.val('3')
        disputeContainer.show();
    } else {
        statusCodeContainer.val('2')
        disputeContainer.hide();
    }
}