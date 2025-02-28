import { getResource } from "scripts/Utilities/resources";
const selectorHandle = "#notification";

function init(): void {
    $(() => {
        handleAjaxMessages();
        bindNotificationCloseHandler();
        bindCopyIdentEvent();
    });
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
        sender.closest(".k-animation-container").remove();
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
            window.location.href = redirectUrl;
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

init();

export enum messageType {
    info = "info",
    success = "success",
    warning = "warning",
    error = "error"
}

export const onShowNotification = (e: kendo.ui.NotificationShowEvent) => {
    if (e.sender.getNotifications().length == 1) {
        var element = e.element.closest(".k-animation-container"),
            eWidth = element.width(),
            eHeight = element.height(),
            wWidth = $(window).width(),
            wHeight = $(window).height(),
            newTop, newLeft;

        newLeft = Math.floor(wWidth / 2 - eWidth / 2);
        newTop = Math.floor(wHeight / 2 - eHeight / 2);

        element.css({ top: newTop, left: newLeft });
    }
}


export function displayMessage(message: string, type: messageType, autoHide: boolean = false, autoHideAfter: number = 5000): void {
    if (!message || !type) {
        return;
    }

    let notificationElement = $(selectorHandle).data("kendoNotification") as kendo.ui.Notification;
    if (notificationElement) {
        switch (String(type)) {
            case messageType.info:
            case messageType.success:
                {
                    notificationElement.options.autoHideAfter = autoHideAfter;
                    break;
                }

            default: {
                notificationElement.options.autoHideAfter = autoHide ? autoHideAfter : 0;
                break;
            }
        }

        notificationElement.show(
            {
                title: type,
                message: message
            },
            type.toString().toLowerCase());
    }
}