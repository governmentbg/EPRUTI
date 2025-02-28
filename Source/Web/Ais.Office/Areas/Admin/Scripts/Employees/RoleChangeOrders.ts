import { rebindEvent, openKendoWindow, requestOptional, createKendoDialog } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, confirmDelete, getSelectedItemByTr, getGridBySearchQueryId } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { MessageType } from "@microsoft/signalr";


function init() {
    rebindEvent("click", ".edit-role-order-js", onUpdateOrder);
    rebindEvent("click", ".edit-employees-roles-js", onEditRole);
    rebindEvent("click", ".add-employees-roles-js", onAddEmployee);
    rebindEvent("click", ".remove-employee-js", onRemoveEmployee);
    rebindEvent("click", ".select-all-js, .select-employee-js", onSelectEmployee);
    rebindEvent("click", ".add-employee-to-role-change-js", addEmployeeToAddGrid);
    rebindEvent("click", ".remove-employee-to-role-change-js", removeEmployeeToAddGrid);
    rebindEvent("click", ".delete-role-order-js", onDeleteOrder);
}

function onUpdateOrder(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }

    if (!item.get("EnableEdit")) {
        displayMessage(getResource("OrdersOfTypeClientsCannotBeEddited"), messageType.warning);
        return;
    }

    let searchQueryId = grid.element.data("searchqueryid");
    let id = item.get("Id");


    openKendoWindow(
        "ChangeRoles",
        "RoleChangeOrders",
        {
            area: "Admin",
            data: {
                id: id,
                searchQueryId: searchQueryId
            }
        },
        {
            title: getResource("SetRoles"),
            resizable: true,
            scrollable: true,
            modal: true,
        }
    )
}

function onAddEmployee(e: JQuery.EventBase) {
    e.preventDefault();
    openKendoWindow(
        "SearchEmployeeToUpdateRoles",
        "RoleChangeOrders",
        {
            data: {
                uniqueSessionId: $("#UniqueId").val()
            },
            area: "Admin"
        },
        {
            title: getResource("AddEmployee"),
            resizable: true,
            scrollable: true,
            modal: true,
        })
}

function onRemoveEmployee(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = $("#employeesGrid").data("kendoGrid") as kendo.ui.Grid;
    let item = getSelectedItemByTr(sender.closest("tr"));
    let actions = [
        {
            text: getResource("Yes"),
            action: () => {
                requestOptional("RemoveEmployeeFromChangeRoleGrid", "RoleChangeOrders", {
                    type: "POST",
                    async: true,
                    area: "Admin",
                    data:
                    {
                        id: item.get("Employee.Id"),
                        uniqueSessionId: $("#UniqueId").val()
                    },
                    success: () => {
                        grid.dataSource.read();
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                })
                return true;
            }
        },
        {
            text: getResource("No")
        }
    ];

    createKendoDialog({
        title: getResource("Deleting"),
        content: getResource("ConfirmDeleting"),
        visible: true,
        actions: actions
    });
}

function onEditRole(e: JQuery.EventBase) {
    e.preventDefault();
    openKendoWindow(
        "EditEmployeesRoles",
        "RoleChangeOrders",
        {
            data: {
                uniqueSessionId: $("#UniqueId").val(),
            },
            area: "Admin",
        },
        {
            title: getResource("EditEmployeesRoles"),
            resizable: true,
            scrollable: true,
            modal: true,
            close: () => {
                let grid = $("#employeesGrid").data("kendoGrid") as kendo.ui.Grid;
                grid.dataSource.read();
            }
        })
}

function onSelectEmployee(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = $("#employeesGrid").data("kendoGrid") as kendo.ui.Grid;

    if (!grid) {
        return;
    }

    let action = sender.hasClass("select-all-js") ? "SelectAllEmployees" : "SelectEmployee";
    let checked = sender.prop("checked");

    requestOptional(
        action,
        "RoleChangeOrders",
        {
            type: "POST",
            async: true,
            area: "Admin",
            data: {
                id: sender.hasClass("select-all-js") ? null : sender.data("id"),
                uniqueSessionId: $("#UniqueId").val(),
                isChecked: checked
            },
            success: () => {
                grid.dataSource.read();
            }
        })

}

function addEmployeeToAddGrid(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let dataItem = getSelectedItemByTr(sender.closest("tr"));
    if (dataItem) {
        let grid = $("#employeesToAddGrid").data("kendoGrid") as kendo.ui.Grid;

        let hasItem = grid.dataSource.data().find(x => (x as kendo.data.ObservableObject).get("Id") == dataItem.get("Id"))
        if (hasItem) {
            displayMessage(getResource("ElementAlreadyAdded"), messageType.warning);
            return null;
        }

        grid.dataSource.add(dataItem);
    }
}


function removeEmployeeToAddGrid(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let dataItem = getSelectedItemByTr(sender.closest("tr"));
    if (dataItem) {
        let grid = $("#employeesToAddGrid").data("kendoGrid") as kendo.ui.Grid;
        grid.dataSource.remove(dataItem);
    }
}

function onDeleteOrder(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    let searchQueryId = grid.element.data("searchqueryid");
    let item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }

    confirmDelete(
        () => {
            requestOptional(
                "Delete",
                "RoleChangeOrders",
                {
                    type: "DELETE",
                    area: "Admin",
                    data: {
                        id: item.get("Id"),
                        searchQueryId: searchQueryId
                    },
                    success: () => {
                        grid.dataSource.read();
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                });
            return true;
        });
}

init();

export function onEmployeesRolesListViewDataBound(e: any): void {
    let dataItems = e.sender.dataItems();
    for (var i = 0; i < dataItems.length; i++) {
        let isChecked = dataItems[i].get("IsChecked");
        if (isChecked == true) {
            $(`#${dataItems[i].get("Id")}`).prop("checked", true);
        } else if (isChecked == false) {
            $(`#${dataItems[i].get("Id")}`).prop("checked", false);
        } else if (isChecked == null) {
            $(`#${dataItems[i].get("Id")}`).prop("indeterminate", true);
            $(`#${dataItems[i].get("Id")}`).val(null);
        }
    }
    if (e.sender.dataItems().length > 0) {
        e.sender.element.parent().show();
    } else {
        e.sender.element.parent().hide();
    }
}

export function onEmployeesRolesCheckboxChange(e: kendo.ui.CheckBoxChangeEvent): void {
    let senderId = e.sender.element.prop("id");
    $(`#checkbox_${senderId}`).val(e.checked);
}

export function onSearchSuccess(res: any): void {
    let resultGrid = $("#searchEmployeesGrid").data("kendoGrid");
    resultGrid.dataSource.data(res.Data);
}

export function onAddedEmployeesSuccess(): void {
    let kWindow = $(".k-window-content:last").data("kendoWindow");
    kWindow.close();
    let grid = $("#employeesGrid").data("kendoGrid") as kendo.ui.Grid;
    grid.dataSource.read();
}

export function onRoleChangeSuccess(res): void {
    var kWindow = $(".k-window-content").data("kendoWindow") as kendo.ui.Window;
    if (!kWindow) {
        return;
    }

    var success = res.success === true;
    if (success === false) {
        kWindow.content(res["result"] ? res["result"] : res);
    } else {
        kWindow.close();
        $("input[name='Number']").val(res.number);
        $("#searchTablebutton").click();
    }
}