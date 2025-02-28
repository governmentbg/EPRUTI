import { rebindEvent, openKendoWindow, enableInputs, requestOptional, triggerAjaxButtonOnEnter, openKendoWindowUrl } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, getGridBySearchQueryId, getSelectedItemByTr, confirmDelete, getKendoWidget, getDataItem, getKendoWidgeDataSource } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";
function init() {
    rebindEvent("click", ".open-client-info-js", openInfo);
    rebindEvent("click", ".edit-client-js, .add-client-js", upsertClient);
    rebindEvent("click", ".add-name-js", onAddNameClick);
    rebindEvent("click", ".remove-name-js", onRemoveNameClick);
    rebindEvent("click", ".remove-address-js", onRemoveAddressClick);
    rebindEvent("click", ".add-address-js, .edit-address-js", onAddAddressClick);
    rebindEvent("click", ".add-representative-js", addRepresentative);
    rebindEvent("click", ".remove-representative-js", removeRepresentative);
    rebindEvent("click", ".create-representative-js", createRepresentative);
    rebindEvent("change", "#powerOfAttorneyFlag", onPowerOfAttorneyFlagChange);
    rebindEvent("click", ".edit-representative-js", onEditRepresentativeClick);
    rebindEvent("click", ".set-roles-js", onSetRoles);
    rebindEvent("click", ".add-clientrole-js", onAddRoleCick);
    rebindEvent("click", ".add-clientsrole-js", onAddRoleMultiClick);
    rebindEvent("click", ".edit-scope-js", onEditRoleScope);
    rebindEvent("click", ".remove-scope-js", onRemoveRoleScope);
    rebindEvent("click", ".check-username-js", onCheckUserNameClick);
    rebindEvent("click", ".load-from-grao-js, .load-from-register-agency-js", onLoadClick);
    rebindEvent("change", ".denial-es-js", onDenialOfElectronicServicesChange);
    rebindEvent("click", ".add-notice-js, .edit-notice-js", onUpsertCreditNotice);
    rebindEvent("change", ".without-egnbulstat-legal-js, .without-egnbulstat-physical-js", onWithoutEgnBulstatClick);
    rebindEvent("click", ".change-status-js", onChangeStatus);
    rebindEvent("change", "[name='IsDead']", onIsDeadCheckChange);
    rebindEvent("keypress", "#FullName, #EgnBulstat, [name='EgnBulstatSearch']", triggerAjaxButtonOnEnter);
    rebindEvent("keypress", "[name='EgnBulstatSearch']", searchOnEnter);
    rebindEvent("click", ".merge-clients-js", onMergeClientsClick);
    rebindEvent("change", ".check-other-system-js", onCheckOtherSystemChange);
    rebindEvent("change", "input.registerType-js", onRegisterTypeChanged);
}

export function onRegisterDataChange(e: kendo.ui.DropDownListEvent): void {
    let selected = e.sender.value();
    if (selected) {
        $(".register-number").show();
    } else {
        $(".register-number").hide();
    }
}
export function onRegisterDataBound(e: kendo.ui.DropDownListDataBoundEvent): void {
    if (e.sender.dataSource.total() > 0) {
        e.sender.enable(true);
        e.sender.trigger("change");
    }
    else {
        e.sender.enable(false);
    }
}

function onCheckOtherSystemChange(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let form = sender.closest("form");
    var item = sender.hasClass("all-js") ? null : getDataItem(sender);
    requestOptional(
        "ChangeOtherSystemSelectedItems",
        "Clients",
        {
            area: "Admin",
            type: "POST",
            data: {
                key: form.find("[name=UniqueId]").val(),
                type: sender.data("type"),
                id: item ? item.get("UniqueId") : null,
                checked: sender.is(":checked")
            },
            success: () => {
                if (!item) {
                    let dataSource = getKendoWidgeDataSource(sender);
                    if (dataSource) {
                        dataSource.read();
                    }
                }
            }
        });
}

function onMergeClientsClick(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    openKendoWindow(
        "Merge",
        "Clients",
        {
            area: "Admin"
        },
        {
            title: sender.attr("title"),
            modal: true,
        });
}

