import { rebindEvent, openKendoWindowUrl, requestOptionalUrl } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGridBySearchQueryId, confirmDelete, removeListViewItem } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".edit-outapplication-type-js, .add-outapplication-type-js", upsertOutApplicationType);
    rebindEvent("click", ".delete-outapplication-type-js", deleteOutApplicationType);
    rebindEvent("click", ".add-outdoc-type-js", addOutDocType);
    rebindEvent("click", ".remove-outdoc-type-js", removeListViewItem);
}

function addOutDocType(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let wrapper = sender.closest("form");
    let outDocTypesDropDown = kendo.widgetInstance(wrapper.find("input.outDocTypesDropDown-js")) as kendo.ui.DropDownList;
    if (!outDocTypesDropDown) {
        return;
    }

    if (!outDocTypesDropDown.dataItem() || outDocTypesDropDown.dataItem().Id == undefined || outDocTypesDropDown.dataItem().Id == null) {
        displayMessage(getResource("PleaseChoose"), messageType.warning);
        return;
    }

    let outDocTypesListView = kendo.widgetInstance(wrapper.find(".outDocTypesListView-js")) as kendo.ui.ListView;
    if (!outDocTypesListView) {
        return;
    }

    if (outDocTypesListView.dataSource.data().some(
        (dataItem) => {
            return dataItem["Id"] == outDocTypesDropDown.dataItem()["Id"]
        })) {
        displayMessage(getResource("AlreadyExists"), messageType.warning);
        return;
    }

    outDocTypesListView.dataSource.add(outDocTypesDropDown.dataItem());
}

function deleteOutApplicationType(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let searchQueryId = sender.data("searchqueryid");

    let grid = getGridBySearchQueryId(searchQueryId);
    if (!grid) {
        return;
    }

    let selectedItem = getSelectedItemByGrid(grid);
    if (!selectedItem) {
        return;
    }

    confirmDelete(
        () => {
            requestOptionalUrl(
                sender.attr("href"),
                {
                    type: "DELETE",
                    data: {
                        searchQueryId: searchQueryId,
                        id: selectedItem["Id"]
                    },
                    success: () => {
                        grid.dataSource.remove(selectedItem);
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                });
            return true;
        });
}

function upsertOutApplicationType(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);

    let id, item;
    let isEdit = sender.hasClass("edit-outapplication-type-js");
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

    openKendoWindowUrl(
        sender.attr("href"),
        {
            data: {
                id: id,
                searchQueryId: sender.data("searchqueryid")
            }
        },
        {
            title: isEdit ? `${getResource("EditOf")} ${item.get("Name")}` : getResource("AddOutApplicationType"),
            width: "80%",
            resizable: true,
            modal: true,
        }
    )
}

init();

