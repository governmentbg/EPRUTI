import { rebindEvent, openKendoWindow, openKendoDialog, requestOptional, ShowConfirmDialogBeforeAction } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, getSelectedItemByTr, removeListViewItem, getGridBySearchQueryId, confirmDelete } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { MessageType } from "@microsoft/signalr";

function init() {
    rebindEvent("click", ".edit-employee-js", editEmployee);
    rebindEvent("click", ".edit-employeeadmin-js", editEmployeeAdmin);
    rebindEvent("click", ".info-employee-js", onOpenEmployeeInfo);
    rebindEvent("click", ".info-employeeAdmin-js", onOpenEmployeeInfoAdmin);
}

function editEmployee(e: JQuery.EventBase): void {
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
        openKendoWindow(
            "Upsert",
            "RegistrationEmployees",
            {
                area: "Admin",
                data: {
                    id: id,
                    searchQueryId: sender.data("searchqueryid")
                }
            },
            {
                title: getResource("ManageRegistrationEmployee"),
                width: "80%",
                resizable: true,
                modal: true,
            }
        )    
}

function onOpenEmployeeInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let id = sender.data("id")
    openKendoWindow(
        "Info",
        "RegistrationEmployees",
        {
            area: "Admin",
            data: {
                id: id,
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

function editEmployeeAdmin(e: JQuery.EventBase): void {
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
    openKendoWindow(
        "Upsert",
        "RegistrationEmployeesAdmin",
        {
            area: "Admin",
            data: {
                id: id,
                searchQueryId: sender.data("searchqueryid")
            }
        },
        {
            title: getResource("ManageRegistrationEmployeeAdmin"),
            width: "500px",
            resizable: true,
            modal: true,
        }
    )
}

function onOpenEmployeeInfoAdmin(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let id = sender.data("id")
    openKendoWindow(
        "Info",
        "RegistrationEmployeesAdmin",
        {
            area: "Admin",
            data: {
                id: id,
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
