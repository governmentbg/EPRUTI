import { openKendoWindowUrl, rebindEvent, requestOptionalUrl, ShowConfirmDialogBeforeAction } from "scripts/Utilities/core";
import { getResource } from "scripts/Utilities/resources";
import { getGridBySearchQueryId, getSelectedItemByGrid, removeListViewItem } from "scripts/Utilities/searchTable";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".edit-field-js", editField);
    rebindEvent("click", ".upsert-field-js", upsertField);
    rebindEvent("click", ".delete-field-js", ShowConfirmDialogBeforeAction(deleteField, getResource("Delete"), getResource("ConfirmDelete")));
}

function editField(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);

    let id, item, docTypeId;
    let grid = getGridBySearchQueryId(sender.data("searchqueryid"));
    if (!grid) {
        return;
    }

    item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }

    id = item.get("Id");
    docTypeId = item.get("DocTypeId");
    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                validationId: id,
                docTypeId,
                searchQueryId: sender.data("searchqueryid")
            },
        },
        {
            title: `${getResource("EditOf")} ${getResource("Field")} ${item.get("Name")}`,
            width: "50%",
            resizable: true,
            modal: true,
        }
    )
}

function deleteField(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);

    let id, item, docTypeId;
    let grid = getGridBySearchQueryId(sender.data("searchqueryid"));
    if (!grid) {
        return;
    }

    item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }

    id = item.get("Id");
    docTypeId = item.get("DocTypeId");
    requestOptionalUrl(
        sender.attr("href"),
        {
            data: {
                validationId: id,
                docTypeId,
                searchQueryId: sender.data("searchqueryid")
            },
        },
    )
}

function upsertField(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);

    let docTypeId, item;
    let grid = getGridBySearchQueryId(sender.data("searchqueryid"));
    if (!grid) {
        return;
    }

    item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }

    docTypeId = item.get("DocTypeId");
    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                searchQueryId: sender.data("searchqueryid"),
                docTypeId
            },
        },
        {
            title: `${getResource("Add")} ${getResource("Field")} for ${item.get("Name")}`,
            width: "50%",
            resizable: true,
            modal: true,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    if (grid) {
                        grid.dataSource.read()
                        grid.refresh();
                    }

                    displayMessage(getResource("Success"), messageType.success);
                }
            }
        }
    )
}

init();
