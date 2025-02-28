import { rebindEvent, openKendoWindow, requestOptional } from  "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, confirmDelete } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";


function init() {
    rebindEvent("click", ".edit-translation-js, .add-translation-js", upsertTranslation);
    rebindEvent("click", ".delete-translation-js", onDeleteTranslation)
}

function upsertTranslation(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
  
    let searchQueryId = sender.data("searchqueryid");
    let id, item;
    let isEdit = sender.hasClass("edit-translation-js");
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
        isEdit ? "Edit" : "Create",
        "Translations",
        {
            area: "Admin",
            data: {
                id: id,
                searchQueryId: searchQueryId
            }
        },
        {
            title: isEdit ? `${getResource("EditOf")} ${item.get("Key")}` : getResource("AddTranslation"),
            resizable: true,
            modal: true
        }
    )
}

function onDeleteTranslation(e: JQuery.EventBase): void {
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
                "Translations",
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