import { rebindEvent, openKendoWindow, requestOptional, getPathToActionMethod, createKendoDialog } from "scripts/Utilities/core";
import { confirmDelete, getGrid, getSelectedItemByGrid, getSelectedItemByTr } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("click", ".delete-faq-js", onDeleteFaq);
    rebindEvent("click", ".edit-faq-js", onUpsertFaq);
    rebindEvent("click", ".manageFAQCategory-js", manageFaqCategory);
    rebindEvent("click", ".add-category-js, .edit-category-js", upsertCategory);
    rebindEvent("click", ".remove-category-js", onRemoveCategory);
}

function onUpsertFaq(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    let item = getSelectedItemByGrid(grid);
    window.open(`${getPathToActionMethod("Upsert", "Faq", { area: "Admin" })}?id=${item.get("Id")}`, "_self")
}

function onDeleteFaq(e: JQuery.EventBase): void {
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
                "Faq",
                {
                    type: "DELETE",
                    area: "Admin",
                    data: {
                        id: item.get("Id"),
                        searchQueryId: searchQueryId
                    },
                    success: () => {
                        grid.dataSource.read();
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                });
            return true;
        });
}

function manageFaqCategory(e: JQuery.EventBase): void {
    e.preventDefault();
    openKendoWindow(
        "ManageCategory",
        "Faq",
        {
            area: "Admin",
            type: "GET",
        },
        {
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }
            },
            title: getResource("ManageCategory")
        });
} 

function upsertCategory(e: JQuery.EventBase): void {
    e.preventDefault();

    let tr = $(e.currentTarget).closest("tr");
    let dataIem = null;
    if (tr) {
        dataIem = getSelectedItemByTr(tr);
    }

    openKendoWindow(
        "UpsertCategory",
        "Faq",
        {
            type: "GET",
            area: "Admin",
            data: {
                id: dataIem != null ? dataIem["Id"] : ''
            }
        },
        {
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }
            },
            title: dataIem != null ? `${getResource("EditOf")} ${dataIem["Name"]}` : getResource("CreateCategory")
        });
}

function onRemoveCategory(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let faqCategoryId = sender.data("id");

    const actions = [
        {
            text: getResource("Yes"),
            action: () => {
                requestOptional(
                    "DeleteCategory",
                    "Faq",
                    {
                        type: "POST",
                        area: "Admin",
                        data: {
                            id: faqCategoryId,
                            newCategoryId: $("#newCategory").val()
                        },
                        success: () => {
                            displayMessage(getResource("DeleteSuccess"), messageType.success);
                            let categoriesDropDownList = $("#Category_Id").data("kendoDropDownList") as kendo.ui.DropDownList;
                            categoriesDropDownList.dataSource.read();
                            $("#CategoriesGrid").data("kendoGrid").dataSource.read();
                        }
                    });
                return true;
            },
            primary: true
        },
        {
            text: getResource("No")
        }
    ];

    let categoryName: string = getSelectedItemByTr(sender.closest("tr"))["Name"]
    let dialogContent: string = getResource("ConfirmDeletingCategory").replace("{0}", categoryName) +
        "<div class='form-input fullwidth'>" +
        `<label>${getResource("TransferCategory")}</label>` +
        "<input id='newCategory'/>" +
        "</div>";

    createKendoDialog(
        {
            title: getResource("Deleting"),
            content: dialogContent,
            visible: true,
            actions: actions
        });

    var url = `${getPathToActionMethod("GetFaqFilteredCategories", "Faq", { area: "Admin" })}?categoryId=${faqCategoryId}`;

    $("#newCategory").kendoDropDownList({
        dataTextField: "Value",
        dataValueField: "Key",
        optionLabel: getResource("CategoryAndQuestionsDelete"),
        dataSource: {
            type: "json",
            serverFiltering: true,
            transport: {
                read: url,
            }
        }
    }).data("kendoDropDownList");
}

init();

export function onFaqCategoryUpsertSuccessfully(e)  {
    let categoriesDropDownList = $("#Category_Id").data("kendoDropDownList") as kendo.ui.DropDownList;

    categoriesDropDownList.dataSource.read();
    $("#CategoriesGrid").data("kendoGrid").dataSource.read();

    let window = $(".k-window-content:last").data("kendoWindow");
    if (!window) {
        return;
    }
    window.close();
}

export function onChangeOrder(e: kendo.ui.SortableChangeEvent): void {
    let oldIndex = e.oldIndex;
    let newIndex = e.newIndex;

    let grid = kendo.widgetInstance(e.sender.element) as kendo.ui.Grid;
    requestOptional(
        "Rearrange",
        "Faq",
        {
            type: "POST",
            area: "Admin",
            data: {
                oldIndex: oldIndex,
                newIndex: newIndex,
                searchQueryId: $(grid.element).data("searchqueryid")
            }
        });
}