function searchOnEnter(e: JQuery.KeyPressEvent): void {
    if (e.which == 13) {
        $($(e.currentTarget).data("searchbuttonselector")).trigger("click");
    }
}

function onIsDeadCheckChange(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let isChecked = sender.prop("checked");
    let datePicker = kendo.widgetInstance(sender.closest(".form-row").find("input[name='DateOfDeath']")) as kendo.ui.DatePicker;
    if (datePicker) {
        datePicker.enable(isChecked);
    }
}

function onWithoutEgnBulstatClick(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget);
    let isChecked = sender.prop("checked");
    let wrapper = sender.closest("form");
    sender.closest(".search-row-js").find(".search-input-js").each((index, item) => {
        let div = $(item);
        if (isChecked) {
            div.hide();
            enableInputs(div, false);
        } else {
            div.show();
            enableInputs(div, true);
        }
    });

    let sectionClass = sender.hasClass("without-egnbulstat-physical-js") ? "physical-js" : "legal-js";
    changeClientSectionVisible(wrapper, sectionClass, isChecked);
}

function changeClientSectionVisible(wrapper: JQuery<any>, sectionClass: string, isVisible: boolean): void {
    if (!wrapper || !sectionClass) {
        return;
    }

    wrapper.find(`.clientType-js${sectionClass.startsWith(".") ? sectionClass : `.${sectionClass}`} .form-row:not(.search-row-js)`).each((index, item) => {
        let div = $(item);
        if (isVisible == true) {
            div.show();
            enableInputs(div, true);

            div.find("input[Name*='Name']").each((index, item) => {
                $(item).prop("readonly", false);
            });

            div.find("input[Name='IsLnch'],input[Name='EgnBulstat']").each((index, item) => {
                $(item).prop("disabled", true).closest(".form-input").hide();
            });

            div.find("button").each((index, item) => {
                $(item).prop("disabled", false);
            });

        } else {
            div.hide();
            enableInputs(div, false);
        }
    });
}

function onDenialOfElectronicServicesChange(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget).data("kendoCheckBox");
    let form = sender.element.closest("form");

    let emailLabel = form.find("label[for='Email']");
    let userNameLabel = form.find("label[for='UserName']");

    if (sender.check() == true) {
        form.find("input[name='Email']").attr("disabled", "true");
        form.find("input[name='UserName']").attr("disabled", "true");
        form.find("input[name='SendChangePasswordEmail']").attr("disabled", "true");

        emailLabel.removeClass("required");
        userNameLabel.removeClass("required");
    }
    else {
        form.find("input[name='Email']").removeAttr("disabled");
        form.find("input[name='UserName']").removeAttr("disabled");
        form.find("input[name='SendChangePasswordEmail']").removeAttr("disabled");

        emailLabel.addClass("required");
        userNameLabel.addClass("required");
    }
}

function onLoadClick(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let wrapper = $(`#${sender.data("id")}`).closest("form");
    let number = wrapper.find("input[name='EgnBulstatSearch']:visible").val() as string;
    let clientTypeDropDown = kendo.widgetInstance(wrapper.find("input[name='Type.Id']")) as kendo.ui.DropDownList;
    var clientType = clientTypeDropDown ? getClientTypeById(clientTypeDropDown.value()) : null;
    if (!number || !clientType) {
        displayMessage(kendo.format(getResource("Required"), getResource("EgnBulstat")), messageType.warning);
        return;
    }

    // Set data to continue when has integartionerros
    wrapper.find(".egn-badge:visible").text(number).show();
    wrapper.find(".search-input-js:visible input[name='EgnBulstat']").val(number);
    openKendoWindow(
        "SearchOtherSystem",
        "Clients",
        {
            area: "Admin",
            type: "POST",
            data: {
                number: number,
                type: clientType,
                clientUniqueId: wrapper.find("input[name='UniqueId']").val(),
                GraoFlag: sender.data("grao"),
                RegistryAgencyFlag: sender.data("registryagency")
            },
            error: () => {
                changeClientSectionVisible(wrapper, sender.data("class"), true);
            }
        },
        {
            title: getResource("Load"),
            width: "80%",
            resizable: true,
            modal: true,
            open: (e) => {
                e.sender.wrapper.css({
                    top: 90
                });
            },
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    let result = e.sender.element.data("result");
                    if (result) {
                        let sectionClass = sender.data("class");
                        let selector = `.clientType-js${sectionClass.startsWith(".") ? sectionClass : `.${sectionClass}`}:last`;
                        wrapper.find(selector).replaceWith(result);
                        wrapper.find(selector).show();
                    }

                    let email = e.sender.element.data("email");
                    if (email) {
                        wrapper.find("#Email").val(email);
                    }

                    let egnBulstat = e.sender.element.data("egnBulstat");
                    wrapper.find(".egn-badge:visible").text(egnBulstat);
                    wrapper.find(".search-input-js:visible input[name^='EgnBulstat']").val(egnBulstat);

                    let representativesGrid = kendo.widgetInstance(wrapper.find("#RepresentativesGrid")) as kendo.ui.Grid;
                    if (representativesGrid) {
                        representativesGrid.dataSource.read();
                    }

                    let addressesListView = kendo.widgetInstance(wrapper.find("[name^='AddressesList_']")) as kendo.ui.Grid;
                    if (addressesListView) {
                        addressesListView.dataSource.read();
                    }
                }
            }
        });
}

