import { rebindEvent, openKendoWindowUrl, requestOptionalUrl, requestOptional } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGridBySearchQueryId, confirmDelete, } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";

function init() {
    rebindEvent("click", ".edit-client-attachment-js, .add-client-attachment-js", upsertClientAttachment);
    rebindEvent("click", ".delete-client-attachment-js", deleteClientAttachment);
}

function upsertClientAttachment(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let searchQueryId = sender.data("searchqueryid");

    let id, item, grid;
    let isEdit = sender.hasClass("edit-client-attachment-js");
    if (isEdit) {
        grid = getGridBySearchQueryId(searchQueryId);
        if (!grid) {
            return;
        }

        item = getSelectedItemByGrid(grid);
        if (!item) {
            return;
        }

        id = item.get("Id");
    }

    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                id: id,
                searchQueryId: searchQueryId
            }
        },
        {
            title: isEdit ? `${getResource("EditOf")} ${item.get("Name")}` : getResource("AddClientAttachment"),
            width: "80%",
            resizable: true,
            modal: true,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true && grid) {
                    grid.dataSource.read();
                }
            }
        }
    )
}

function deleteClientAttachment(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let searchQueryId = sender.data("searchqueryid");
    let grid = getGridBySearchQueryId(searchQueryId);
    if (!grid) {
        return;
    }

    let item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }

    confirmDelete(
        () => {
            requestOptionalUrl(
                sender.attr("href"),
                {
                    type: "DELETE",
                    data: {
                        searchQueryId: searchQueryId,
                        id: item.get("Id")
                    },
                    success: () => {
                        grid.dataSource.read();
                    }
                });
            return true;
        });
}

init();

export const onAttachmentTypeChange = (e: kendo.ui.DropDownListChangeEvent) => {
    let replaceDiv = e.sender.wrapper.closest("form").find("div.attachment-js");
    let typeId = e.sender.value();
    if (!typeId) {
        replaceDiv.html("");
        return;
    }

    requestOptional(
        "Attachment",
        "ClientAttachments",
        {
            area: "Admin",
            type: "GET",
            data: {
                typeid: e.sender.value()
            },
            success: (data) => {
                replaceDiv.html(data ? data : "");
            }
        });
}