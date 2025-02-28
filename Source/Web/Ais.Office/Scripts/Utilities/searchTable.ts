import { requestOptional, rebindEvent, openKendoDialog, createKendoDialog } from 'scripts/Utilities/core';
import { displayMessage, messageType } from 'scripts/Utilities/notification';
import { getResource } from 'scripts/Utilities/resources';

function bindEvents(): void {
    rebindEvent(
        "change",
        "input[type=checkbox].checkAll-js, input[type=checkbox].checkItem-js",
        (e) => {
            let sender = $(e.currentTarget);
            let grid = getGrid(sender);
            if (!grid) {
                return;
            }

            let checked = sender.is(":checked");
            let searchQueryId = grid.element.data("searchqueryid");
            let controller = grid.element.data("controller");
            let area = grid.element.data("area");

            let selectOperationType: string;
            let selectedObjects = [];

            let requestObject;
            if (sender.hasClass("checkItem-js")) {
                selectOperationType = checked ? "Add" : "Remove";
                selectedObjects.push(grid.dataItem(sender.closest("tr")).toJSON());
            } else {
                selectOperationType = checked ? "AddAll" : "RemoveAll";
                requestObject = (grid.dataSource as any).transport.parameterMap({
                    filter: grid.dataSource.filter(),
                });
            }

            requestOptional("ChangeSelectedItems",
                controller,
                {
                    type: "POST",
                    area: area,
                    data: {
                        filter: requestObject ? requestObject.filter : null,
                        searchQueryId: searchQueryId,
                        selectOperationType: selectOperationType,
                        selectedObjects: selectedObjects,
                    }, //// aко няма името на пропъртито две точки стойността му - ИЕ се чупи
                    success: function () {
                        grid.dataSource.read().then(() => {
                            grid.refresh();
                        });
                    },
                });
        }
    );
    rebindEvent("click", ".open-label-dialog-js[data-key!='']", openLabelDialog);
}

function openLabelDialog(e: JQuery.EventBase) {
    let sender = $(e.currentTarget);
    let key = sender.data("key");
    openKendoDialog(
        "ReadResourceDescription",
        "Resources",
        {
            useArea: false,
            data: {
                key
            }
        }, {
        title: `${getResource("Description")} - ${getResource(key)}`
    })
}

function convertValues(value): any {
    var data = {};

    value = $.isArray(value) ? value : [value];

    for (var idx = 0; idx < value.length; idx++) {
        data["values[" + idx + "]"] = value[idx];
    }

    return data;
}

function init(): void {
    bindEvents();
}

init();

export function getKendoWidget(element: JQuery<Element>): kendo.ui.Grid | kendo.ui.ListView | kendo.ui.TreeList {
    let widgetElement = element.closest(".k-grid,.k-listview,.k-treelist");
    if (widgetElement.hasClass("k-treelist")) {
        return widgetElement.data("kendoTreeList") as kendo.ui.TreeList;
    }
    else if (widgetElement.hasClass("k-grid")) {
        return widgetElement.data("kendoGrid") as kendo.ui.Grid;
    } else if (widgetElement.hasClass("k-listview")) {
        return widgetElement.data("kendoListView") as kendo.ui.ListView;
    }

    return null;
}


export function getKendoWidgeDataSource(element: JQuery<Element>): kendo.data.DataSource {
    let widget = getKendoWidget(element);
    if (!widget) {
        return null;
    }

    return widget.dataSource;
}

export function getDataItem(element: JQuery<HTMLElement>): kendo.data.ObservableObject {
    let widget = getKendoWidget(element);
    if (!widget) {
        return null;
    }

    let selector: string = null;
    if (widget instanceof kendo.ui.ListView) {
        selector = '[role="listitem"]';
    }
    else if (widget instanceof kendo.ui.Grid) {
        selector = '[role="row"]';
    }
    else if (widget instanceof kendo.ui.TreeList) {
        selector = '[role="row"]';
    }

    if (!selector) {
        return null;
    }

    return widget.dataItem($(element).closest(selector));
}

