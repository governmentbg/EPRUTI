import { rebindEvent, requestOptional, createKendoDialog, openKendoWindow, initFormSteps, requestOptionalUrl, enableInputs } from "scripts/Utilities/core";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { getDataItem } from "../../Utilities/searchTable";

const serviceObjectWrapperSelector = ".serviceObjects-js";
const chooseServiceWrapperSelector = ".chooseService-js";
const paymentTypeSelector = "input[type=radio].payment-type-js";

declare let termData: any;
declare let application: any;

function init() {
    bindEvents();
    $(() => {
        initFormSteps($("form#application:last"));
        initPayment();
    });
}

init();


function initPayment() {
    let item = $(`${paymentTypeSelector}:checked`);
    if (item.length < 1) {
        item = $(`${paymentTypeSelector}:first`).attr("checked", "checked");
    }

    item.trigger("change");
}

function onPaymentTypeChange(e: JQuery.EventBase) {
    let sender = $(e.currentTarget);
    let form = sender.closest("form");
    let newPayerId = sender.data("account");
    if (newPayerId) {
        let payerDropDownList = form.find("input[name='Payer.Id']").data("kendoDropDownList") as kendo.ui.DropDownList;
        if (payerDropDownList) {
            payerDropDownList.value(newPayerId);
        };
    }
}

