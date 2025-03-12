import { rebindEvent, openKendoWindow, requestOptional, createKendoDialog, getPathToActionMethod, openKendoDialog, onFileUploadRemove } from "scripts/Utilities/core";
import { confirmDelete, getGrid, getGridBySearchQueryId, getSelectedItemByGrid, getSelectedItemByTr } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".delete-outdoc-js", onDeleteOutdoc);
    rebindEvent("click", ".info-outdoc-js", onOpenInfo);
    rebindEvent("click", ".sent-clerical-work-js", onSentClericalWork);
    rebindEvent("click", ".mark-bgposts-js, .mark-edelivery-js", markSend);
    rebindEvent("change", ".checkbox-js, .checkboxAll-js", onCheckBoxChange);
    rebindEvent("click", ".info-delivery-js", onDeliveryInfoClick);
    rebindEvent("click", ".edit-delivery-js", onDeliveryEditClick);
    rebindEvent("click", ".edit-outdoc-js", onEditOutdoc);
    rebindEvent("click", ".edit-proof-doc-js", onAddProofDoc);
    rebindEvent("click", ".add-proof-doc-js", addProofDoc);
    rebindEvent("click", ".remove-proof-doc-js", onRemoveProofDoc);
    rebindEvent("click", ".archive-outdoc-js", onArchiveOutdoc);
    rebindEvent("click", ".change-outdocs-office-js", onChangeOutDocsOffice);
    rebindEvent("click", ".edit-applicant-js", onEditApplicantClick);
    rebindEvent("click", ".edit-admact-state-js", onEditAdmActState);
    rebindEvent("click", ".delete-admact-history-row-js", OnDeleteAdmActState);
    rebindEvent("click", ".remove-attachment-js", removeStateAttachment)
    rebindEvent("click", ".add-adm-act-state", addAdmActStateRow)
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

function onChangeOutDocsOffice(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    let searchQueryId = grid.element.data("searchqueryid");

    e.preventDefault();
    openKendoWindow("ChangeOffice", "OutDocuments",
        {
            type: "GET",
            data: {
                searchQueryId: searchQueryId
            },
        },
        {
            modal: true,
            width: "70%",
            height: "auto;",
            title: getResource("ChangeOffice")
        }
    );
}

function onArchiveOutdoc(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    let searchQueryId = grid.element.data("searchqueryid");

    requestOptional(
        "Archive",
        "OutDocuments",
        {
            type: "POST",
            data: {
                searchQueryId: searchQueryId
            },
            useArea: false,
            success: () => {
                grid.dataSource.read();
                displayMessage(getResource("SuccessAction"), messageType.success);
            }
        });
}

function onOpenInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    openKendoWindow(
        "Info",
        "OutDocuments",
        {
            data: {
                id: sender.data("id"),
                groupTypeId: sender.data("outgroupid"),
            },
            useArea: false,
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

function onEditOutdoc(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    let selectedItem = getSelectedItemByGrid(grid);

    requestOptional(
        "Edit",
        "OutApplication",
        {
            type: "GET",
            data: {
                id: selectedItem.get("Id"),
                groupTypeId: selectedItem.get("OutDocGroupTypeId")
            },
            useArea: false,
        }
    );
}

function onDeleteOutdoc(e: JQuery.EventBase): void {
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
                "OutDocuments",
                {
                    type: "DELETE",
                    data: {
                        id: item.get("Id"),
                        searchQueryId: searchQueryId
                    },
                    useArea: false,
                    success: () => {
                        grid.dataSource.read();
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                });
            return true;
        });
}

function onSentClericalWork(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    let items = grid.dataSource.data().filter(x => x["IsChecked"] == true);
    let regNums = items.map(x => x["RegNumber"] as string);
    if (regNums.length == 0) {
        displayMessage(getResource("NoSelectedItems"), messageType.warning);
        return;
    }
    let actions = [
        {
            text: getResource("Yes"),
            action: () => {
                requestOptional("SendToClericalWork", "OutDocuments", {
                    type: "POST",
                    async: true,
                    data:
                    {
                        searchQueryId: grid.element.data("searchqueryid")
                    },
                    success: () => {
                        grid.dataSource.read();
                        displayMessage(getResource("Success"), messageType.success);
                    }
                })
                return true;
            }
        },
        {
            text: getResource("No")
        }
    ];

    createKendoDialog({
        title: getResource("SendToClericalWork"),
        content: `${getResource("SendToClericalWorksRegnums")} - ${regNums.join(', ')}`,
        visible: true,
        actions: actions
    });
}

