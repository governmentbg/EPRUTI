import { rebindEvent, openKendoWindow } from "scripts/Utilities/core";
import { getGrid } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";

function init() {
    rebindEvent("click", ".info-log-js", openLogInfo)
}

function openLogInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let searchQueryId = grid.element.data("searchqueryid");
    let id = sender.data("id")
    openKendoWindow(
        "Info",
        "Logs",
        {
            area:"Admin",
            data: {
                id: id,
                searchQueryId: searchQueryId
            }
        },
        {
            title: getResource("LogsInfo"),
            resizable: true,
            modal: true,
        }
    )
}

init()