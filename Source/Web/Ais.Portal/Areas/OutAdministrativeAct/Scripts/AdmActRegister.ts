import { rebindEvent, openKendoWindow, requestOptional, createKendoDialog, getPathToActionMethod, openKendoDialog } from "scripts/Utilities/core";
import { confirmDelete, getGrid, getGridBySearchQueryId, getSelectedItemByGrid, getSelectedItemByTr } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".info-admact-js", onOpenInfo);
    rebindEvent("click", ".open-model-version-js", openModelVersion)

    $(function () {
        $(".mainGroup").each(function () {
            if ($(this).find(".uploadedFile").length == 0) {
                $(this).remove();
            }

            let emptyUpload = $(this).find(".k-upload-empty")
            if (emptyUpload.length == 0) {
                $(emptyUpload).remove();
            }
        })
    })
}
function onOpenInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    openKendoWindow(
        "Info",
        "AdmActRegister",
        {
            area: "OutAdministrativeAct",
            type: "GET",
            data: {
                id: sender.data("id"),
            },
        },
        {
            title: `${getResource("Info")} ${sender.data("regnum")}`,
            resizable: true,
            modal: true,
            height: "90%",
            width: "90%",
        }
    )
}

function openModelVersion(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    openKendoWindow(
        "VersionInfo",
        "AdmActRegister",
        {
            area: "OutAdministrativeAct",
            type: "GET",
            data: {
                fileId: sender.data("fileid")
            },
        },
        {
            title: `${getResource("Info")} ${sender.data("name")} - ${getResource("Version")} : ${sender.data("date")}`,
            resizable: true,
            modal: true,
            height: "90%",
            width: "90%",
        }
    )
}

init();

