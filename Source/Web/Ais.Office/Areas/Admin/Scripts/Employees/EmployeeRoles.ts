import { rebindEvent, openKendoWindow, requestOptional } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, confirmDelete } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".edit-role-js, .add-role-js", onUpsertRole);
    rebindEvent("click", ".delete-role-js", onDeleteRole);
    rebindEvent("change", ".select-activity-js, .select-all-js", onSelectActivity);
    rebindEvent("click", ".info-employeerole-js", onOpenRoleInfo);
}

function onUpsertRole(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)

    let searchQueryId = sender.data("searchqueryid");
    let id, item;
    let isEdit = sender.hasClass("edit-role-js");
    if (isEdit) {
        let grid = getGrid(sender);
        if (!grid) {
            return;
        }

        item = getSelectedItemByGrid(grid);
        if (!item) {
            return;
        }

        id = item.get("Id");
    }

    openKendoWindow(
        "Upsert",
        "EmployeeRoles",
        {
            area: "Admin",
            data: {
                id: id,
                searchQueryId: searchQueryId
            }
        },
        {
            title: isEdit ? `${getResource("EditOf")} ${item.get("Name")}` : getResource("AddRole"),
            width: "80%",
            height: "90%",
            resizable: true,
            scrollable: true,
        }
    )
}

function onDeleteRole(e: JQuery.EventBase): void {
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
                "EmployeeRoles",
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

function onSelectActivity(e: JQuery.EventBase) {
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let isChecked = sender.prop("checked");
    let tr = sender.closest("tr");
    let dataItem = grid.dataItem(tr as JQuery);
    let isTaskType = grid.element.attr("id") == "task-activities";
    let groupCode = sender.data("group-code");
    let isAll = sender.hasClass("select-all-js");
    requestOptional(
        isTaskType ? "ChangeTaskActivity" : "ChangeActivity",
        "EmployeeRoles",
        {
            area: "Admin",
            type: "POST",
            data: {
                ids: isAll ? grid.dataSource.data().map(x => x['Id']) : [ dataItem.get("Id") ],
                isChecked,
                uniqueId: $("#UniqueId").val(),
                groupCode,
            },
            success: (res) => {
                if (res.success) {
                    grid.dataSource.read()
                    grid.refresh()
                }
            }
        })
}

function onCollapse(e: JQuery.EventBase) {
    let sender = $(e.currentTarget);
    let content = sender.next();
    content.hasClass("hidden") ? content.removeClass("hidden") : content.addClass("hidden");
}

function onOpenRoleInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let id = sender.data("id")
    openKendoWindow(
        "Info",
        "EmployeeRoles",
        {
            area: "Admin",
            data: {
                id: id,
            }
        },
        {
            title: `${getResource("Info")} - ${sender.text()}`,
            resizable: true,
            modal: true,
        }
    )
}


init();