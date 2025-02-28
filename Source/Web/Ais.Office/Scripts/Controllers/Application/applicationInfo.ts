import { rebindEvent, openKendoWindow, initFormSteps, openKendoWindowUrl, createKendoDialog, requestOptional } from "scripts/Utilities/core";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    bindEvents();
    $(() => {
        initFormSteps($("div.docinfo:last"));
    });
}

function bindEvents() {
    rebindEvent("click", ".view-objects-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        openKendoWindow(
            "ViewObjects",
            sender.data("controllername") ? sender.data("controllername") : "Application",
            {
                type: "GET",
                data: {
                    applicationUniqueId: sender.data("app-id"),
                    type: sender.data("type"),
                    id: sender.data("id"),
                },
            },
            {
                modal: true,
                title: `${getResource("Objects")}: ${sender.data("title")}`,
                width: 500,
            });
    });
    rebindEvent("click", ".view-all-attachments-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        openKendoWindow(
            "ViewAttachments",
            sender.data("controllername") ? sender.data("controllername") : "Application",
            {
                type: "GET",
                data: {
                    applicationUniqueId: sender.data("app-id"),
                },
            },
            {
                modal: true,
                title: getResource("Documents"),
                width: 600, height: '90%'
            });
    });
    rebindEvent("click", ".info-change-service-objects-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        openKendoWindowUrl(
            sender.attr("href"),
            null,
            {
                title: sender.attr("title"),
                modal: true,
                close: (e) => {
                    if (e.userTriggered) {
                        return;
                    }

                    let success = e.sender.element.data("success");
                    if (success === true) {
                        let tabStrip = sender.closest(".k-tabstrip").data("kendoTabStrip") as kendo.ui.TabStrip;
                        if (tabStrip) {
                            tabStrip.reload("li.k-active");
                        }
                    }
                }
            });
    });
    rebindEvent("click", ".add-tasks-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapper = sender.closest(".service-wrapper-js");

        openKendoWindow(
            "AddTasksToService",
            "InDocuments",
            {
                data: {
                    serviceId: sender.data("serviceid")
                }
            },
            {
                title: getResource("AddingTasks"),
                resizable: true,
                modal: true,
                width: "50%",
                close: (e) => {
                    if (e.userTriggered) {
                        return;
                    }

                    let success = e.sender.element.data("success");
                    if (success === true) {
                        let tasksListView = wrapper.find(".tasks .k-listview").data("kendoListView");
                        if (tasksListView) {
                            tasksListView.dataSource.data(e.sender.element.data("tasks"));
                        }
                    }
                }
            });
    });
    rebindEvent("click", ".upsert-additional-attachments-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        openKendoWindowUrl(
            sender.attr("href"),
            null,
            {
                title: sender.attr("title"),
                modal: true,
                close: (e) => {
                    if (e.userTriggered) {
                        return;
                    }

                    let success = e.sender.element.data("success");
                    if (success === true) {
                        let tabStrip = sender.closest(".k-tabstrip").data("kendoTabStrip") as kendo.ui.TabStrip;
                        if (tabStrip) {
                            tabStrip.reload("li.k-active");
                        }
                    }
                }
            });
    });
    rebindEvent("click", ".cancel-service-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);

        openKendoWindow(
            "CancelService",
            "InDocuments",
            {
                data: {
                    serviceId: sender.data("serviceid")
                }
            },
            {
                title: getResource("CancelService"),
                resizable: true,
                modal: true,
                width: "50%",
                close: (e) => {
                    if (e.userTriggered) {
                        return;
                    }
                }
            });
    });
    rebindEvent(
        "click",
        ".copy-regNumber-js",
        function (e) {
            let sender = $(e.currentTarget);
            let regnumber = sender.closest(".regnumber").text().trim();
            if (regnumber) {
                navigator.clipboard.writeText(regnumber).then(() => displayMessage(getResource("RegNumberCopied"), messageType.info));
            }
        }
    );

    rebindEvent("click", ".get-force-service-dialog-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);

        let actions = [
            {
                text: getResource("Yes"),
                action: () => {
                    requestOptional(
                        "ForceService",
                        "InDocuments",
                        {
                            type: "POST",
                            data: {
                                serviceId: sender.data("serviceid"),
                                inDocumentId: $("#InDocumentId").val()
                            },
                            success: (data) => {
                            }
                        });
                },
                primary: true
            },
            {
                text: getResource("No")
            }
        ];

        createKendoDialog(
            {
                title: getResource("ForceService"),
                content: getResource("ConfirmForceService"),
                visible: true,
                actions: actions
            });
    });

    rebindEvent("click", ".canceledbyclient-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);

        let actions = [
            {
                text: getResource("Yes"),
                action: () => {
                    requestOptional("CanceledByClient", "InDocuments", {
                        type: "POST",
                        async: true,
                        data:
                        {
                            id: sender.data("id"),
                        }
                    });
                },
                primary: true
            },
            {
                text: getResource("No")
            }
        ];

        createKendoDialog(
            {
                title: getResource("ChangeStatus"),
                content: `${getResource("StatusChangeCanceledByClient")} ${sender.data("regnum")}`,
                visible: true,
                actions: actions
            });
    });
}

init();