export function getGridBySearchQueryId(query: string): kendo.ui.Grid {
    return $(`.k-grid[data-searchqueryid=${query}]`).data("kendoGrid");
}

export function getGrid(element: JQuery<Element>): kendo.ui.Grid {
    return getKendoWidget(element) as kendo.ui.Grid;
}

export function getLink(element: string): JQuery {
    let elem = $(element);
    if (elem.is("a")) {
        return elem;
    }

    if (elem.is("button")) {
        return elem;
    }

    return elem.find("a:first");
}

export function getSelectedRow(element: JQuery<Element>): JQuery {
    let grid = getGrid(element);
    if (!grid) {
        return null;
    }

    return grid.select();
}

export function getSelectedItem(element: JQuery<Element>): kendo.data.ObservableObject {
    let grid = getGrid(element);
    if (!grid) {
        return null;
    }

    let selectedRows = grid.select();
    if (selectedRows.length > 1) {
        displayMessage(getResource("SelectOneRowMessage"), messageType.warning);
        return null;
    }

    let selectedItem = grid.dataItem(selectedRows);
    if (!selectedItem) {
        displayMessage(getResource("NotSelectedRowMessage"), messageType.warning);
        return null;
    }

    return selectedItem;
}

export function getSelectedItemByGrid(grid: kendo.ui.Grid): kendo.data.ObservableObject {
    let selectedRows = grid.select();
    if (selectedRows.length > 1) {
        displayMessage(getResource("SelectOneRowMessage"), messageType.warning);
        return null;
    }

    let selectedItem = grid.dataItem(selectedRows);
    if (!selectedItem) {
        displayMessage(getResource("NotSelectedRowMessage"), messageType.warning);
        return null;
    }

    return selectedItem;
}

export function getSelectedItemByTr(element: JQuery<Element>): kendo.data.ObservableObject {
    let grid = getGrid($(element));
    if (!grid) {
        return null;
    }

    let selectedItem = grid.dataItem(element[0]);
    if (!selectedItem) {
        displayMessage(getResource("NotSelectedRowMessage"), messageType.warning);
        return null;
    }
    return selectedItem;
}

export function onDeleteDataSourceItem(dataSource: kendo.data.DataSource, item: kendo.data.ObservableObject): void {
    dataSource.remove(item);

    let currentPage = dataSource.page();
    let totalPages = dataSource.totalPages();
    if (currentPage > totalPages) {
        dataSource.page(totalPages);
    } else {
        dataSource.read();
    }
}

export function goToCorrectGridPage(grid: kendo.ui.Grid): boolean {
    let dataSource = grid.dataSource;
    let currentPage = dataSource.page();
    let totalPages = dataSource.totalPages();
    if (currentPage > totalPages && dataSource.total() > 0) {
        dataSource.page(totalPages);
        return true;
    }

    return false;
}

export function getSelectedRowId(element: JQuery<Element>): string {
    let selectedItem = getSelectedItem(element);
    if (!selectedItem) {
        return;
    }

    return selectedItem.toJSON()["Id"] as string;
}

export function onDataBindingIndex(e: kendo.ui.GridDataBindingEvent): void {
    if (!e.items || e.items.length <= 0) {
        return;
    }

    let isReverse = e.sender.element.hasClass("reverse-js");
    let dataSource = e.sender.dataSource;

    let index = 0;
    if (dataSource.page()) {
        index = (dataSource.page() - 1) * dataSource.pageSize();
    }

    if (isReverse) {
        index += e.items.length + 1;
    }

    for (let i = 0; i < e.items.length; i++) {
        if (!e.items[i].Index) {
            e.items[i].Index = isReverse ? --index : ++index;
        } else {
            isReverse ? --index : ++index;
        }
    }
}

export async function getSelectedItemsCount(grid: kendo.ui.Grid): Promise<number> {
    //// да се извиква с await
    let controller = grid.element.data("controller");
    let count = await requestOptional("GetSelectedItemsCount",
        controller,
        {
            type: "GET",
            data: {
                searchQueryId: grid.element.data("searchqueryid"),
            },
            success: (res) => res as Number
        });

    return count as number;
}

