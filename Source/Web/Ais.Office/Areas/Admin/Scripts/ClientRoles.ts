import { rebindEvent, openKendoWindow, requestOptional } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, getSelectedItemByTr, removeListViewItem, getGridBySearchQueryId, confirmDelete } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".edit-role-js, .add-role-js", upsertRole);
    rebindEvent("change", ".select-all-js, .select-js", onCheckRightsOrServicesCheckbox);
    rebindEvent("change", ".select-all-right-js, .select-right-js", onCheckRightsCheckbox);
    rebindEvent("click", ".add-scope-js", addScope);
    rebindEvent("click", ".remove-item-js", removeListViewItem);
    rebindEvent("click", ".add-tariff-template-js", addTarrifTemplate);
    rebindEvent("click", ".remove-tariff-template-js", removeTariffTemplate);
    rebindEvent("change", ".select-receive-method-js, .select-receive-method-all-js", onCheckReceiveMethod)
}

function removeTariffTemplate(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    if (!grid) {
        return;
    }

    let selectedItem = getSelectedItemByTr(sender.closest('tr'));
    if (!selectedItem) {
        return;
    }

    confirmDelete(
        () => {
            grid.dataSource.remove(selectedItem);
            return true;
        });
}

function addTarrifTemplate(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);

    let roleContractsGird = $("#RoleTariffTemplatesGrid").data("kendoGrid");
    if (!roleContractsGird) {
        return;
    }

    let selectedItem = getSelectedItemByTr(sender.closest('tr'));
    if (!selectedItem) {
        return;
    }

    if (roleContractsGird.dataSource.data().some(
        (dataItem) => {
            return dataItem["Id"] == selectedItem["Id"]
        }))
    {
        displayMessage(getResource("AlreadyExists"), messageType.warning);
        return;
    }

    roleContractsGird.dataSource.data([ selectedItem ])
}

function addScope(e: JQuery.EventBase): void {
    e.preventDefault();

    let provinceDropDown = $("input.provinces-dropdown-js").data("kendoDropDownList");
    if (!provinceDropDown) {
        return;
    }

    let municipalityDropDown = $("input.municipality-dropdown-js").data("kendoDropDownList");
    if (!municipalityDropDown) {
        return;
    }

    let ekatteDropDown = $("input.ekattes-drodown-js").data("kendoDropDownList");
    if (!ekatteDropDown) {
        return;
    }

    let scopesListView = $("#scopesListView").data("kendoListView");
    if (!scopesListView) {
        return;
    }

    let newScope = {
        UniqueId: kendo.guid(),
        Province: null,
        Municipality: null,
        Ekatte: null,
    };

    if (provinceDropDown.dataItem() && provinceDropDown.dataItem().Id != '') {
        newScope.Province = provinceDropDown.dataItem();
    }

    if (municipalityDropDown.dataItem() && municipalityDropDown.dataItem().Id != '') {
        newScope.Municipality = municipalityDropDown.dataItem();
    }

    if (ekatteDropDown.dataItem() && ekatteDropDown.dataItem().Id != '') {
        newScope.Ekatte = ekatteDropDown.dataItem();
    }

    if (newScope.Province == null) {
        displayMessage(getResource("ChooseAtLeastProvince"), messageType.warning);
        return;
    }

    if (scopesListView.dataSource.data().some(
        (dataItem) => {
            return dataItem["Province"] == newScope.Province
                && dataItem["Municipality"] == newScope.Municipality
                && dataItem["Ekatte"] == newScope.Ekatte
        })) {
        displayMessage(getResource("AlreadyExists"), messageType.warning);
        return;
    }

    scopesListView.dataSource.add(newScope);
}

function upsertRole(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);

    let id, item;
    let isEdit = sender.hasClass("edit-role-js");
    if (isEdit) {
        let grid = getGridBySearchQueryId(sender.data("searchqueryid"));

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
        "ClientRoles",
        {
            area: "Admin",
            data: {
                id: id,
                searchQueryId: sender.data("searchqueryid")
            }
        },
        {
            title: isEdit ? `${getResource("EditOf")} ${item.get("Name")}` : getResource("AddRole"),
            width: "80%",
            height: "90%",
            resizable: true,
            modal: true,
        }
    )
}

function onCheckRightsCheckbox(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let checked = sender.prop("checked");
    let isAll = sender.hasClass("select-all-js");
    let tr = sender.closest("tr");
    let dataItem = grid.dataItem(tr as JQuery);
    requestOptional(
        "ChangeRights",
        "ClientRoles",
        {
            area: "Admin",
            type: "POST",
            data: {
                id: dataItem.get("Id"),
                isChecked: checked,
                uniqueId: $("#UniqueId").val(),
                isAll: isAll
            },
            success: (res) => {
                if (res.success) {
                    grid.dataSource.read()
                    grid.refresh()
                }
            }
        })
}

function onCheckRightsOrServicesCheckbox(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let checked = sender.prop("checked");
    let isAll = sender.hasClass("select-all-js");
    let tr = sender.closest("tr");
    let dataItem = grid.dataItem(tr as JQuery);
    requestOptional(
        sender.hasClass("rights") ? "ChangeRights" : "ChangeServices",
        "ClientRoles",
        {
            area: "Admin",
            type: "POST",
            data: {
                ids: isAll ? grid.dataSource.view().map(x => x['Id']) : [dataItem.get("Id")],
                isChecked: checked,
                uniqueId: $("#UniqueId").val(),
            },
            success: (res) => {
                if (res.success) {
                    grid.dataSource.read()
                    grid.refresh()
                }
            }
        })
}

function onCheckReceiveMethod(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }
    let isAll = sender.hasClass("select-receive-method-all-js")
    let checked = sender.prop("checked");
    let receiveMethodId = sender.data("id");
    let serviceId = sender.data("service-id");
    requestOptional(
        "ChangeReceiveMethods",
        "ClientRoles",
        {
            area: "Admin",
            type: "POST",
            data: {
                id: receiveMethodId,
                serviceIds: isAll ? grid.dataSource.view().filter(x => x["IsChecked"]).map(x => x['Id']) : serviceId,
                isChecked: checked,
                uniqueId: $("#UniqueId").val(),
            },
            success: (res) => {
                if (res.success) {
                    grid.dataSource.read()
                    grid.refresh()
                }
            }
        })
}

init();