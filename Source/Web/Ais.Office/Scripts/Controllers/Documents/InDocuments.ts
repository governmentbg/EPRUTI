import { rebindEvent, openKendoWindow, openKendoWindowUrl, requestOptional, openKendoDialog, createKendoDialog } from "scripts/Utilities/core";
import { getGrid, getSelectedItemByGrid, getDataItem, confirmDelete } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("change", "[type=radio].payment-type-js", onPaymentTypeChange);
    rebindEvent("click", ".pay-doc-js", payDocument);
    rebindEvent("click", ".regoutdoc-js", regOutDocument);
    rebindEvent("click", ".add-exist-client-js", onAddExistClick);
    rebindEvent("change", ".recipients-js .k-checkbox", onRecipientCheckboxChange);
    rebindEvent("click", ".create-client-js", onUpsertClientClick);
}

function onAddExistClick(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    addRecipient(getDataItem(sender), sender.closest("form"));
}

function addRecipient(client, form) {
    let grid = kendo.widgetInstance(form.find(".recipients-js")) as kendo.ui.Grid;
    if (grid && client && client["Id"]) {
        let exist = grid.dataSource.data().find((item) => item["Id"] == client["Id"]);
        if (exist) {
            return;
        }

        grid.dataSource.add(client);
    }
}

function onRecipientCheckboxChange(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let client = getDataItem(sender);
    client.set("Checked", sender.is(':checked'));
}

function onUpsertClientClick(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let isEdit = sender.hasClass("edit-client-js");
    openKendoWindow(
        isEdit ? "Edit" : "Create",
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
            title: isEdit ? `${getResource("EditOf")} ${sender.data("name")}` : getResource("AddClient"),
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    let client = e.sender.element.data("item");
                    addRecipient(client, sender.closest("form"));

                    let searchWrapepr = sender.closest(".search-client-wrapper-js");
                    if (searchWrapepr && searchWrapepr.length > 0) {
                        setTimeout(
                            () => {
                                let searchClientForm = searchWrapepr.find("#searchClientForm");
                                searchClientForm.find("input[name!=Limit]").val("");
                                searchClientForm.find("[name=Knik]").val(client["Knik"]);
                                searchClientForm.find("button[type=submit]").trigger("click");
                            },
                            500);
                    }
                }
            }
        });
}

function regOutDocument(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    let dataItem = grid ? getSelectedItemByGrid(grid) : null;
    if (!dataItem) {
        return;
    }

    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                id: dataItem.get("Id"),
            },
            useArea: false
        },
        {
            title: sender.attr("title"),
            resizable: true,
            modal: true,
            width: "90%"
        }
    )
}

function payDocument(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);
    if (!grid) {
        return;
    }

    let item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }

    if (item.get("Amount") == 0) {
        displayMessage(getResource("DocumentPriceIs0"), messageType.warning);
        return;
    }

    if (item.get("Amount") == item.get("PaidAmount")) {
        displayMessage(getResource("DocumentAlreadyPaid"), messageType.warning);
        return;
    }

    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                id: item.get("Id"),
            },
            useArea: false
        },
        {
            title: `${getResource("PayDocWithRegnum")} ${item.get("RegNum")}`,
            resizable: true,
            modal: true,
            width: "90%"
        }
    )
}

function onPaymentTypeChange(e: JQuery.EventBase) {
    let sender = $(e.currentTarget);
    let form = sender.closest("form");
    form.find("[name=BillingAccountId]").val(sender.data("account"));
}

init();
