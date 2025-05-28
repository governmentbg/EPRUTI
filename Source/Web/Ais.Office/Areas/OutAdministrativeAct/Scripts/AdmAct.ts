import { rebindEvent, openKendoWindow, requestOptional, createKendoDialog, getPathToActionMethod, openKendoDialog, onFileUploadRemove, ShowConfirmDialogBeforeAction } from "scripts/Utilities/core";
import { confirmDelete, getGrid, getGridBySearchQueryId, getSelectedItemByGrid, getSelectedItemByTr } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".info-admact-js", onOpenInfo);
    rebindEvent("click", ".edit-outdoc-js", onEditAdmAct);
    rebindEvent("click", ".edit-applicant-js", onEditApplicantClick);
    rebindEvent("click", ".edit-admact-state-js", onEditAdmActState);
    rebindEvent("click", ".delete-admact-history-row-js", ShowConfirmDialogBeforeAction(OnDeleteAdmActState, getResource("RemoveAdmActStateRowTitle"), getResource("ConfirmRemoveAdmActStateRow")));
    rebindEvent("click", ".remove-attachment-state-js", ShowConfirmDialogBeforeAction(onFileUploadRemove, getResource("RemoveFileTitle"), getResource("ConfirmRemoveFile")));
    rebindEvent("click", ".add-admact-state", addAdmActStateRow)
    rebindEvent("click", ".open-model-version-js", openModelVersion)
}
function onEditApplicantClick(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    openKendoWindow(
        "Edit",
        "Clients",
        {
            type: "GET",
            area: "Admin",
            data: {
                id: sender.val()
            }
        },
        {
            modal: true,
            title: `${getResource("EditOf")} ${sender.data("name")}`,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    let form = sender.closest("form");
                    let grid = kendo.widgetInstance(form.find("#recipients")) as kendo.ui.Grid;
                    if (grid) {
                        grid.dataSource.read();
                    }

                    displayMessage(getResource("Success"), messageType.success);
                }
            }
        });
}

function onOpenInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    openKendoWindow(
        "Info",
        "OutApplication",
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

function onEditAdmAct(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    let selectedItem = getSelectedItemByGrid(grid);

    requestOptional(
        "Edit",
        "OutApplication",
        {
            area: "OutAdministrativeAct",
            type: "GET",
            data: {
                id: selectedItem.get("Id"),
                groupTypeId: selectedItem.get("OutDocGroupTypeId")
            },
            useArea: false,
        }
    );
}

function onEditAdmActState(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    let selectedItem = getSelectedItemByGrid(grid);

    openKendoWindow(
        "AdmActStateUpsert",
        "OutApplication",
        {
            type: "GET",
            area: "OutAdministrativeAct",
            data: {
                admActId: selectedItem.get("Id")
            }
        },
        {
            modal: true,
            title: `${getResource("EditOf")} ${getResource("AdmActStatus")}`,
            open: (e) => {
                e.sender.wrapper.css({
                    top: 100
                });
            },
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    let form = sender.closest("form");
                    let grid = kendo.widgetInstance(form.find("#recipients")) as kendo.ui.Grid;
                    if (grid) {
                        grid.dataSource.read();
                    }

                    displayMessage(getResource("Success"), messageType.success);
                }
            }
        });
}

function OnDeleteAdmActState(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    if (!grid) return;
    let row = sender.closest("tr");
    let rowData = grid.dataItem(row);
    let rowId = rowData.get("Id");
    if (rowId) {
        requestOptional(
            "AdmActStateHistoryDeleteRow",
            "OutApplication",
            {
                area: "OutAdministrativeAct",
                type: "POST",
                data: {
                    rowId: rowId
                },
                success: (data) => {
                    if (data.success) {
                        grid.dataSource.read()
                    }
                }
            });
    }
}

function addAdmActStateRow(e: JQuery.EventBase): void {
    e.preventDefault();

    let stateDropDownId = $("#StateUpsertModel_State_Id").data("kendoDropDownList").value();
    let data = {
        State: {
            Id: stateDropDownId,
            Name: $("#StateUpsertModel_State_Id").data("kendoDropDownList").text()
        },
        Date: $("#StateUpsertModel_ChangeDate").val(),
    };

    if (String(stateDropDownId).trim() === getDisputedGUID()) {
        data["Dispute"] = {
            Description: $("#StateUpsertModel_Dispute_Description").val(),
            Attachment: {
                Url: $(".k-file-success").find("input[name$='Url']").val(),
                Name: $(".k-file-success").find("input[name$='Name']").val(),
                Size: $(".k-file-success").find("input[name$='Size']").val()
            }
        }
    }

    requestOptional(
        "AdmActStateHistoryAddRow",
        "OutApplication",
        {
            area: "OutAdministrativeAct",
            type: "POST",
            data: data,
            success: (data) => {
                if (data.success) {
                    let grid = $("#grid").data("kendoGrid") as kendo.ui.Grid;
                    grid.dataSource.read();
                    onFileUploadRemove(e);
                    $("#StateUpsertModel_Dispute_Description").val('');
                }
            }
        });
}

function openModelVersion(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    openKendoWindow(
        "VersionInfo",
        "OutApplication",
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


function getDisputedGUID() {
    return "c87aa861-856f-4763-a8ab-b9f74faa1302";
}

init();

export function onAdmActStateChange(e: kendo.ui.DropDownListSelectEvent): void {
    e.preventDefault();
    let selectedValue = e.sender.value();
    let disputeContainer = $("#disputeContainer");

    if (String(selectedValue).trim() === getDisputedGUID()) {
        disputeContainer.show();
    } else {
        disputeContainer.hide();
    }
}