function onCheckUserNameClick(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let wrapper = sender.closest(".form-row");
    let username = wrapper.find("[name=UserName]").val();
    if (!username) {
        displayMessage(kendo.format(getResource("Required"), getResource("UserName")), messageType.warning);
        return;
    }

    requestOptional(
        "CheckUserName",
        "Clients",
        {
            area: "Admin",
            data: {
                userName: username
            },
            success: (data) => {
                if (data && data.exists) {
                    displayMessage(getResource("ExistMessage"), messageType.warning);
                } else {
                    displayMessage(getResource("NotExistMessage"), messageType.info);
                }
            }
        });
}

function onEditRoleScope(e: JQuery.EventBase) {
    let sender = $(e.currentTarget);
    let item = getSelectedItemByTr(sender.closest("tr"));
    openKendoWindow(
        "EditRoleScopes",
        "Clients",
        {
            area: "Admin",
            data: {
                clientId: $("#Id").val(),
                roleId: item.get("Id")
            },
        },
        {
            title: `${getResource("EditScopes")}`,
            resizable: true,
            scrollable: true,
            modal: true,
        });
}

function onAddRoleCick(e: JQuery.EventBase) {
    e.preventDefault();
    let ddl = $("#rolesDropdown").data("kendoDropDownList");
    let grid = $("#rolesGrid").data("kendoGrid");
    let elementIndex = ddl.select();
    if (elementIndex == 0) {
        displayMessage(getResource("PleaseSelectElement"), messageType.warning)
        return;
    }

    let item = ddl.dataSource.data()[elementIndex - 1];
    if (grid.dataSource.data().some(x => x['Id'] === item["Id"])) {
        displayMessage(getResource("ElementAlreadyAdded"), messageType.warning)
        return;
    }

    requestOptional(
        "AddRoleToClient",
        "Clients",
        {
            area: "Admin",
            type: "POST",
            data: {
                roleId: item.get("Id"),
                clientId: $("#Id").val(),
            },
            success: (res) => {
                grid.dataSource.read();
                grid.refresh();
            }
        })
}

function onRemoveRoleScope(e: JQuery.EventBase) {
    e.preventDefault();

    let grid = $("#rolesGrid").data("kendoGrid");
    let element = getSelectedItemByTr($(e.currentTarget).closest("tr"))

    requestOptional(
        "RemoveRoleFromClient",
        "Clients",
        {
            area: "Admin",
            type: "POST",
            data: {
                roleId: element.get("Id"),
                clientId: $("#Id").val(),
            },
            success: (res) => {
                grid.dataSource.read();
                grid.refresh();
            }
        })
}

