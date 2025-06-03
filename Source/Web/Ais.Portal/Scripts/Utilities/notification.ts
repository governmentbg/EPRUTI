import { getResource } from "scripts/Utilities/resources";
import { openKendoWindow, rebindEvent } from "scripts/Utilities/core";

function init(): void {
    $(() => {
        handleAjaxMessages();
        bindNotificationCloseHandler();
        bindCopyIdentEvent();
    });

    rebindEvent("click", ".open-notification-js", onNotificationsOpen);
}

function bindCopyIdentEvent(): void {
    $(document).on("click", ".copy-ident-js", function (e) {
        let sender = $(e.currentTarget);
        let identificator = sender.closest("#identificator").text();
        if (identificator) {
            navigator.clipboard.writeText(identificator).then(() => displayMessage(getResource("IdentificatorCopied"), messageType.info));
        }
    });
}

function bindNotificationCloseHandler(): void {
    $(document).off("click", ".k-notification .k-i-close");
    $(document).on("click", ".k-notification .k-i-close", function (e) {
        let sender = $(e.currentTarget);
        sender.closest(".notification-container .k-notification").remove();
    });
}

function handleAjaxMessages(): void {
    $(document)
        .ajaxSuccess(function (event, response) {
            if (checkRedirectUrl(response)) {
                return;
            }

            checkAndHandleMessageFromHeader(response);
        })
        .ajaxError(function (event, response) {
            showError(response);
        });
}

function checkRedirectUrl(response: any): boolean {
    let redirectUrl = response.getResponseHeader("X-Redirect-Url");
    let forceRedirect = response.getResponseHeader("X-Redirect-Force");
    let openInNewWindow = response.getResponseHeader("X-Redirect-Blank");
    if (redirectUrl) {
        if (openInNewWindow == true) {
            window.open(redirectUrl, "_blank");
        }
        else {
            window.location.assign(redirectUrl);
        }

        return forceRedirect === "True";
    }

    return false;
}

function checkAndHandleMessageFromHeader(response: any): boolean {
    let msg = response.getResponseHeader('X-Message');
    if (msg) {
        msg = decodeURIComponent(decodeURIComponent(msg.replace(/\+/g, " ")));
        let messageType = response.getResponseHeader('X-Message-Type');
        displayMessage(msg, messageType);
        return true;
    }

    return false;
}

function showError(response: any): void {
    if (!response) {
        return;
    }

    if (checkRedirectUrl(response)) {
        return;
    }

    if (checkAndHandleMessageFromHeader(response)) {
        return;
    }

    displayMessage(response.responseText ? response.responseText : getResource("InternalServerError"), messageType.error);
}


function onNotificationsOpen(e) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let tr = sender.closest("tr");
    let dataItem, searchQueryId;
    let dataSource: kendo.data.DataSource = null
    if (!tr) {
        let lv = $("#unread-notif").data("kendoListView") as kendo.ui.ListView;
        let uid = $(e.currentTarget).closest("li.k-listview-item").data("uid");
        dataItem = lv.dataSource.getByUid(uid);
        dataSource = lv.dataSource;
    } else {
        let grid = tr.closest(".k-grid").data("kendoGrid") as kendo.ui.Grid;
        searchQueryId = grid.element.data("searchqueryid");
        dataItem = grid.dataSource.getByUid(tr.data("uid"))
        dataSource = grid.dataSource;
    }

    openKendoWindow("Info", "Notifications",
        {
            data: {
                id: dataItem.get("Id"),
                searchQueryId: searchQueryId
            },
        },
        {
            title: dataItem.get("Title"),
            close: () => {
                dataSource.read();
            }
        }

    )
}

init();

export enum messageType {
    info = "info",
    success = "success",
    warning = "warning",
    error = "error"
}

export function displayMessage(message: string, type: messageType, autoHide: boolean = false, autoHideAfter: number = 5000): void {
    if (!message || !type) {
        return;
    }

    let kendoNotification = $("#notification").data("kendoNotification") as kendo.ui.Notification;
    if (kendoNotification) {
        switch (String(type)) {
            case messageType.info:
            case messageType.success:
                {
                    kendoNotification.options.autoHideAfter = autoHideAfter;
                    break;
                }

            default: {
                kendoNotification.options.autoHideAfter = autoHide ? autoHideAfter : 0;
                break;
            }
        }

        kendoNotification.show(
            {
                message: message
            },
            type.toString().toLowerCase());
    }
}