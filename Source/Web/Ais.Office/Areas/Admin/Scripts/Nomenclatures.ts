import { rebindEvent, openKendoWindow, languages } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".edit-nomenclature-js", editNomenclature);
    rebindEvent("click", ".edit-value-js", editNomenclatureValue);
    rebindEvent("click", ".add-value-js", addNomenclatureValue);
    rebindEvent("click", ".visibility-value-js", visibilityNomenclatureValue);
    rebindEvent("click", ".upsert-current-value-js", upsertValue);
    rebindEvent("click", ".remove-current-value-js", removeValue);
}

function editNomenclature(e: JQuery.EventBase): void {
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

    let searchQueryId = grid.element.data("searchqueryid");
    let id = item.get("Id")
    let name = item.get("Name")
    openKendoWindow(
        "Edit",
        "Nomenclatures",
        {
            area: "Admin",
            data: {
                id: id,
                searchQueryId: searchQueryId
            }
        },
        {
            title: `${getResource("EditOf")} ${getResource(name)}`,
            resizable: true,
            modal: true,
        }
    )
}

function editNomenclatureValue(e: JQuery.EventBase): void {
    e.preventDefault();
    $("#values-editor").removeClass("hidden");
    let sender = $(e.currentTarget);
    let id: string = sender.closest(".k-listview-item").data("uid");
    let listView = $("#values-listview").data("kendoListView") as kendo.ui.ListView;
    let item = listView.dataSource.getByUid(id);

    for (let key in languages) {
        let langId = languages[key];
        let name = item["Name"];
        $(`input[name='editor[${langId}]']`).val(name[langId]);
    }
    $("#value-id").val(item["uid"]);
}

function addNomenclatureValue(e: JQuery.EventBase): void {
    e.preventDefault();
    $("#values-editor").removeClass("hidden");
    clearInputs();
}

function visibilityNomenclatureValue(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let id: string = sender.closest(".k-listview-item").data("uid");
    let listView = $("#values-listview").data("kendoListView") as kendo.ui.ListView;
    let item = listView.dataSource.getByUid(id);
    item.set("IsVisible", !item["IsVisible"])
    listView.refresh();
    displayMessage(getResource("Success"), messageType.success, true, 2000);
}

function clearInputs(): void {
    let inputs = $("#values-editor input");
    for (let i = 0; i < inputs.length; i++) {
        $(inputs).val("");
    }
}

function upsertValue(e: JQuery.EventBase): void {
    e.preventDefault();
    let listView = $("#values-listview").data("kendoListView") as kendo.ui.ListView;
    let id: string = $("#value-id").val() as string;
    let dataItem = id ? listView.dataSource.getByUid(id) : null;
    let nameObject = dataItem ? dataItem["Name"] : {};
    let isValid = setProperties(nameObject);
    if (!isValid) {
        displayMessage(kendo.format(getResource("Required"), getResource("Value")), messageType.warning);
        return;
    }

    if (dataItem) {
        dataItem.set("Name", nameObject);
    } else {
        listView.dataSource.add({ "Name": nameObject, "Id": null, IsVisible: true });
    }

    clearInputs();
    $("#values-editor").addClass("hidden");
    listView.refresh();
    displayMessage(getResource("Success"), messageType.success, true, 2000);

    function setProperties(nameObject: any) {
        let valid = true;
        for (var key in languages) {
            let langId = languages[key];
            nameObject[langId] = $(`input[name='editor[${langId}]']`).val();
            if (valid && !nameObject[langId]) {
                valid = false;
            }
        }

        return valid;
    }
}

function removeValue(e: JQuery.EventBase): void {
    e.preventDefault();
    clearInputs();
    $("#values-editor").addClass("hidden");
}

init();