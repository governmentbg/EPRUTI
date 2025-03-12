import { rebindEvent, requestOptional, copyToClipboard } from "scripts/Utilities/core";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { confirmDelete } from "scripts/Utilities/searchTable";

function init() {
    rebindEvent("click", ".page-remove-js", onRemovePageClick);
    rebindEvent("click", ".copy-link-js", onCopyLinkPageClick);

    $(() => {
        let pagesTreeList = $("#pages").data("kendoTreeList") as kendo.ui.TreeList;
        if (pagesTreeList) {
            pagesTreeList.setOptions({
                toolbar: kendo.template($("#pageToolbarTemplate").html())
            });
        }

        let pageType = $("#pageType").data("kendoDropDownList") as kendo.ui.DropDownList;
        if (pageType) {
            pageType.bind("change", onPageTypeChange);
            pageType.one("dataBound", () => { pageType.trigger("change"); });
        }

        let locationType = $("#LocationTypesMultiSelect").data("kendoMultiSelect") as kendo.ui.MultiSelect;
        if (locationType) {
            locationType.bind("change", onLocationTypeChange);
            locationType.one("dataBound", () => { locationType.trigger("change"); });
        }
    });
};

function onLocationTypeChange(e: kendo.ui.MultiSelectChangeEvent) {
    let form = $(e.sender.wrapper).closest("form");
    let elements = form.find(".menuTitle-js");

    if (e.sender.value().filter(item => item === e.sender.element.data("location-type-id")).length == 1) {
        e.sender.value(e.sender.element.data("location-type-id"));
        elements.hide();
    } else {
        elements.show();
    }
}

function onPageTypeChange(e: kendo.ui.DropDownListChangeEvent) {
    let form = $(e.sender.wrapper).closest("form");
    let elements = form.find(".content-js");
    let urlName: string;
    if (e.sender.value() == e.sender.element.data("content-type-id")) {
        elements.show();
        urlName = getResource("UrlAddressName");
    } else {
        elements.hide();
        urlName = getResource("Link");
    }

    form.find("label[for='PermanentLink']:first").text(urlName);
    form.find("input[name='PermanentLink']:first").attr("data-val-required", kendo.format(getResource("Required"), urlName));
}

function onCopyLinkPageClick(e) {
    let sender = $(e.currentTarget);
    let tr = sender.closest("tr");
    let treeList = tr.closest(".k-treelist").data("kendoTreeList") as kendo.ui.TreeList;
    let item = treeList.dataItem(tr);
    let copyLink = item["PermanentLink"];
    copyToClipboard(copyLink);
    displayMessage(getResource("CopyLinkMessage"), messageType.info);
}

function onRemovePageClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let treeList = sender.closest(".k-treelist").data("kendoTreeList") as kendo.ui.TreeList;
    let dataItem = treeList.dataItem(sender.closest("tr") as JQuery);
    if (!dataItem) {
        return;
    }

    confirmDelete(
        () => {
            requestOptional(
                "Delete",
                "Cms",
                {
                    type: "DELETE",
                    area: "Admin",
                    useArea: true,
                    data: { id: dataItem["Id"] },
                    success: () => {
                        treeList.dataSource.read();
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                });
            return true;
        });
}

init();

export function onDrop(e) {
    if (e.source["Id"] == e.destination["Id"]) {
        e.setValid(false);
        return;
    }

    requestOptional(
        "ChangePosition",
        "Cms",
        {
            type: "POST",
            useArea: true,
            area: "Admin",
            data: {
                sourceId: e.source["Id"],
                destinationId: e.destination["Id"],
                position: e.position
            },
            success: () => {
                e.setValid(true);
            },
            error: () => {
                e.setValid(false);
            }
        });
}

export function getPageTypeIcon(type) {
    switch (type.toString()) {
        case "2":
        case "Content":
            {
                return "k-i-file-programming";
            }

        case "1":
        case "Link":
            {
                return "k-i-hyperlink";
            }

        default:
            {
                return "";
            }
    }
}

export function getPageVisibilityTypeIcon(type: string) {
    switch (type.toString()) {
        case "1":
        case "Hide":
            {
                return "k-i-minus-circle";
            }

        case "2":
        case "AuthenticatedUsed":
            {
                return "k-i-lock";
            }

        case "3":
        case "Public":
            {
                return "k-i-check-circle";
            }

        default: {
            return "";
        }
    }
}