export async function onDeleteMvcTableItem(grid: kendo.ui.Grid, items: kendo.data.ObservableObject) {
    let dataSource = grid.dataSource;

    grid.dataSource.remove(items);

    let currentPage = dataSource.page();
    let totalPages = dataSource.totalPages();
    if (currentPage > totalPages) {
        dataSource.page(totalPages);
    } else {
        dataSource.read();
    }
}

export function removeListViewItem(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let kendoWidget = getKendoWidget(sender);
    let dataItem = getDataItem(sender as JQuery<HTMLElement>);
    if (!dataItem) {
        return;
    }

    let actions = [
        {
            text: getResource("Yes"),
            action: () => {
                kendoWidget.dataSource.remove(dataItem);
                return true;
            },
            primary: true
        },
        {
            text: getResource("No")
        }
    ];

    createKendoDialog(
        {
            title: getResource("DeleteTitle"),
            content: getResource("ConfirmDeleteSelectedItem"),
            visible: true,
            actions: actions
        });
}

export function confirmDelete(action: Function) {
    let actions = [
        {
            text: getResource("Yes"),
            action: action,
            primary: true
        },
        {
            text: getResource("No")
        }
    ];

    // TODO: 57237
    createKendoDialog(
        {
            title: getResource("Deleting"),
            content: getResource("ConfirmDeleting"),
            visible: true,
            actions: actions
        });
}

export function onHiddenGridDataBound(e: kendo.ui.GridDataBoundEvent): void {
    e.sender.element.parent().toggleClass("hidden", !(e.sender.dataItems().length > 0));
}

export function valueMapper(options) {
    let currController = window.location.href.split("/").pop();
    let indexOfQueryParams = currController.indexOf("?");
    if (indexOfQueryParams > 0) {
        currController = currController.substr(0, indexOfQueryParams);
    }
    let data = convertValues(options.value);
    requestOptional("ValueMapper", currController,
        {
            data: data,
            success: function (data) {
                options.success(data);
            }
        })
}

export function virtualDdlPrepData(e) {
    if (e && e.filter && e.filter.filters && e.filter.filters[0] && e.filter.filters[0].value) {
        return {
            name: e.filter.filters[0].value
        }
    }
}

export function getDropDownSelectedItem(dropDownSelector: string, warningMessageKey: string) {
    let dropDown = $(dropDownSelector).data("kendoDropDownList");
    if (!dropDown) {
        return;
    }

    let selectedItem = dropDown.dataItem();
    if (!selectedItem || !selectedItem.Id) {
        displayMessage(getResource(warningMessageKey), messageType.warning)
        return;
    }

    return selectedItem;
}

export function addItemToGrid(item: any, gridSelector: string, skipCheck: boolean = false): void {

    let grid = $(gridSelector).data("kendoGrid");
    if (!grid) {
        return;
    }

    if (!skipCheck && grid.dataSource.data().some(
        (dataItem) => {
            return dataItem["Id"] == item.Id
        })) {
        displayMessage(getResource("AlreadyExists"), messageType.warning);
        return;
    }

    grid.dataSource.add(item);
}

export function removeItemFromGrid(e: JQuery.EventBase): void {
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

export function onGridDataBound(e: kendo.ui.GridDataBoundEvent): void {
    let totalPages = e.sender.dataSource.totalPages();
    if (totalPages > 0 && totalPages < e.sender.dataSource.page()) {
        e.sender.dataSource.page(totalPages > 1 ? totalPages - 1 : 1);
        return;
    }

    if (e.sender.element.data("hide-empty")) {
        e.sender.element.parent().toggleClass("hidden", !(e.sender.dataItems().length > 0));
    }

    if (e.sender.element.data("show-first-details")) {
        e.sender.expandRow(e.sender.tbody.find("tr.k-master-row").first());
    }

    if (e.sender.element.hasClass('withoutpager')) {
        if (totalPages < 2) {
            e.sender.pager.element.hide();
        }
        else {
            e.sender.pager.element.show();
        }
    }
}
