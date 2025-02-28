import { rebindEvent, requestOptional, openKendoWindow, requestOptionalUrl } from "scripts/Utilities/core";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { getResource } from "scripts/Utilities/resources";
const serviceObjectWrapperSelector = ".objects-js";

function init() {
    rebindEvent("click", ".add-outdoc-attachment-js", onAddAttachment);
    rebindEvent("click", ".add-admact-object-js", onAddObjects);
    rebindEvent("click", ".remove-admact-object-js", onRemoveObject);
    rebindEvent("click", ".choose-contact-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let id = sender.val();
        if (!id) {
            let wrapper = sender.closest(".step-box-body");
            let contactsDropDown = wrapper.find("[data-role='dropdownlist']").data("kendoDropDownList");
            id = contactsDropDown.value();
        }

        if (!id) {
            return;
        }

        requestOptional(
            "ChooseAddress",
            "OutApplication",
            {
                type: "POST",
                data: {
                    applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                    id: id,
                    recipientId: sender.data("recipient"),
                    qualityId: sender.data("quality")

                },
                success: (data) => {
                    if (data) {
                        let wrapper = $(".applicants-js");
                        if (data.applicant) {
                            if (data["index"] >= 0) {
                                wrapper.find(`> :eq(${data["index"]})`).replaceWith(data.applicant);
                            }
                            else {
                                wrapper.append(data.applicant);
                            }
                        }
                        else if (data.contact) {
                            wrapper.find(`> :eq(${data["index"]})`).find(".contact-data-js").replaceWith(data.contact);
                        }
                    }
                }
            });
    });
    rebindEvent("click", ".choose-applicant-js, .choose-agent-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let applicationUniqueId = sender.closest("form").find("[name=UniqueId]:first").val();
        let recipientId = sender.data("recipient");
        let authorId = sender.data("author");
        let qualityId = sender.data("quality");
        requestOptional(
            "AddApplicant",
            "OutApplication",
            {
                type: "POST",
                data: {
                    applicationUniqueId: applicationUniqueId,
                    recipientId: recipientId,
                    authorId: authorId,
                    qualityId: qualityId
                },
                success: (data) => {
                    if (data) {
                        if (data.applicant) {
                            let applicantsWrapper = $(".applicants-js");
                            if (data["index"] >= 0) {
                                applicantsWrapper.find(`> :eq(${data["index"]})`).replaceWith(data.applicant);
                            }
                            else {
                                applicantsWrapper.append(data.applicant);
                            }
                        }
                    }
                }
            });
    });
    rebindEvent("click", ".remove-applicant-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        requestOptional(
            "RemoveApplicant",
            "OutApplication",
            {
                type: "POST",
                data: {
                    applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                    uniqueid: sender.data("uniqueid")
                },
                success: (data) => {
                    let wrapper = sender.closest(".applicants-js");
                    sender.closest(".flex").remove();
                    wrapper.find(".flex > .number").each((index, item) => { $(item).html(`${(index + 1)}.`); });
                }
            });
    });
    rebindEvent("click", ".add-applicant-js, .edit-applicant-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let isEdit = sender.hasClass("edit-applicant-js");
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
                        requestOptional(
                            "RefreshApplicant",
                            "OutApplication",
                            {
                                type: "GET",
                                data: {
                                    applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                                    clientId: client["Id"]

                                },
                                success: (data) => {
                                    if (!data) {
                                        return;
                                    }

                                    if (data.applicants) {
                                        $("#applicants").replaceWith(data.applicants);
                                    }

                                    displayMessage(getResource("Success"), messageType.success);
                                }
                            }
                        )

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
    });
    $(window).on('beforeunload', (e) => {
        if (!document.activeElement
            || !document.activeElement["href"]) {
            return;
        }

        let form = $("form#application");
        if (form.length < 1) {
            return;
        }

        let url = form.data("save");
        if (!url) {
            return;
        }

        let data = form.serializeArray();
        setTimeout(() => {
            requestOptionalUrl(
                url,
                {
                    type: "POST",
                    postAsJson: false,
                    data: data
                });
        },
            0);
    });
}

function onAddObjects(e: JQuery.EventBase) {
    debugger;
    e.preventDefault();
    let sender = $(e.currentTarget);
    openKendoWindow(
        "AddObject",
        "OutAdmAct",
        {
            type: "GET",
            data: {
                applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),                
            },
        },
        {
            modal: true,
            title: getResource("ChooseObjects")
        });
}

function onRemoveObject(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let wrapper = sender.closest(".object-wrapper-js");
    let id = wrapper.find(".object-id").val()
    requestOptional(
        "RemoveObject",
        "OutApplication",
        {
            type: "POST",
            data: {
                applicationUniqueId: $("#application").find("[name=UniqueId]:first").val(),
                id: id
            },
            success: () => {
                wrapper.remove();
            }
        });
}

function onAddAttachment(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    requestOptional(
        "AddAttachment",
        "OutApplication",
        {
            type: "POST",
            data: {
                applicationUniqueId: sender.data("unique-id"),
            },
            success: (content) => {
                if (content) {
                    let wrapper = sender.closest(".step-box");
                    let lastAttachemntByType = wrapper.find(".step-box-body:last");
                    if (lastAttachemntByType && lastAttachemntByType.length == 1) {
                        lastAttachemntByType.after(content);
                    }
                    else {
                        sender.closest(".step-box-head").after(content);
                    }
                }
            }
        });
}

function onObjectCheckboxChange(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let wrapper = sender.closest(serviceObjectWrapperSelector);
    if (!wrapper || wrapper.length < 1) {
        return;
    }

    let listView = wrapper.find(".k-listview:first").data("kendoListView") as kendo.ui.ListView;
    if (!listView) {
        return;
    }

    // ask the parameterMap to create the request object for you
    let requestObject = (new kendo.data.transports["aspnetmvc-server"]({ prefix: "" }))
        .options.parameterMap({
            filter: listView.dataSource.filter()
        });

    let selectAll = sender.hasClass("select-all-js");
    let data = {
        key: listView.element.data("key"),
        intersectKey: listView.element.data("intersectkey"),
        addToSelection: sender.is(':checked'),
        filter: requestObject.filter || '~'
    };

    if (!selectAll) {
        let selector = sender.closest(".k-listview-item")[0];
        let object = listView.dataItem(selector).toJSON();
        data["objects"] = [object];
    }

    requestOptional(
        selectAll ? "ChangeSelectedObjectsByType" : "ChangeSelectedObjects",
        "OutApplication",
        {
            type: "POST",
            data: data,
            success: (types: number[]) => {
                listView.dataSource.read();
            }
        });
}

init();

export const signCallback = (sender: JQuery, json: any) => {
    let form = $(`#${sender.attr("form")}`);
    form.find("#signApplication").val(json.signature);
    sender.removeClass("sign-xml-js");
    sender.trigger("click");
};