function bindEvents() {
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

    rebindEvent("refresh", "#cart", (e: JQuery.EventBase) => {
        let listView = kendo.widgetInstance($(".choose-service-objects-js:last")) as kendo.ui.ListView;
        if (!listView) {
            return;
        }

        listView.dataSource.read();
        requestOptional(
            "RefreshSelectedObjects",
            "Application",
            {
                type: "POST",
                data: {
                    key: listView.element.data("key"),
                    intersectKey: listView.element.data("intersectkey")
                },
                success: (types: number[]) => {
                    changeChooseServiceStatus(types);
                }
            });
    });

    rebindEvent("change", "input[type=radio].payment-type-js", onPaymentTypeChange);

    rebindEvent("click", "a.steps-box.done", (e: JQuery.EventBase) => {
        e.preventDefault();
        let form = $("form#application:first");
        let nextStep = $("<input>")
            .attr("type", "hidden")
            .attr("name", "next").val($(e.currentTarget).data("step"));
        let direction = $("<input>")
            .attr("type", "hidden")
            .attr("name", "direction").val("Backward");
        form.append(nextStep).append(direction).trigger("submit");
    });

    rebindEvent("click", `${serviceObjectWrapperSelector} .filterChooseObject-js`, (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapper = sender.closest(serviceObjectWrapperSelector);
        if (!wrapper || wrapper.length < 1) {
            return;
        }

        if (sender.data("clear") == true) {
            wrapper.find("[name='objectFilter']:first").val("");
            wrapper.find(".checkboxes [type=checkbox]:not(:checked)").prop("checked", true);
        }

        filterServiceObjects(wrapper);
    });

    rebindEvent("change", `${serviceObjectWrapperSelector} .checkboxes [type="checkbox"]`, (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapper = sender.closest(serviceObjectWrapperSelector);
        if (!wrapper || wrapper.length < 1) {
            return;
        }

        if (!sender.val()) {
            let checked = sender.is(":checked");
            wrapper.find('.checkboxes [type="checkbox"]').prop('checked', checked);
        }
        else {
            wrapper.find('.checkboxes [type="checkbox"][value=""]').prop('checked', wrapper.find('.checkboxes [type="checkbox"][value!=""]:not(:checked)').length == 0);
        }

        filterServiceObjects(wrapper);
    });

    rebindEvent("change", `${serviceObjectWrapperSelector} .results-pre-wrap [type="checkbox"]`, (e: JQuery.EventBase) => {
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
            "Application",
            {
                type: "POST",
                data: data,
                success: (types: number[]) => {
                    if (selectAll) {
                        listView.setDataSource(listView.dataSource);
                    }

                    changeChooseServiceStatus(types);
                }
            });
    });

    rebindEvent("click", `${chooseServiceWrapperSelector} .filterChooseService-js`, (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapper = sender.closest(chooseServiceWrapperSelector);
        if (!wrapper || wrapper.length < 1) {
            return;
        }

        if (sender.data("clear") == true) {
            wrapper.find("[name='serviceFilter']:first").val("");
            wrapper.find(".checkboxes [type=checkbox]:not(:checked)").prop("checked", true);
        }

        filterChooseServices(wrapper);
    });

    rebindEvent("change", `${chooseServiceWrapperSelector} .checkboxes [type="checkbox"]`, (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapper = sender.closest(chooseServiceWrapperSelector);
        if (!wrapper || wrapper.length < 1) {
            return;
        }

        if (!sender.val()) {
            let checked = sender.is(":checked");
            wrapper.find('.checkboxes [type="checkbox"]').prop('checked', checked);
        }
        else {
            wrapper.find('.checkboxes [type="checkbox"][value=""]').prop('checked', wrapper.find('.checkboxes [type="checkbox"][value!=""]:not(:checked)').length == 0);
        }

        filterChooseServices(wrapper);
    });

    rebindEvent("change", `${chooseServiceWrapperSelector} .results-pre-wrap [type="checkbox"]`, (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapper = sender.closest(chooseServiceWrapperSelector);
        if (!wrapper || wrapper.length < 1) {
            return;
        }

        let listView = wrapper.find(".k-listview:first").data("kendoListView") as kendo.ui.ListView;
        if (!listView) {
            return;
        }

        let itemElement = sender.closest(".k-listview-item");
        if (itemElement && itemElement.length > 0) {
            let dataItem = listView.dataItem(sender.closest(".k-listview-item")[0]);
            dataItem["IsChecked"] = sender.is(':checked');
        }
        else {
            let checked = sender.is(':checked');
            let view = listView.dataSource.view(); // To work remove list view paging - page must be -1
            listView.dataSource.data().forEach((item) => {
                item["IsChecked"] = item["Disable"] == true || view.indexOf(item) < 0
                    ? false
                    : checked;
            });
        }

        listView.refresh();
    });

    rebindEvent("click", `${chooseServiceWrapperSelector} .add-service-js`, (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapper = sender.closest(chooseServiceWrapperSelector);
        if (!wrapper || wrapper.length < 1) {
            return;
        }

        addServices(wrapper);
    });

    rebindEvent("click", ".remove-service-js, .remove-all-service-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);

        let wrapper = sender.closest(sender.hasClass("remove-service-js") ? ".service-wrapper-js" : ".steps-body");
        if (!wrapper || wrapper.length < 1) {
            return;
        }

        removeServices(wrapper);
    });

    rebindEvent("change", "input.serviceAttribute-js,textarea.serviceAttribute-js:visible,select.serviceAttribute-js:visible", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        if (sender.hasClass("term-js") && termData) {
            let widget = kendo.widgetInstance(sender);
            if (widget) {
                let termCode: any;
                switch (true) {
                    case widget instanceof kendo.ui.RadioGroup:
                        {
                            let radioGroup = widget as kendo.ui.RadioGroup;
                            termCode = radioGroup.item(null).data("code");
                            break;
                        }

                    case widget instanceof kendo.ui.DropDownList:
                        {
                            let dropDownList = widget as kendo.ui.DropDownList;
                            termCode = dropDownList.dataItem()["Code"];
                            break;
                        }
                }

                if (termCode) {
                    let deliveryMethodTypeElement = sender.closest(".service-wrapper-js").find(".deliverymethodtype-js:last");
                    let deliveryMethodTypeDropDownList = kendo.widgetInstance(deliveryMethodTypeElement) as kendo.ui.DropDownList;
                    if (deliveryMethodTypeDropDownList) {
                        let oldValue = deliveryMethodTypeDropDownList.value();
                        let filter = {
                            logic: "or",
                            filters: []
                        };

                        if (termData[termCode]) {
                            filter.filters = termData[termCode].map((item, index) => {
                                return {
                                    field: "Id",
                                    operator: "eq",
                                    value: item
                                };
                            });
                        }

                        if (filter.filters.length < 1) {
                            filter = null;
                        }

                        let dataSource = deliveryMethodTypeDropDownList.dataSource;
                        if (dataSource.filter() != filter) {
                            dataSource.filter(filter);
                            if (oldValue) {
                                deliveryMethodTypeDropDownList.value(oldValue);
                            }
                            if (!deliveryMethodTypeDropDownList.value()) {
                                deliveryMethodTypeDropDownList.select(0);
                            }
                            deliveryMethodTypeDropDownList.trigger("change");
                        }
                    }
                }
            }
        }

        calculateServicesPrices(sender.closest(".service-wrapper-js"));
    });

    rebindEvent("click", ".remove-attachment-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let groupAttachmentWrapper = sender.closest(".attachment-group-wrapper");
        let target = ".step-box-body";

        if (groupAttachmentWrapper && $(groupAttachmentWrapper).find(".step-box-body").length == 1) {
            return;
        }
        
        let actions = [
            {
                text: getResource("Yes"),
                action: () => {
                    $(e.currentTarget).closest(target).remove();
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
                title: getResource("CancelFileTitle"),
                content: getResource("ConfirmCancelFile"),
                visible: true,
                actions: actions
            });
    });

    rebindEvent("click", ".add-attachment-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        requestOptional(
            "AddAttachment",
            "Application",
            {
                type: "POST",
                data: {
                    applicationUniqueId: sender.data("unique-id"),
                    type: sender.val(),
                    dontShowRelateObject: sender.data("dont-show-relate")
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
    });

    rebindEvent("click", ".attachment-objects-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        openKendoWindow(
            "AttachmentObjects",
            "Application",
            {
                type: "GET",
                data: {
                    applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                    attachment: sender.val(),
                },
            },
            {
                modal: true,
                title: getResource("ChooseObjects")
            });
    });

    rebindEvent("click", ".change-service-objects-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapper = sender.closest(".service-wrapper-js");
        let selector = wrapper.hasClass("service-wrapper-js") ? "*" : ".service-wrapper-js *";
        let data = wrapper.find(selector).serializeArray();
        data.push({
            name: "applicationUniqueId",
            value: wrapper.closest("form").find("[name=UniqueId]:first").val().toString(),
        });

        openKendoWindow(
            "ServiceObjects",
            "Application",
            {
                type: "POST",
                postAsJson: false,
                data: data,
            },
            {
                modal: true,
                title: `${getResource("ChooseObjects")}: ${wrapper.find(".servicename h3").text()}`,
                close: (e) => {
                    if (e.userTriggered) {
                        return;
                    }

                    let success = e.sender.element.data("success");
                    if (success == true) {
                        wrapper.replaceWith(e.sender.element.data("service"));
                        reindexServices();
                    }
                }
            });
    });

    rebindEvent("change", ".relate-objects-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let button = sender.closest(".column").find(".attachment-objects-js");
        if (sender.is(":checked")) {
            button.removeClass("hidden");
        }
        else {
            button.addClass("hidden");
        }
    });

    rebindEvent("change", "#OtherRecipientFlag", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let wrapeper = sender.closest(".other-recipient-wrapper-js");
        let formInputs = wrapeper.find(".other-recipient-js");
        formInputs.toggleClass("hidden");
        enableInputs(formInputs, sender.is(":checked"));
    });

    rebindEvent("click", ".add-other-recipient-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let dataItem = getDataItem(sender);
        if (!dataItem) {
            return;
        }

        let wrapeper = sender.closest(".other-recipient-wrapper-js");
        wrapeper.find("[name=OtherRecipient\\.FullName]:visible").val(dataItem.get("FullName"));
        wrapeper.find("[name=OtherRecipient\\.Egn]:visible").val(dataItem.get("EgnBulstat"));
    });

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
            "Application",
            {
                type: "POST",
                data: {
                    applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                    id: id,
                    recipientId: sender.data("recipient")
                },
                success: (data) => {
                    if (data) {
                        if (data.applicant) {
                            $(".applicants-js").append(data.applicant);
                        }

                        if (data.contact) {
                            $("#contactData").html($(data.contact).html());
                            initFormSteps($("form#application:last"));
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
            "Application",
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

                        if (data.contact) {
                            $("#contactData").html($(data.contact).html());
                            initFormSteps($("form#application:last"));
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
            "Application",
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
                    $("#contactData").html($(data).html());
                    initFormSteps($("form#application:last"));
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
                open: (e) => {
                    e.sender.wrapper.css({
                        top: 0
                    });
                },
                close: (e) => {
                    if (e.userTriggered) {
                        return;
                    }

                    let success = e.sender.element.data("success");
                    if (success === true) {
                        let client = e.sender.element.data("item");
                        requestOptional(
                            "RefreshApplicant",
                            "Application",
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
                                        $("#applicants").html($(data.applicants).html());
                                    }

                                    if (data.contact) {
                                        $("#contactData").html($(data.contact).html());
                                    }

                                    initFormSteps($("form#application:last"));
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

    rebindEvent("click", ".add-application-js", (e: JQuery.EventBase) => {
        requestOptional("AddApplication", "Application",
            {
                type: "POST",
                data: {
                    id: $(e.currentTarget).val(),
                    applicationUniqueId: $(e.currentTarget).closest("form").find("[name=UniqueId]:first").val(),
                },
                success: () => {
                    let listView = $("#applications").data("kendoListView");
                    listView.dataSource.read();
                }
            }
        )
    })

    rebindEvent("click", ".remove-appication-js", (e: JQuery.EventBase) => {
        let sender = $(e.currentTarget);
        requestOptional("RemoveApplication", "Application", {
            type: "DELETE",
            data: {
                id: sender.data("id"),
                applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
            },
            success: () => {
                let listView = $("#applications").data("kendoListView");
                listView.dataSource.read();
            }
        })
    });

    rebindEvent("click", ".add-objects-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let form = sender.closest("form");
        requestOptional(
            "AddSelectedObjects",
            "Application",
            {
                useArea: true,
                type: "POST",
                data: {
                    applicationUniqueId: form.find("[name=UniqueId]:first").val(),
                },
                success: (content) => {
                    if (content) {
                        $(".selected-objects-js").html(content);
                    }
                }
            });
    });

    rebindEvent("click", ".remove-object-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let form = sender.closest("form");
        let wrapper = sender.closest(".object-wrapper-js");
        let id = wrapper.find(".object-id").val()
        requestOptional(
            "RemoveObject",
            "Application",
            {
                useArea: true,
                type: "POST",
                data: {
                    applicationUniqueId: form.find("[name=UniqueId]:first").val(),
                    id: id
                },
                success: () => {
                    wrapper.remove();
                }
            });
    })
}

function addServices(wrapper: JQuery | JQuery<Element>) {
    let listView = wrapper.find(".k-listview:first").data("kendoListView") as kendo.ui.ListView;
    if (!listView) {
        return;
    }

    let selectedServices = listView.dataSource.data().filter((item) => item["Disable"] != true && item["IsChecked"] == true);
    if (!selectedServices || selectedServices.length < 1) {
        displayMessage(getResource("chooseService"), messageType.success);
        return;
    }

    requestOptional(
        "AddServices",
        "Application",
        {
            type: "POST",
            data: {
                applicationUniqueId: wrapper.closest("form").find("[name=UniqueId]:first").val(),
                types: selectedServices.map((item) => item["Id"])
            },
            success: (content) => {
                if (content) {
                    let wrapper = $(".service-wrapper-js:last");
                    if (!wrapper || wrapper.length < 1) {
                        wrapper = $(".add-service-wrapper-js");
                    }

                    wrapper.after(content);
                    calculateApplicationPrice();

                    // Clear selected service after success add
                    selectedServices.forEach((item) => item["IsChecked"] = false);
                    listView.refresh();

                    // Uncheck all checkbox
                    listView.wrapper.closest(".results-pre-wrap").parent().find(".results-pre-wrap > label .k-checkbox:first").prop('checked', false);
                }
            }
        });
}

function removeServices(wrapper: JQuery | JQuery<Element>) {
    let ids = wrapper.find(".service-unique-id-js").map((index, item) => $(item).val()).toArray();
    if (!ids || ids.length < 1) {
        return;
    }

    let actions = [
        {
            text: getResource("Yes"),
            action: () => {
                requestOptional(
                    "RemoveServices",
                    "Application",
                    {
                        type: "POST",
                        data: {
                            applicationUniqueId: wrapper.closest("form").find("[name=UniqueId]:first").val(),
                            ids: ids
                        },
                        success: () => {
                            if (wrapper.hasClass("service-wrapper-js")) {
                                wrapper.remove();
                            }
                            else {
                                wrapper.find(".service-wrapper-js").remove();
                            }

                            calculateApplicationPrice();
                            reindexServices();
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

    createKendoDialog(
        {
            title: getResource("CancelServicesTitle"),
            content: getResource("ConfirmCancelServices"),
            visible: true,
            actions: actions
        });
}

function calculateServicesPrices(wrapper: JQuery | JQuery<Element>) {
    let selector = wrapper.hasClass("service-wrapper-js") ? "*" : ".service-wrapper-js *";
    let data = wrapper.find(selector).serializeArray();
    data.push({
        name: "applicationUniqueId",
        value: wrapper.closest("form").find("[name=UniqueId]:first").val().toString(),
    });
    requestOptional(
        "CalculateServicesPrice",
        "Application",
        {
            type: "POST",
            postAsJson: false,
            data: data,
            success: (prices) => {
                wrapper.find("[data-price]").each((index, item) => {
                    let priceElement = $(item);
                    priceElement.data("price", prices[index]);
                    priceElement.find("strong").text(kendo.format("{0:C2}", prices[index]));
                })

                calculateApplicationPrice();
            }
        });
}

function calculateApplicationPrice() {
    let count = 0;
    let total = 0;
    $(".service-wrapper-js [data-price]").each((index, item) => {
        total += kendo.parseFloat($(item).data("price"));
        count++;
    })

    $("#totalServiceCount").html(`${count} ${getResource("Count")}`);
    $("[data-total-price]:first").data("total-price", total).html(kendo.format("{0:C2}", total));
}

let reindexServices = () => {
    $(".service-wrapper-js .indicator").each((index, item) => {
        $(item).html(`${(index + 1)}`);
    });
};

let filterServiceObjects = (wrapper: JQuery | JQuery<Element>) => {
    if (!wrapper || wrapper.length < 1) {
        return;
    }

    let listView = wrapper.find(".k-listview:first").data("kendoListView") as kendo.ui.ListView;
    if (!listView) {
        return;
    }

    let filter = {
        logic: "and",
        filters: []
    };

    let titleFilter = wrapper.find("[name='objectFilter']:first").val();
    if (titleFilter) {
        filter.filters.push({
            field: "Title",
            operator: "contains",
            value: titleFilter
        });
    }

    let typeFilters = wrapper
        .find(".checkboxes [type=checkbox]:checked:not([value=''])")
        .map((index, item) => {
            return {
                field: "Type",
                operator: "eq",
                value: $(item).val()
            };
        })
        .get();
    if (typeFilters && typeFilters.length > 0) {
        filter.filters.push(
            {
                logic: "or",
                filters: typeFilters
            });
    }
    else {
        filter.filters.push(
            {
                field: "Type",
                operator: "isnull"
            });
    }

    if (filter.filters.length < 1) {
        filter = null;
    }

    if (listView.dataSource.filter() != filter) {
        listView.dataSource.init({
            page: 1,
            filter: filter
        });
        listView.setDataSource(listView.dataSource);
    }
}

let filterChooseServices = (wrapper: JQuery | JQuery<Element>) => {
    if (!wrapper || wrapper.length < 1) {
        return;
    }

    let listView = wrapper.find(".k-listview:first").data("kendoListView") as kendo.ui.ListView;
    if (!listView) {
        return;
    }

    let filter = {
        logic: "and",
        filters: []
    };

    let titleFilter = wrapper.find("[name='serviceFilter']:first").val();
    if (titleFilter) {
        filter.filters.push({
            field: "Title",
            operator: "contains",
            value: titleFilter
        });
    }

    let typeFilters = wrapper
        .find(".checkboxes [type=checkbox]:checked:not([value=''])")
        .map((index, item) => {
            return {
                field: "GroupId",
                operator: "eq",
                value: $(item).val()
            };
        })
        .get();
    if (typeFilters && typeFilters.length > 0) {
        filter.filters.push(
            {
                logic: "or",
                filters: typeFilters
            });
    }
    else {
        filter.filters.push(
            {
                field: "GroupId",
                operator: "isnull"
            });
    }

    if (filter.filters.length < 1) {
        filter = null;
    }

    if (listView.dataSource.filter() != filter) {
        listView.dataSource.filter(filter);
    }
}

let changeChooseServiceStatus = (selectObjectTypes: number[]) => {
    let listView = $(`${chooseServiceWrapperSelector} .k-listview:first`).data("kendoListView") as kendo.ui.ListView;
    if (!listView) {
        return;
    }

    if (!selectObjectTypes) {
        selectObjectTypes = [];
    }

    let data = listView.dataSource.data();
    if (data.length > 0) {
        for (let i = 0; i < data.length; i++) {
            let objectTypes = data[i].ObjectTypes ? data[i].ObjectTypes : [];
            data[i]["Disable"] = !(
                data[i].ObjectsCountType == 0
                || data[i].ObjectsCountType == 3
                || (selectObjectTypes.length < 1 && objectTypes.length < 1)
                || objectTypes.length > 0 && objectTypes.some((item) => selectObjectTypes && selectObjectTypes.indexOf(item) >= 0));
        }

        listView.refresh();
    }
};

export const signCallback = (sender: JQuery, json: any) => {
    let form = $(`#${sender.attr("form")}`);
    form.find("#signApplication").val(json.signature);
    sender.closest(".steps-footer").find(".sign-xml-js").removeClass("sign-xml-js");
    sender.trigger("click");
};

export const onDeliveryMethodChanged = (e: kendo.ui.DropDownListChangeEvent) => {
    let wrapper = e.sender.wrapper.closest(".service-wrapper-js");
    let deliveryDropDownList = wrapper.find("input.delivery-js").data("kendoDropDownList") as kendo.ui.DropDownList;
    if (deliveryDropDownList) {
        let id = e.sender.value();
        let deliveryToPortal = id == e.sender.element.data("id");
        let portalCode = e.sender.element.data("code");
        let find = deliveryDropDownList.dataSource.data().find((d) => deliveryToPortal ? d["Code"] == portalCode : d["Code"] != portalCode);
        if (find) {
            let newVal = find[e.sender.options.dataValueField];
            if (newVal != deliveryDropDownList.value()) {
                deliveryDropDownList.value(newVal);
                deliveryDropDownList.trigger("change");
                calculateServicesPrices(wrapper);
            }
        }
    }
};

export const onDeliveryMethodDataBound = (e: kendo.ui.DropDownListDataBoundEvent) => {
    e.sender.trigger("change");
    e.sender.unbind("dataBound", application.onDeliveryMethodDataBound);
};