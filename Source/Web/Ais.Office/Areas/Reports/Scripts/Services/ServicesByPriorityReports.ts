import { openKendoWindow, openKendoWindowUrl, rebindEvent } from "scripts/Utilities/core";
import { getResource } from "scripts/Utilities/resources";
import { getGridBySearchQueryId, getSelectedItemByGrid, getGrid } from "scripts/Utilities/searchTable";

function init() {
    rebindEvent("click", ".update-priority-status-js", updateServicePriority);
    rebindEvent("click", ".info-indoc-js", onOpenInfo);

}

function onOpenInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    if (!grid) {
        return;
    }

    let id = sender.data("id")
    if (!id) {
        return;
    }

    openKendoWindow(
        "Info",
        "InDocuments",
        {
            data: {
                id: id
            },
            useArea: false,
        },
        {
            title: `${getResource("Info")} ${sender.text()}`,
            resizable: true,
            modal: true,
            height: "90%",
            width: "90%",
        }
    )
}

function updateServicePriority(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);

    let id, item;
    let grid = getGridBySearchQueryId(sender.data("searchqueryid"));
    if (!grid) {
        return;
    }

    item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }

    id = item.get("Id");
    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                id: id,
                searchQueryId: sender.data("searchqueryid")
            }
        },
        {
            title: getResource("ChangeConfirmStatus"),
            width: "30%",
            resizable: true,
            modal: true,
        });
}

export function onStatusChange(e: JQuery.EventBase): void {
    let reasonDesc = $(".reasonDesc");

    if (reasonDesc.hasClass("hidden")) {
        reasonDesc.removeClass("hidden");
    }
    else {
        reasonDesc.addClass("hidden");
    }
}

init();