import { rebindEvent, openKendoWindow, requestOptional, openKendoWindowUrl} from "scripts/Utilities/core";
import { getResource } from "scripts/Utilities/resources";
import { confirmDelete, getGrid, getKendoWidget, getSelectedItemByGrid } from "scripts/Utilities/searchTable";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".set-roles-js", onSetRoles);
    rebindEvent("click", ".add-employee-js", onAddEmployee);
    rebindEvent("click", ".edit-employee-js", onEditEmployee);
    rebindEvent("click", ".edit-o-and-s-js", onEditOverTimeAndSubstitutionClick);
    rebindEvent("click", ".add-overtimes-js", onAddOverTimesClick);
    rebindEvent("click", ".edit-overtime-js", onEditOverTimeClick);
    rebindEvent("click", ".remove-overtime-js", DeleteOverTime);
    rebindEvent("click", ".remove-substitution-js", DeleteSubstitution);
    rebindEvent("click", ".add-substitution-js, .edit-substitution-js", onAddSubstitutionClick);
}

function onAddSubstitutionClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let isEdit = sender.hasClass("edit-substitution-js");
    let data = {};
    if (isEdit) {
        data = {
            id: sender.data("id"),
            employeeId: sender.data("employeeid")
        };
    } else {
        data = {
            id: null,
            employeeId: sender.data("employeeid")
        };
    }

    openKendoWindow(
        "UpsertSubstitution",
        "Employees",
        {
            area: "Admin",
            data: data
        },
        {
            title: isEdit ? getResource("EditingSubstitution") : getResource("AddingSubstitution"),
            width: "80%",
            resizable: true,
            modal: true,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    displayMessage(getResource("Success"), messageType.success);
                    let grid = getKendoWidget($("#SubstitutionsGrid")) as kendo.ui.Grid;
                    if (grid) {
                        grid.dataSource.read();
                    }
                }
            }
        }
    );
}

function DeleteSubstitution(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);

    confirmDelete(
        () => {
            requestOptional(
                "DeleteSubstitution",
                "Employees",
                {
                    type: "DELETE",
                    area: "Admin",
                    useArea: true,
                    data: {
                        id: sender.data("id"),
                        employeeId: sender.data("employeeid")
                    },
                    success: () => {
                        let grid = getKendoWidget($("#SubstitutionsGrid")) as kendo.ui.Grid;
                        if (grid) {
                            grid.dataSource.read();
                            displayMessage(getResource("DeleteSuccess"), messageType.success);
                        }
                    }
                });
            return true;
        });
}

function DeleteOverTime(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);

    confirmDelete(
        () => {
            requestOptional(
                "DeleteOverTime",
                "Employees",
                {
                    type: "DELETE",
                    area: "Admin",
                    useArea: true,
                    data: {
                        id: sender.data("id"),
                        employeeId: sender.data("employeeid")
                    },
                    success: () => {
                        let grid = getKendoWidget($("#OverTimesGrid")) as kendo.ui.Grid;
                        if (grid) {
                            grid.dataSource.read();
                            displayMessage(getResource("DeleteSuccess"), messageType.success);
                        }
                    }
                });
            return true;
        });
}

function onEditOverTimeClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    openKendoWindow(
        "UpdateOverTimeRow",
        "Employees",
        {
            area: "Admin",
            data: {
                id: sender.data("id"),
                employeeId: sender.data("employeeid")
            }
        },
        {
            title: getResource("EditingOverTimes"),
            width: "80%",
            resizable: true,
            modal: true,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    displayMessage(getResource("Success"), messageType.success);
                    let grid = getKendoWidget($("#OverTimesGrid")) as kendo.ui.Grid;
                    if (grid) {
                        grid.dataSource.read();
                    }
                }
            }
        }
    );
}

function onAddOverTimesClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    openKendoWindow(
        "AddOverTimes",
        "Employees",
        {
            area: "Admin",
            data: {
                employeeId: sender.data("employeeid")
            }
        },
        {
            title: getResource("AddingOverTimes"),
            width: "80%",
            resizable: true,
            modal: true,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    displayMessage(getResource("Success"), messageType.success);
                    let grid = getKendoWidget($("#OverTimesGrid")) as kendo.ui.Grid;
                    if (grid) {
                        grid.dataSource.read();
                    }
                }
            }
        }
    );
}

function onEditOverTimeAndSubstitutionClick(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    let dataItem = getSelectedItemByGrid(grid);
    if (!dataItem) {
        return;
    }

    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                employeeId: dataItem["Id"]
            }
        },
        {
            title: getResource("Settings"),
            width: "80%",
            resizable: true,
            modal: true,
        }
    );
}

function onSetRoles(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget)
    let searchQueryId = sender.data("searchqueryid");
    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
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

function onAddEmployee(e: JQuery.EventBase) :void{
    e.preventDefault();
    let sender = $(e.currentTarget)
    let searchQueryId = sender.data("searchqueryid");
    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                searchQueryId: searchQueryId
            }
        },
        {
            title: getResource("AddEmployeee"),
            resizable: true,
            scrollable: true,
            modal: true,
        }
    )
}

function onEditEmployee(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget)
    let searchQueryId = sender.data("searchqueryid");
    let grid = getGrid(sender);
    let item = getSelectedItemByGrid(grid);
    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                id: item.get("Id"),
                searchQueryId: searchQueryId
            }
        },
        {
            title: `${getResource("EditOf")} ${item.get("FullName")}`,
            resizable: true,
            scrollable: true,
            modal: true,
        }
    )
}

init();

export function onUseNewPasswordClick(e: kendo.ui.CheckBoxChangeEvent) {
    if (e.checked) {
        $("#pass-input").removeClass("hidden");
    } else {
        $("#pass-input").addClass("hidden");
    }
}