function markSend(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    let searchQueryId = grid.element.data("searchqueryid");
    let action = sender.hasClass("mark-bgposts-js") ? "MarkSendWithBgPosts" : "MarkSendWithEDelivery";
    requestOptional(
        action,
        "OutDocuments",
        {
            data: {
                searchQueryId: searchQueryId,
            },
            type: "POST",
            useArea: false,
            success: (res) => {
                if (res.success) {
                    grid.dataSource.read();
                }
            }
        })
}

function onCheckBoxChange(e: JQuery.EventBase): void {
    let checkBox = $(e.currentTarget);
    let checked = $(e.currentTarget).prop("checked") ? true : false;
    checkBox.val(checked.toString())
    let grid = $("#recipients").data("kendoGrid") as kendo.ui.Grid;
    if (checkBox.hasClass("checkboxAll-js")) {
        grid.dataSource.data().map(x => x["IsChecked"] = checked);
    } else {
        grid.dataSource.getByUid(checkBox.closest("tr").data("uid")).set("IsChecked", checked);
    }
    grid.refresh();
}

function onDeliveryInfoClick(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    let item = grid.dataSource.getByUid(sender.closest("tr").data("uid"));
    if (!item) {
        return;
    }

    let channel = item.get("Channel.Id");

    if (channel == "4a233b0b-0b3f-4a44-aaaf-b862d0647b96") { //// Bg posts channel id
        onTrackBgPostPackage(item);
    } else if (channel == "89cdcedb-a089-4714-8f07-bf8b28d619d9") {//// EDelivery channel id
        ////TODO
    }
}

function onTrackBgPostPackage(item: kendo.data.ObservableObject,): void {
    openKendoWindow(
        "TrackParcel",
        "OutDocuments",
        {
            data: {
                id: item.get("Id"),
                hasDeliveryDate: item.get("DeliveryDate") != null || item.get("DeliveryDate") != ''
            },
            useArea: false,
        },
        {
            title: `${getResource("TrackParcel")}`,
            resizable: true,
            modal: true,
            height: "auto",
            width: "90%"
        },
    )
}

function onDeliveryEditClick(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    let item = grid.dataSource.getByUid(sender.closest("tr").data("uid"));
    if (!item) {
        return;
    }

    openKendoWindow(
        "EditDeliveryData",
        "OutDocuments",
        {
            type: "GET",
            data: {
                id: item.get("Id")
            },
        },
        {
            modal: true,
            width: "600px",
            title: getResource("EditDeliveryData")
        }
    )
}

function onAddProofDoc(e: JQuery.EventBase): void {
    e.preventDefault();
    openKendoWindow(
        "AddProofDoc",
        "OutDocuments",
        {
            type: "GET",
            data: {
                sessionId: $("#UniqueId").val()
            },
        },
        {
            modal: true,
            width: "70%",
            height: "auto;",
            title: getResource("AddProofDoc"),
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    displayMessage(getResource("Success"), messageType.success);
                    $(".proof-doc-js:last").html(e.sender.element.data("proofdoc"))
                }
            }
        }
    )
}

function addProofDoc(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = $("#proofDocsToAdd").data("kendoGrid") as kendo.ui.Grid
    let dataItem = getSelectedItemByTr(sender.closest("tr"));
    if (dataItem) {
        let hasItem = grid.dataSource.data().find(x => (x as kendo.data.ObservableObject).get("Id") == dataItem.get("Id"))
        if (hasItem) {
            displayMessage(getResource("ElementAlreadyAdded"), messageType.warning);
            return null;
        }

        grid.dataSource.data([dataItem]);
    }
}

function onRemoveProofDoc(e: JQuery.EventBase): void {
    e.preventDefault();
    let grid = $("#proofDocsToAdd").data("kendoGrid") as kendo.ui.Grid
    grid.dataSource.data([]);
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

function removeStateAttachment(e: JQuery.EventBase): void {
    onFileUploadRemove(e);
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

    let stateDropDownId = $("#State_Id").data("kendoDropDownList").value();
    let data = {
        State: {
            Id: stateDropDownId,
            Name: $("#State_Id").data("kendoDropDownList").text()
        },
        Date: $("#ChangeDate").val(),
    };

    if (String(stateDropDownId).trim() === getDisputedGUID()) {
        data["Dispute"] = {
            Description: $("#Dispute_Description").val(),
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
                    onFileUploadRemove(e, false);
                    $("#Dispute_Description").val('');
                }
            }
        });
}

