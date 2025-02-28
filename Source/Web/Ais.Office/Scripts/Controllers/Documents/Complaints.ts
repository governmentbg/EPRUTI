import { rebindEvent, openKendoWindow } from "scripts/Utilities/core";
import { getGrid } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";

function init() {
    rebindEvent("click", ".info-complaint-js", onOpenInfo);
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
        "Complaints",
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

init();