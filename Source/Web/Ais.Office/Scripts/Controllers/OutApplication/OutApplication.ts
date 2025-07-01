import { MessageType } from "@microsoft/signalr";
import { rebindEvent, requestOptional, createKendoDialog, openKendoWindow, requestOptionalUrl, onFileUploadRemove, ShowConfirmDialogBeforeAction } from "scripts/Utilities/core";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { getResource } from "scripts/Utilities/resources";
import { getGrid, getSelectedItemByGrid } from "scripts/Utilities/searchTable";

const serviceObjectWrapperSelector = ".objects-js";

interface Selector {
    keyusages: string[];
    issuers: string[];
}

interface Request {
    selector: Selector;
    showValidCerts: string;
}

const request: Request = {
    selector: {
        keyusages: [] as string[],
        issuers: [] as string[]
    },
    showValidCerts: "true"
};

let bissport: number = 53952;
let bissGetUrl: string = `https://localhost:${bissport}/getsigner`;
let bissSignUrl = `https://localhost:${bissport}/sign`;


function init() {


    rebindEvent("click", ".add-outdoc-attachment-js", onAddAttachment);
    rebindEvent("click", ".add-objects-js", onAddObjects);
    rebindEvent("change", `${serviceObjectWrapperSelector} .results-pre-wrap [type="checkbox"]`, onObjectCheckboxChange);
    rebindEvent("click", ".remove-object-js", ShowConfirmDialogBeforeAction(onRemoveObject, getResource("RemoveObjectTitle"), getResource("ConfirmRemoveObject")));
    rebindEvent("click", ".add-admact-object-js", onAddAdmActObject);
    rebindEvent("click", ".add-admact-object-address-js", onAddAddressToAdmActObject);
    rebindEvent("click", ".arrow-icon", onArrowClick);
    rebindEvent("click", ".search-admact-js", onSearchAdmAct);
    rebindEvent("click", ".choose-admact-js", onChooseConnectedAdmAct);
    rebindEvent("click", ".remove-admact-js", ShowConfirmDialogBeforeAction(onRemoveConenctedAdmAct, getResource("RemoveAdmActTitle"), getResource("ConfirmRemoveAdmAct")));
    rebindEvent("click", ".save-connected-acts-js", onSaveConnectedAdmActs);
    rebindEvent("click", ".attach-admact-js", onAddAddressToAdmActObject);
    rebindEvent("click", ".edit-admact-state-js", onEditAdmActState);
    rebindEvent("click", ".delete-admact-history-row-js", ShowConfirmDialogBeforeAction(OnDeleteAdmActState, getResource("RemoveAdmActStateRowTitle"), getResource("ConfirmRemoveAdmActStateRow")));
    rebindEvent("click", ".remove-attachment-state-js", ShowConfirmDialogBeforeAction(onFileUploadRemove, getResource("RemoveFileTitle"), getResource("ConfirmRemoveFile")));
    rebindEvent("click", ".add-admact-state", addAdmActStateRow);
    rebindEvent("click", ".download-file", downloadFile);
    rebindEvent("click", ".attachments-wrap .attachment-group-name", onAttachmentGroupNameClick);
    rebindEvent("click", ".change-adm-act-actuality", onDocActualityChange);
    rebindEvent("click", ".info-admact-js", onOpenAmdActInfo);
    rebindEvent("click", ".indentificators-info-js", openIdentificatorsInfo)
    rebindEvent("click", ".remove-allobjects-js", ShowConfirmDialogBeforeAction(onRemoveAllObjects, getResource("RemoveAllObjectTitle"), getResource("ConfirmRemoveAllObject")));
    rebindEvent("click", ".add-objects-js", onAddIndetificators)
    rebindEvent("click", ".remove-applicant-js", ShowConfirmDialogBeforeAction(onRemoveApplicant, getResource("RemoveApplicantTitle"), getResource("ConfirmRemoveApplicant")));
    rebindEvent("click", ".remove-actObject-js", ShowConfirmDialogBeforeAction(onRemoveActObject, getResource("RemoveActObjectTitle"), getResource("ConfirmRemoveActObject")));
    rebindEvent("click", ".remove-objectAddress-js", ShowConfirmDialogBeforeAction(onRemoveObjectAddress, getResource("RemoveObjectAddressTitle"), getResource("ConfirmRemoveObjectAddress")));
    rebindEvent("change", "#RegisterTypeId", onAARegisterTypeChange);
    rebindEvent("change", "#AdministrationId", onAdminiStrationChangeChange);
    rebindEvent("click", ".publish-admact-js", ShowConfirmDialogBeforeAction(onPublishAdmAct, getResource("ConfirmPublishTitle"), getResource("ConfirmPublish")));
    rebindEvent("click", ".save-admact-js", onSaveAdmAct);
    rebindEvent("click", ".delete-admact-js", ShowConfirmDialogBeforeAction(onDeleteAdmAct, getResource("DeleteAdmActTitle"), getResource("ConfirmDeleteAdmAct")));
    rebindEvent("click", ".open-model-version-js", openModelVersion);
    rebindEvent("click", ".choose-contact-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let id = sender.val();
        if (!id) {
            let wrapper = sender.closest(".contact-data-js");
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
    rebindEvent("click", ".add-applicant-js, .edit-applicant-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        let isEdit = sender.hasClass("edit-applicant-js");
        let docTypeId = $("#docTypeId").val();
        openKendoWindow(
            isEdit ? "Edit" : "Create",
            "Clients",
            {
                type: "GET",
                area: "Admin",
                data: {
                    id: sender.val(),
                    docTypeId: docTypeId
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
                        let oldApplicantId = e.sender.element.data("editedApplicant");
                        requestOptional(
                            "RefreshApplicant",
                            "OutApplication",
                            {
                                type: "GET",
                                data: {
                                    applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                                    clientId: client["Id"],
                                    oldApplicantId
                                },
                                success: (data) => {
                                    if (!data) {
                                        return;
                                    }

                                    if (data.applicants) {
                                        $("#applicantsInfoWrapper").replaceWith(data.applicants);
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
                                    searchClientForm.find("[name=EgnBulstat]").val(client["EgnBulstat"]);
                                    searchClientForm.find("button[type=submit]").trigger("click");
                                },
                                500);
                        }
                    }
                }
            });
    });

    rebindEvent("focusout", "#Object_NameDesc", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        requestOptional(
            "SaveObjectsDescription",
            "OutApplication",
            {
                type: "POST",
                data: {
                    applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                    objectsDesc: sender.val()
                },
                success: (data) => {

                }
            });
    });

    rebindEvent("click", ".add-admatdata-js", (e: JQuery.EventBase) => {
        e.preventDefault();
        let sender = $(e.currentTarget);
        requestOptional(
            "SaveAdmActData",
            "OutApplication",
            {
                type: "POST",
                data: {
                    applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                    legalGrounds: $('[name ="LegalGrounds"]').val(),
                    regNumber: $('[name ="RegNumber"]').val(),
                    regDate: $('[name ="RegDate"]').val(),
                    factGrounds: $('[name ="FactGrounds"]').val(),
                    validByDate: $('[name ="ValidByDate"]').val(),
                    announcementDate: $('[name ="AnnouncementDate"]').val(),
                    effectiveDate: $('[name ="EffectiveDate"]').val(),
                    announcementType: $('[name ="AnnouncementType.Id"]').val(),
                },
                success: (data) => {

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

    $(function () {
        $(".mainGroup").each(function () {
            if ($(this).find(".uploadedFile").length == 0) {
                $(this).remove();
            }

            let emptyUpload = $(this).find(".k-upload-empty")
            if (emptyUpload.length == 0) {
                $(emptyUpload).remove();
            }
        })
    })
    //////$(function () {
    //////    $(".attachment-group-name").each(function () {
    //////        if ($(this).find(".attachment-group-wrapper").length == 0) {
    //////            $(this).remove();
    //////        }
    //////    })
    //////})

    //////$(function () {
    //////    $(".attachment-main-group").each(function () {
    //////        if ($(this).find(".attachment-group-wrapper").length == 0) {
    //////            $(this).remove();
    //////        }
    //////    })


    //////})
}

function onSaveAdmAct(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let saveUrl = sender.attr('formaction');
    ContinueWithSave(saveUrl);
}

function onDeleteAdmAct(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let saveUrl = sender.attr('formaction');
    ContinueWithSave(saveUrl);
}

function onPublishAdmAct(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let saveUrl = sender.attr('formaction');
    let validateAction = sender.attr('validateaction');
    let applicationUniqueId = sender.data("uniqueid");

    requestOptionalUrl(validateAction,
        {
            type: "POST",
            area: "OutAdministrativeAct",
            success: (data) => {
                if (data.success == true) {
                    BissSignRequest(applicationUniqueId, saveUrl);
                }
                else {
                    $('.content').html(data);
                }
            }
        });
}

function BissSignRequest(uniqueId, saveUrl) {
    request.selector.keyusages = new Array();
    request.selector.keyusages.push("digitalSignature");
    request.selector.issuers = new Array();
    var payload = JSON.stringify(request);
    $.ajaxPrefilter(function (options, originalOptions, jqXHR) {
        if (options.url === bissGetUrl) {
            delete options.headers['X-CSRF-TOKEN'];
        }
    });

    $.ajax({
        url: bissGetUrl,
        type: "POST",
        crossDomain: true,
        data: payload,
        contentType: "application/json",
        dataType: "json",
        global: false,
    }).done(function (response) {
        if (response.status == "ok") {
            requestOptional(
                "HashBissCertificate",
                "OutApplication",
                {
                    type: "POST",
                    area: "OutAdministrativeAct",
                    data: {
                        applicationUniqueId: uniqueId,
                        signerCertificateB64: response.chain[0]
                    },
                    success: (data) => {
                        bissSign(data.contents, new Array(data.signedContentsBase64), new Array(data.signedContentsCert), data.signerCertificateB64, saveUrl);
                    }
                });
        } else {
            displayMessage(getResource("BISSServerError"), messageType.error);
        }
    })
        .fail(function (jqXHR, textStatus, errorThrown) {
            if (jqXHR.status == 0) {
                DownloadBissFile();
                displayMessage(getResource("BISSNotFoundError"), messageType.error);
            }

            if (jqXHR.responseJSON.reasonText) {
                displayMessage(getResource("BissResponseMessage") + `: ${jqXHR.responseJSON.reasonText}`, messageType.error);
            }
        });
}

function bissSign(contents, signedContentsBase64, signedContentsCert, signerCertificateB64, saveUrl) {

    var port = 53952;
    let request = {
        version: "1.0",
        signatureType: "signature",
        contentType: "data",
        confirmText: [getResource("ConfirmSignBISS")],
        signerCertificateB64: signerCertificateB64,
        signedContents: signedContentsBase64,
        signedContentsCert: signedContentsCert,
        contents: [contents],
        hashAlgorithm: "SHA256",
    };

    let payload = JSON.stringify(request);

    $.ajaxPrefilter(function (options, originalOptions, jqXHR) {
        if (options.url === bissSignUrl) {
            delete options.headers['X-CSRF-TOKEN'];
        }
    });

    $.ajax({
        url: bissSignUrl,
        type: "POST",
        crossDomain: true,
        data: payload,
        contentType: "application/json",
        dataType: "json",
        global: false,

    }).done(function (response) {
        if (response.status == "ok") {
            requestOptional(
                "SignBiss",
                "OutApplication",
                {
                    type: "POST",
                    area: "OutAdministrativeAct",
                    data: { clientSignature: response.signatures[0], signerCertificateB64: signerCertificateB64 },
                    success: (data) => {
                        if (data.success == true) {
                            ContinueWithSave(saveUrl);
                        }
                    }
                });
        }
        else {
            displayMessage(getResource("BISSServerError"), messageType.error);
        }
    }).fail(function (jqXHR, textStatus, errorThrown) {
        if (jqXHR.responseJSON.reasonText) {
            displayMessage(getResource("BissResponseMessage") + `: ${jqXHR.responseJSON.reasonText}`, messageType.error);
        }
    });
}

function ContinueWithSave(saveUrl) {
    requestOptionalUrl(saveUrl,
        {
            type: "POST",
            area: "OutAdministrativeAct",
            success: (data) => {
            }
        });
}

function ContinueWithPublish(url) {
    requestOptionalUrl(url,
        {
            type: "POST",
            area: "OutAdministrativeAct",
            success: () => {
                displayMessage(getResource("SuccessfullPublishAdmAct"), messageType.success, false, 0);
            }
        });
}

function onAARegisterTypeChange(e: JQuery.EventBase) {
    let dropDown = $('#RegisterTypeId').data("kendoDropDownList");
    let registerId = dropDown.value();

    requestOptional(
        "GetTypesByRegister",
        "AdmActRegister",
        {
            type: "GET",
            area: "OutAdministrativeAct",
            data: {
                key: registerId
            },
            success: (data) => {
                if (data) {
                    var typeDropDown = $("#TypeId").data("kendoDropDownList");

                    typeDropDown.setDataSource(new kendo.data.DataSource({ data: data }));
                    typeDropDown.enable(true);
                }
            }
        });
}

function onAdminiStrationChangeChange(e: JQuery.EventBase) {
    let dropDown = $('#AdministrationId').data("kendoDropDownList");
    let registerId = dropDown.value();

    requestOptional(
        "GetIssuerByAdministration",
        "AdmActRegister",
        {
            type: "GET",
            area: "OutAdministrativeAct",
            data: {
                key: registerId
            },
            success: (data) => {
                if (data) {
                    var typeDropDown = $("#IssuerId").data("kendoDropDownList");

                    typeDropDown.setDataSource(new kendo.data.DataSource({ data: data }));
                    typeDropDown.enable(true);
                }
            }
        });
}

export function onAddIndetificators(e: JQuery.EventBase) {
    e.preventDefault();
    window.location.reload();
}

function onRemoveObjectAddress(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    requestOptional(
        "RemoveAdmObjectAddress",
        "OutApplication",
        {
            type: "POST",
            area: "OutAdministrativeAct",
            data: {
                applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                uniqueid: sender.data("uniqueid")
            },
            success: (data) => {
                let wrapper = sender.closest(".admact-object-addresses-js");
                sender.closest(".info-row").remove();
                wrapper.find(".flex > .number").each((index, item) => { $(item).html(`${(index + 1)}.`); });
                console.log('wrapper', wrapper)
                if (!wrapper.find('.info-row').length) {
                    wrapper.closest('.admact-object-js').find('.info-row i').removeClass('k-i-caret-alt-down').addClass('k-i-sarrow-e')
                }
            }
        });
}

function onRemoveActObject(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    requestOptional(
        "RemoveAdmObject",
        "OutApplication",
        {
            type: "POST",
            area: "OutAdministrativeAct",
            data: {
                applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                uniqueid: sender.data("uniqueid")
            },
            success: (data) => {
                let wrapper = sender.closest(".admact-objects-js");
                sender.closest(".admact-object-js").remove();
                wrapper.find(".flex > .number").each((index, item) => { $(item).html(`${(index + 1)}.`); });
            }
        });
}

function onRemoveApplicant(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    requestOptional(
        "RemoveApplicant",
        "OutApplication",
        {
            type: "POST",
            area: "OutAdministrativeAct",
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
}

function onRemoveAllObjects(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    requestOptional(
        "RemoveAllObjects",
        "OutApplication",
        {
            type: "GET",
            area: "OutAdministrativeAct",
            data: {
                applicationUniqueId: $("#application").find("[name=UniqueId]:first").val(),
            },
            success: (data) => {
                window.location.reload();
            }
        },
    )
}

function openIdentificatorsInfo(e: JQuery.EventBase) {
    e.preventDefault();
    openKendoWindow(
        "GetExcelToolTip",
        "OutApplication",
        {
            useArea: true,
            area: "OutAdministrativeAct",
        },
        {
            title: `${getResource("Info")}`,
            resizable: true,
            modal: true,
            height: "90%",
            width: "90%",
        }
    )
}

function onOpenAmdActInfo(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget)
    let title = sender.data("regnumber") != undefined ? sender.data("regnumber") : sender.data("regnum");
    openKendoWindow(
        "Info",
        "OutApplication",
        {
            data: {
                id: sender.data("id"),
                groupTypeId: sender.data("outgroupid"),
            },
            useArea: true,
            area: "OutAdministrativeAct",
        },
        {
            title: `${getResource("Info")} ${title}`,
            resizable: true,
            modal: true,
            height: "90%",
            width: "90%",
        }
    )
}

function onSaveConnectedAdmActs(e: JQuery.EventBase) {
    e.preventDefault();
    let connectedInforWrapper = $("#connected-docs-info");
    let connectInfoData = connectedInforWrapper.find(".connected-documents-info-js");

    requestOptional(
        "SaveConnectedDocuments",
        "OutApplication",
        {
            type: "POST",
            area: "OutAdministrativeAct",
            data: {
                applicationUniqueId: $("#application").find("[name=UniqueId]:first").val(),
            },
            success: (data) => {
                if (data.success) {
                    $(".closeKendoWindow-js").trigger('click');
                    if (connectInfoData) {
                        connectInfoData.remove();
                    }

                    if (data.connectedDocs) {
                        connectedInforWrapper.append(data.connectedDocs);
                    }

                }
            },
        }
    )
}

function onChooseConnectedAdmAct(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let id = sender.data("id");
    let regNumber = sender.data("regnumber");
    let regDate = sender.data("regdate");
    let administration = sender.data("administration");
    let typeName = sender.data("typename");

    requestOptional(
        "ConnectDocument",
        "OutApplication",
        {
            type: "POST",
            area: "OutAdministrativeAct",
            data: {
                id,
                regNumber,
                regDate,
                type: {
                    name: typeName
                },
                administration,
                applicationUniqueId: $("#application").find("[name=UniqueId]:first").val(),
            },
            success: (data) => {
                if (data) {
                    let applicantsWrapper = $(".connected-documents-js");
                    if (data["index"] >= 0) {
                        applicantsWrapper.find(`> :eq(${data["index"]})`).replaceWith(data.doc);
                    }
                    else {
                        applicantsWrapper.append(data.doc);
                    }
                }
            },
        }
    )
}

function onRemoveConenctedAdmAct(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let id = sender.data("id");
    let regNumber = sender.data("regnumber");

    requestOptional(
        "RemoveConnectedDocumenct",
        "OutApplication",
        {
            type: "POST",
            area: "OutAdministrativeAct",
            data: {
                id,
                regNumber,
                applicationUniqueId: $("#application").find("[name=UniqueId]:first").val(),
            },
            success: (data) => {
                let wrapper = sender.closest(".connected-documents-js");
                sender.closest(".info-row").remove();
            }
        }
    )
}

function onDocActualityChange(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    requestOptional(
        "ChangeActualityStatus",
        "OutApplication",
        {
            type: "POST",
            area: "OutAdministrativeAct",
            data: {
                applicationUniqueId: $("#application").find("[name=UniqueId]:first").val(),
                status: sender.data("status")
            }
        }
    )
}

function onSearchAdmAct(e: JQuery.EventBase) {
    e.preventDefault();
    openKendoWindow(
        "GetSearchAdmAct",
        "OutApplication",
        {
            type: "GET",
            area: "OutAdministrativeAct",
            data: {
                applicationUniqueId: $("#application").find("[name=UniqueId]:first").val(),
            }
        },
        {
            modal: true,
            title: getResource("SearchAndConnectAdmAct"),
            open: (e) => {
                e.sender.wrapper.css({
                    top: 10
                });
            },
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                }
            }
        });
}


function onAddObjects(e: JQuery.EventBase) {
    let lv = $(serviceObjectWrapperSelector).find(".k-listview").data("kendoListView");
    if (!lv) {
        displayMessage(getResource("NoObjectsToAdd"), messageType.warning);
        return;
    }
    let selectedObjects = lv.dataSource.data().filter(item => item["IsChecked"] == true);

    if (!selectedObjects || selectedObjects.length == 0) {
        displayMessage(getResource("ChooseObject"), messageType.warning);
        return;
    }

    requestOptional(
        "AddObjects",
        "OutApplication",
        {
            type: "POST",
            data: {
                applicationUniqueId: $("#application").find("[name=UniqueId]:first").val(),
                objectIds: selectedObjects.map((item) => item["Id"])
            },
            success: (content) => {
                if (content) {
                    $(".selected-objects-js").html(content);
                }
            }
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
    let uniqueId = sender.data("unique-id");
    let attachmentTypeId = sender.data("type-id");
    let wrapperTarget = (".step-box");
    let attachmentWraper = sender.closest('.attachments-wrap');
    let required = attachmentWraper.find('label:first-of-type').hasClass('required');

    if ($(sender).closest(".attachment-group-wrapper")) {
        //uniqueId = sender.find('.add-outgroup-attachment-js').data('type-id');
        //attachmentTypeId = sender.find('.add-outgroup-attachment-js').data('unique-id');
        wrapperTarget = ".attachment-group-wrapper";
    }

    requestOptional(
        "AddAttachment",
        "OutApplication",
        {
            type: "POST",
            data: {
                applicationUniqueId: uniqueId,
                attachmentTypeId,
                isRequired: required ? true : false
            },
            success: (content) => {
                if (content) {
                    let wrapper = sender.closest(wrapperTarget);
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

function onAddAdmActObject(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let isEdit = sender.hasClass("edit-admact-object-js");
    let admObjectUniqueId = isEdit ? sender.data("uniqueid") : null;

    openKendoWindow(
        isEdit ? "EditObject" : "AddObject",
        "OutApplication",
        {
            type: "GET",
            area: "OutAdministrativeAct",
            data: {
                applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                admObjectUniqueId: admObjectUniqueId,
            }
        },
        {
            modal: true,
            title: isEdit ? getResource("EditObject") : getResource("AddObject"),
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    let admObject = e.sender.element.data("item");

                    let objectsWrapper = $(".admact-objects-js");
                    ////TODO - remove element that has been edited
                    objectsWrapper.replaceWith(e.sender.element.data("items"));
                    ///objectsWrapper.remove()
                    ///objectsWrapper.append(admObject);

                }
            }
        });
}

function onAddAddressToAdmActObject(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let isEdit = sender.hasClass("edit-admact-object-address-js");
    openKendoWindow(
        isEdit ? "EditObjectAddress" : "AddObjectAddress",
        "OutApplication",
        {
            type: "GET",
            area: "OutAdministrativeAct",
            data: {
                applicationUniqueId: sender.closest("form").find("[name=UniqueId]:first").val(),
                admObjectUniqueId: sender.data("admobjectuniqueid"),
                admObjectAddressUniqueId: sender.data("uniqueid"),
            }
        },
        {
            modal: true,
            title: getResource("AddObjectAddress"),
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    let admObjectAddress = e.sender.element.data("item");

                    ///let objectAdressesWrapper = $(".admact-object-addresses-js");
                    let admActObjectWrapper = sender.closest(".admact-object-js");
                    admActObjectWrapper.replaceWith(e.sender.element.data("item"));
                    ////admActObjectWrapper.append(admObjectAddress);

                }
            }
        });
}

function onEditAdmActState(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    let selectedItem = getSelectedItemByGrid(grid);

    openKendoWindow(
        "AdmActStateUpsert",
        "OutApplication",
        {
            type: "GET",
            area: "OutAdministrativeAct",
            data: {
                admActId: selectedItem.get("Id")
            }
        },
        {
            modal: true,
            title: `${getResource("EditOf")} ${getResource("AdmActStatus")}`,
            close: (e) => {
                if (e.userTriggered) {
                    return;
                }

                let success = e.sender.element.data("success");
                if (success === true) {
                    let form = sender.closest("form");
                    let grid = kendo.widgetInstance(form.find("#recipients")) as kendo.ui.Grid;
                    if (grid) {
                        grid.dataSource.read();
                    }

                    displayMessage(getResource("Success"), messageType.success);
                }
            }
        });
}

function OnDeleteAdmActState(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let grid = getGrid(sender) as kendo.ui.Grid;
    if (!grid) return;
    let row = sender.closest("tr");
    let rowData = grid.dataItem(row);
    let rowId = rowData.get("Id");
    if (rowId) {
        requestOptional(
            "AdmActStateHistoryDeleteRow",
            "OutApplication",
            {
                area: "OutAdministrativeAct",
                type: "POST",
                data: {
                    rowId: rowId
                },
                success: (data) => {
                    if (data.success) {
                        grid.dataSource.read()
                    }
                }
            });
    }
}

function onArrowClick(e: JQuery.EventBase) {
    e.preventDefault();
    let sender = $(e.currentTarget);

    if (sender.hasClass("k-i-sarrow-e")) {
        sender.removeClass('k-i-sarrow-e').addClass('k-i-caret-alt-down')
        sender.closest('.admact-object-js').find('.admact-object-addresses-js').show();

    } else {
        sender.removeClass('k-i-caret-alt-down').addClass('k-i-sarrow-e')
        sender.closest('.admact-object-js').find('.admact-object-addresses-js').hide();
    }
}

function addAdmActStateRow(e: JQuery.EventBase): void {
    e.preventDefault();

    let stateDropDownId = $("#StateUpsertModel_State_Id").data("kendoDropDownList").value();
    let data = {
        State: {
            Id: stateDropDownId,
            Name: $("#StateUpsertModel_State_Id").data("kendoDropDownList").text()
        },
        Date: $("#StateUpsertModel_ChangeDate").val(),
    };

    if (String(stateDropDownId).trim() === getDisputedGUID()) {
        data["Dispute"] = {
            Description: $("#StateUpsertModel_Dispute_Description").val(),
            Attachment: {
                Url: $(".k-file-success").find("input[name$='Url']").val(),
                Name: $(".k-file-success").find("input[name$='Name']").val(),
                Size: $(".k-file-success").find("input[name$='Size']").val()
            }
        }
    }

    requestOptional(
        "AdmActStateHistoryAddRow",
        "OutApplication",
        {
            area: "OutAdministrativeAct",
            type: "POST",
            data: data,
            success: (data) => {
                if (data.success) {
                    let gridGuid = $("#gridGuid").val();
                    let grid = $("#" + gridGuid).data("kendoGrid") as kendo.ui.Grid;
                    grid.dataSource.read();
                    onFileUploadRemove(e);
                    $("#StateUpsertModel_Dispute_Description").val('');
                }
            }
        });
}

function downloadFile(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);

    requestOptional(
        "Download",
        "Attachment",
        {
            type: "GET",
            data: {
                ids: [sender.data("downloadid")]
            },
            success: (data) => {

            }
        });
}

function getDisputedGUID() {
    return "c87aa861-856f-4763-a8ab-b9f74faa1302";
}

function onAttachmentGroupNameClick(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    sender.closest("fieldset").toggleClass('open')
}

function DownloadBissFile() {
    let actions = [
        {
            text: getResource("Download"),
            action: () => {
                requestOptional(
                    "DownloadBiss",
                    "OutApplication",
                    {
                        type: "GET",
                        area: "OutAdministrativeAct",
                        success: (data) => {
                            if (data.success == true) {
                                window.location.href = data.url;
                            }
                        }
                    });
                return true;
            },
            primary: true
        },
        {
            text: getResource("Close")
        }
    ];
    createKendoDialog({
        title: getResource("DownloadBissTitle"),
        content: getResource("ConfirmDownloadBiss"),
        visible: true,
        actions: actions
    });

}

function openModelVersion(e: JQuery.EventBase): void {
    e.preventDefault();
    let sender = $(e.currentTarget);
    openKendoWindow(
        "VersionInfo",
        "OutApplication",
        {
            area: "OutAdministrativeAct",
            type: "GET",
            data: {
                fileId: sender.data("fileid")
            },
        },
        {
            title: `${getResource("Info")} ${sender.data("name")} - ${getResource("Version")} : ${sender.data("date")}`,
            resizable: true,
            modal: true,
            height: "90%",
            width: "90%",
        }
    )
}

init();

export const signCallback = (sender: JQuery, json: any) => {
    let form = $(`#${sender.attr("form")}`);
    form.find("#signApplication").val(json.signature);
    sender.removeClass("sign-xml-js");
    sender.trigger("click");
};

export function onAdmActStateChange(e: kendo.ui.DropDownListSelectEvent): void {
    e.preventDefault();
    let selectedValue = e.sender.value();
    let disputeContainer = $("#disputeContainer");

    if (String(selectedValue).trim() === getDisputedGUID()) {
        disputeContainer.show();
    } else {
        disputeContainer.hide();
    }
}