function onSetRoles(e: JQuery.EventBase) {
    let sender = $(e.currentTarget)
    let searchQueryId = sender.data("searchqueryid");
    let grid = getGridBySearchQueryId(searchQueryId);
    if (!grid) {
        displayMessage(getResource("NoSelectedItems"), messageType.warning);
        return;
    }

    requestOptional(
        "GetAllSelectedItems",
        "Clients",
        {
            type: "GET",
            area: "Admin",
            data: {
                searchQueryId: searchQueryId
            },
            success: (res) => {
                let selectedObjects = res;
                let action;
                let editMany = selectedObjects.length > 1;
                if (selectedObjects.length == 0) {
                    displayMessage(getResource("NoSelectedItems"), messageType.warning);
                    return;
                } else if (editMany) {
                    action = "ChangeClientsRoles"
                } else {
                    action = "ChangeClientRoles"
                }
                openKendoWindow(
                    action,
                    "Clients",
                    {
                        type: "GET",
                        area: "Admin",
                        data: {
                            searchQueryId: searchQueryId
                        },
                    },
                    {
                        title: `${getResource("ChangeRolesOf")} - ${selectedObjects.map(x => x["FullName"]).join()}`,
                        resizable: true,
                        scrollable: true,
                        modal: true,
                        close: (e) => {
                            if (!editMany) {
                                requestOptional("ClearClientRoleSession", "Clients", { type: "POST", area: "Admin", data: { clientId: $("#Id").val() } })
                            }
                        }
                    }
                )
            }
        })
}

function onEditRepresentativeClick(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);

    let dataItem = getSelectedItemByTr(sender.closest("tr"));
    if (!dataItem) {
        return;
    }

    openEditAgent(sender.data("clientuniqueid"), dataItem["UniqueId"]);
}

function openEditAgent(clientUniqueId, agentUniqueId) {
    openKendoWindow(
        "EditRepresentative",
        "Clients",
        {
            area: "Admin",
            data: {
                clientuniqueid: clientUniqueId,
                representativeUniqueId: agentUniqueId
            }
        },
        {
            title: getResource("EditRepresentativeData"),
            width: "80%",
            resizable: true,
            modal: true,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    let grid = kendo.widgetInstance($("div[name='RepresentativesGrid']:last")) as kendo.ui.Grid;
                    if (grid) {
                        grid.dataSource.read();
                    }
                }
            }
        }
    );
}

function onPowerOfAttorneyFlagChange(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget).data("kendoCheckBox");
    let wrapper = sender.wrapper.closest("form");
    let elements = wrapper.find(".powerofattorney-enddate-js, .powerofattorney-denial-js");
    if (sender.check() != true && sender.wrapper.is(":visible")) {
        elements.show();
        enableInputs(elements, true);
    }
    else {
        elements.hide();
        enableInputs(elements, false);
    }
}

function createRepresentative(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    openKendoWindow(
        "CreateRepresentative",
        "Clients",
        {
            area: "Admin",
            data: {
                clientuniqueid: sender.data("clientuniqueid")
            }
        },
        {
            title: getResource("AddRepresentative"),
            width: "80%",
            resizable: true,
            modal: true,
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
                    let wrapper = sender.closest("form");
                    let grid = wrapper.find("div[name='RepresentativesGrid']").data("kendoGrid");
                    if (grid) {
                        let agent = e.sender.element.data("item");
                        if (!agent) {
                            return;
                        }

                        grid.dataSource.read();
                        openEditAgent(wrapper.find("[name=UniqueId]:first").val(), agent["UniqueId"]);
                    }
                }
            }
        }
    );
}

function removeRepresentative(e: JQuery.EventBase): void {
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
            requestOptional(
                "DeleteRepresentative",
                "Clients",
                {
                    type: "DELETE",
                    area: "Admin",
                    useArea: true,
                    data: {
                        clientUniqueId: grid.element.data("clientid"),
                        representativeUniqueId: selectedItem["UniqueId"]
                    },
                    success: () => {
                        grid.dataSource.read();
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                });
            return true;
        });
}

function addRepresentative(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let wrapper = sender.closest("form");
    let grid = wrapper.find("div[name='RepresentativesGrid']").data("kendoGrid");
    if (!grid) {
        return;
    }

    let selectedItem = getSelectedItemByTr(sender.closest('tr'));
    if (!selectedItem) {
        return;
    }

    requestOptional(
        "AddRepresentative",
        "Clients",
        {
            area: "Admin",
            type: "POST",
            data: {
                clientUniqueId: sender.data("clientuniqueid"),
                representative: selectedItem.toJSON()
            }
            ,
            success: (data) => {
                if (data.success && data.item) {
                    grid.dataSource.read();
                    openEditAgent(wrapper.find("[name=UniqueId]:first").val(), data.item["UniqueId"]);
                }
            }
        });
}

function onAddAddressClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let uniqueId, item;
    let kendoWidget = getKendoWidget($(`#${sender.data("widgetname")}`));
    if (!kendoWidget) {
        return;
    }

    let isEdit = sender.hasClass("edit-address-js");
    if (isEdit) {
        item = getDataItem(sender);
        if (!item) {
            return;
        }

        uniqueId = item.get("UniqueId");
    }

    openKendoWindow(
        "UpsertAddress",
        "Clients",
        {
            area: "Admin",
            data: {
                clientId: kendoWidget.element.data("clientid"),
                uniqueId
            }
        },
        {
            title: isEdit ? `${getResource("EditOf")} ${item.get("FullDescription")}` : getResource("CreateAddress"),
            width: "80%",
            resizable: true,
            modal: true,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    kendoWidget.dataSource.read();
                }
            }
        }
    )
}

function onRegisterTypeChanged(e: JQuery.EventBase): void {
    let element = $(e.currentTarget);
    if (element.is(':radio') && !element.is(":checked")) {
        return;
    }

    let sectionWrapper = element.closest("form");
    sectionWrapper.find("div.registerType-js").each(function () {
        let registerDiv = $(this);
        let registerType = element.val();
        let activeSelectorClass = `${registerType}-js`.toLowerCase();
        if (registerDiv.hasClass(activeSelectorClass)) {
            registerDiv.show();
            registerDiv.find("input").prop('disabled', false);
        } else {
            registerDiv.hide();
            registerDiv.find("input").prop('disabled', true);
        }
    });
}

function onAddNameClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let input = sender.parent().prev();
    let index = kendo.guid();

    let indexInput = input.prev("input[name*='Index']").clone();

    if (indexInput.length == 0) {
        indexInput = $('<input>').attr({
            type: 'hidden',
            name: `${input.attr("name").split("[")[0]}.Index`,
            value: index
        }) as JQuery<HTMLInputElement>;
    }

    indexInput.val(index);

    let newInput = input.clone();
    newInput.val("").attr("id", kendo.guid()).attr("name", `${input.attr("name").split("[")[0]}[${index}]`);

    let newButton = sender.clone();
    let actions = $('<div class="actions"/>').append(newButton.removeClass("add-name-js").addClass("k-button k-small remove-name-js").html("<i class='k-i-trash k-icon k-font-icon'></i>"));
    let actionInput = $('<div class="action-input k-mt-1"/>').append(newInput).append(actions)
    actionInput.insertAfter(sender.closest('.form-input').find(".action-input").last())
}

function onRemoveNameClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let actionInput = sender.closest(".action-input");
    let inputsWrapper = actionInput.prev();

    actionInput.remove()

    let index = actionInput.find("button.names-button").index(sender);
    if (index && index > -1) {
        let input = inputsWrapper.find(".names-input").eq(index);
        input.prev("input[name*='Index']").remove();
        input.remove();
        sender.remove();
    }
}

function upsertClient(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let data = {
        searchQueryId: sender.data("searchqueryid")
    };

    let action, title;
    let isEdit = sender.hasClass("edit-client-js");
    if (isEdit) {
        let grid = getGridBySearchQueryId(sender.data("searchqueryid"));

        if (!grid) {
            return;
        }

        let item = getSelectedItemByGrid(grid);
        if (!item) {
            return;
        }

        data["id"] = item.get("Id");
        title = `${getResource("EditOf")} ${item.get("FullName")}`;
        action = "Edit";
    }
    else {
        title = getResource("AddClient");
        action = "Create";
    }

    openKendoWindow(
        action,
        "Clients",
        {
            area: "Admin",
            data: data
        },
        {
            title: title,
            width: "80%",
            resizable: true,
            modal: true,
            open: (e) => {
                e.sender.wrapper.css({
                    top: 0
                });
            },
        }
    )
}