function getDisputedGUID() {
    return "c87aa861-856f-4763-a8ab-b9f74faa1302";
}

init();

export function onRecipientCellEdit(e: kendo.ui.GridCellCloseEvent) {
    e.preventDefault();
    let ddl = $(e.container[0]).find("input").data("kendoDropDownList");
    if (ddl && ddl.element.hasClass("rec-address")) {
        let recipientId = e.model.get("Applicant.Recipient.Id");
        let authorId = e.model.get("Applicant.Author.Id");
        ddl.dataSource.read({ recipientId: recipientId, authorId: authorId }).then(function () { ddl.value(e.model.get("Address.Id")) });
    }
}

export function onChannelSelect(e: kendo.ui.DropDownListSelectEvent): void {
    let selected = e.dataItem;
    let uid = e.sender.element.closest("tr").data("uid");
    let grid = $("#recipients").data("kendoGrid") as kendo.ui.Grid;
    let dataItem = grid.dataSource.getByUid(uid);
    dataItem.set("Channel", { Id: selected.Id, Name: selected.Name })
    dataItem.set("IsChecked", true);
    grid.refresh();
}

export function onChannelSelectForAll(e: kendo.ui.DropDownListSelectEvent): void {
    let selected = e.dataItem;
    let grid = $("#recipients").data("kendoGrid") as kendo.ui.Grid;
    grid.dataSource.data().filter(x => x["IsChecked"] == true).map(x => x['Channel'] = { Id: selected.Id, Name: selected.Name })
    grid.refresh();
}

export function onAddressSelect(e: kendo.ui.DropDownListSelectEvent): void {
    let selected = e.dataItem;
    let uid = e.sender.element.closest("tr").data("uid");
    let grid = $("#recipients").data("kendoGrid") as kendo.ui.Grid;
    let dataItem = grid.dataSource.getByUid(uid);
    dataItem.set("Address", { Id: selected.Id, FullDescriptionWithMail: selected.FullDescriptionWithMail, Email: selected.Email })
    grid.refresh();
}

export function onMarkingSuccess(res) {
    if (res.success) {
        let recipientsGrid = kendo.widgetInstance($("#recipients")) as kendo.ui.Grid;
        if (recipientsGrid) {
            recipientsGrid.dataSource.read();
        }
        let historyGrid = kendo.widgetInstance($("#recipient-history")) as kendo.ui.Grid;
        if (historyGrid) {
            historyGrid.dataSource.read();
        }
    }
}

export function onEditDeliverySucccess(res: any): void {
    let window = $(".k-window-content:last").data("kendoWindow") as kendo.ui.Window;
    displayMessage(getResource("Success"), messageType.success);

    if (res.success) {
        if (!window) {
            return;
        }
        window.close();
        let grid = $("#recipient-history").data("kendoGrid");
        grid.dataSource.read();
        grid.refresh();
    } else {
        let keys = Object.keys(res);
        for (var i = 0; i < keys.length; i++) {
            let key = keys[i];
            $(window.element).data(key, res[key]);
        }
    }
}

export function onDeliveryStatusChange(e: kendo.ui.DropDownListChangeEvent): void {
    let element = $(e.sender.element);
    if (element.data("deliveredtoclientid") != e.sender.value()) {
        return;
    }

    let form = element.closest("form");
    if (!form) {
        return;
    }

    let deliveryDateDatePicker = kendo.widgetInstance(form.find("input[name=DeliveryDate]")) as kendo.ui.DateTimePicker;
    if (deliveryDateDatePicker) {
        if (!deliveryDateDatePicker.value()) {
            deliveryDateDatePicker.value(new Date());
        }

        let waitresponsedays = element.data("waitresponsedays") ? Number.parseInt(element.data("waitresponsedays")) : 0;
        if (waitresponsedays > 0) {
            let waitResponseToDateDatePicker = kendo.widgetInstance(form.find("input[name=WaitResponseToDate]")) as kendo.ui.DateTimePicker;
            if (waitResponseToDateDatePicker && !waitResponseToDateDatePicker.value()) {
                waitResponseToDateDatePicker.value(new Date(deliveryDateDatePicker.value().getTime() + waitresponsedays * 24 * 60 * 60 * 1000));
            }
        }
    }
}

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