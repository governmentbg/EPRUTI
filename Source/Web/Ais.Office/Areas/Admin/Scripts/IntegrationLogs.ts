
import { rebindEvent, openKendoWindow } from "scripts/Utilities/core";
import { getGrid } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";

function init() {
    rebindEvent("click", ".info-log-js", openInfo)
}

function openInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    openKendoWindow(
        "Info",
        "IntegrationLogs",
        {
            area: "Admin",
            data: {
                id: sender.data("id"),
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