function openInfo(e: JQuery.EventBase): void {
    let sender = $(e.currentTarget)
    let grid = getGrid(sender);

    if (!grid) {
        return;
    }

    let item = getSelectedItemByTr(sender.closest("tr"));
    if (!item) {
        return;
    }

    let searchQueryId = grid.element.data("searchqueryid");
    let id = item.get("Id");
    let objectTitle = `${item.get("FullName")}, ${item.get("Knik") ? item.get("Knik") : ''}, ${item.get("UniqueIdentifier") ? item.get("UniqueIdentifier") : ''}`;
    openKendoWindow(
        "MenuInfo",
        "Clients",
        {
            area: "Admin",
            data: {
                clientId: id,
                searchQueryId: searchQueryId
            }
        },
        {
            title: `${getResource("ClientInfo")}: ${objectTitle}`,
            resizable: true,
            modal: true,
            width: "90%",
            height: "90%",
            objectTitle: objectTitle,
        }
    )
}

function getClientTypeById(id: string): string {

    switch (id) {
        case "5703695c-ae9e-46a3-988a-c4e8831802d0":
            {
                return "physical";
            }
        case "c7d444cd-8145-4553-9b17-25214e3ad403":
            {
                return "legal";
            }
        case "ed83a08a-05be-4b2d-bf8c-cbd5919255a2":
            {
                return "foreignphysical";
            }
        case "288f3329-0f4f-4885-a288-94a9e65966c7":
            {
                return "foreignlegal";
            }
        case "6e242e07-347c-476f-80d2-688aa0acb61b":
            {
                return "physicalwithbulstat";
            }
    }
}

function onRemoveAddressClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let kendoWidget = getKendoWidget($(`#${sender.data("widgetname")}`));
    if (!kendoWidget) {
        return;
    }

    let selectedItem = getDataItem(sender as JQuery<HTMLElement>);
    if (!selectedItem) {
        return;
    }

    confirmDelete(
        () => {
            requestOptional(
                "DeleteAddress",
                "Clients",
                {
                    type: "DELETE",
                    area: "Admin",
                    useArea: true,
                    data: {
                        clientId: kendoWidget.element.data("clientid"),
                        uniqueId: selectedItem["UniqueId"]
                    },
                    success: () => {
                        kendoWidget.dataSource.read();
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                });
            return true;
        });
}

function onAddRoleMultiClick(e: JQuery.EventBase): void {
    e.preventDefault();
    let ddl = $("#rolesDropdown").data("kendoDropDownList");
    let listview = $("#clientsRoles").data("kendoListView");
    let elementIndex = ddl.select();
    if (elementIndex == 0) {
        displayMessage(getResource("PleaseSelectElement"), messageType.warning)
        return;
    }

    let item = ddl.dataSource.data()[elementIndex - 1];
    if (listview.dataSource.data().some(x => x['Id'] === item["Id"])) {
        displayMessage(getResource("ElementAlreadyAdded"), messageType.warning)
        return;
    }

    requestOptional("AddRoleToClients", "Clients",
        {
            type: "POST",
            area: "Admin",
            data: {
                uniqueId: $("#uniqueId").val(),
                id: item.Id,
                name: item.Name
            },
            success: (res) => {
                if (res.success == true) {
                    listview.dataSource.read();
                    listview.refresh();
                }
            }
        })
}

function onUpsertCreditNotice(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let isEdit = sender.hasClass("edit-notice-js");
    let id, item, clientId;
    let grid = $("#credit-notices").data("kendoGrid");
    if (isEdit) {
        item = getSelectedItemByGrid(grid);
        id = item.get("Id");
    }
    else {
        clientId = sender.data("clientid");
    }

    openKendoWindow(
        "UpsertCreditNotice",
        "Clients",
        {
            area: "Admin",
            data: {
                id: id,
                clientId: clientId
            }
        },
        {
            title: isEdit ? `${getResource("EditOf")} ${item.get("RegNum")}` : getResource("AddCreditNotice"),
            width: "25%",
            resizable: true,
            modal: true,
        })
}

function onChangeStatus(e: JQuery.EventBase) {
    let sender = $(e.currentTarget);
    let searchQueryId = sender.data("searchqueryid");
    let grid = getGridBySearchQueryId(searchQueryId);
    let item = getSelectedItemByGrid(grid);
    if (!item) {
        return;
    }
    openKendoWindow(
        "ChangeStatus",
        "Clients",
        {
            data: {
                id: item.get("Id"),
            },
        },
        {
            modal: true,
            title: `${getResource("ChangeStatus")} - ${item.get("FullName")}`,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    displayMessage(getResource("Success"), messageType.success)
                }
            }
        }
    )
}

init();

export function onCreditNoticeUpsert(res) {
    let window = $(".k-window-content:last").data("kendoWindow") as kendo.ui.Window;
    if (res.success) {
        window.close();
        displayMessage(getResource("Success"), messageType.success);
        $("#search-notices").trigger("click");
        return;
    } else {
        let content = res["result"] ? res["result"] : res;
        if (content) {
            window.content(content);
        }
    }
}

export function onClientTypeDropDownDataBound(e: kendo.ui.DropDownListDataBoundEvent): void {
    e.sender.trigger("change");
    if (!(e.sender.element.data("skipcheckboxchange") === 'True')) {
        e.sender.element.closest("form").find(".without-egnbulstat-legal-js").trigger("change");
        e.sender.element.closest("form").find(".without-egnbulstat-physical-js").trigger("change");
    }
}

export function onClientTypeChanged(e: kendo.ui.DropDownListChangeEvent): void {
    let sender = e.sender;
    let wrapper = sender.element.closest("form");
    let activeSelectorClass = `${getClientTypeById(sender.value())}-js`;
    wrapper.find("div.clientType-js").each((index, item) => {
        let clientTypeDiv = $(item);
        if (clientTypeDiv.hasClass(activeSelectorClass)) {
            clientTypeDiv.show();
            enableInputs(clientTypeDiv, true);
        } else {
            clientTypeDiv.hide();
            enableInputs(clientTypeDiv, false);
        }
    });
}

export const onQualityTypeChanged = (e: kendo.ui.DropDownListChangeEvent) => {
    let wrapper = e.sender.wrapper.closest("form");
    let elements = wrapper.find(".powerofattorney-js");
    if (e.sender.value() == e.sender.element.data("notaryqualityid")) {
        elements.hide();
        enableInputs(elements, false);
    }
    else {
        elements.show();
        enableInputs(elements, true);
    }
    wrapper.find("#powerOfAttorneyFlag").trigger("change");
};

export const onQualityTypeDataBound = (e: kendo.ui.DropDownListDataBoundEvent) => {
    e.sender.trigger("change");
};

export function onClientsRolesCheckboxChange(e: kendo.ui.CheckBoxChangeEvent): void {
    requestOptional("ChangeRoleState", "Clients",
        {
            type: "POST",
            area: "Admin",
            data: {
                roleId: e.sender.element.prop("id"),
                isChecked: e.checked,
                uniqueId: $("#uniqueId").val(),
            },
            success: (res) => {
                $("#clientsRoles").data("kendoListView").dataSource.read();
            }
        });
}

export function onClientsRolesListViewDataBound(e: any): void {
    let dataItems = e.sender.dataItems();
    for (var i = 0; i < dataItems.length; i++) {
        let isChecked = dataItems[i].get("IsChecked");
        if (isChecked == true) {
            $(`#${dataItems[i].get("Id")}`).prop("checked", true);
        } else if (isChecked == false) {
            $(`#${dataItems[i].get("Id")}`).prop("checked", false);
        } else if (isChecked == null) {
            $(`#${dataItems[i].get("Id")}`).prop("indeterminate", true);
            $(`#${dataItems[i].get("Id")}`).val(null);
        }
    }
    if (e.sender.dataItems().length > 0) {
        e.sender.element.parent().show();
    } else {
        e.sender.element.parent().hide();
    }
}

export function onScopesChangeSuccess(data: any): void {
    let grid = $("#rolesGrid").data("kendoGrid");
    grid.dataSource.read();
    grid.refresh();
    var window = $(".k-window-content:last").data("kendoWindow") as kendo.ui.Window;
    if (!window) {
        return;
    }
    window.close();
}

export const onRepresentativesDataBound = (e) => {
    let wrapper = e.sender.wrapper
    wrapper.find(".expired").closest("tr").addClass("bg-red");
};