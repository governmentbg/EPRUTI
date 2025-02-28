import { rebindEvent, requestOptional, openKendoWindowContent } from "scripts/Utilities/core";
import { getSelectedItemByGrid, getGrid, onDeleteMvcTableItem, confirmDelete, getSelectedItemByTr } from "scripts/Utilities/searchTable";
import { getResource } from "scripts/Utilities/resources";
import { displayMessage, messageType } from "scripts/Utilities/notification";

function init() {
    rebindEvent("change", ".imageDiv>label>input[type='checkbox']", onMainPictureCheckBoxChange);
    rebindEvent("click", ".file-remove-js", onRemoveFileClick);
    rebindEvent("click", ".publication-remove-js", onRemovePublicationClick);
};

function onRemovePublicationClick(e: JQuery.EventBase): void {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let grid = getGrid(sender);
    let dataItem = getSelectedItemByTr(sender.closest("tr"))

    if (!dataItem) {
        return;
    }

    let searchQueryId = grid.wrapper.data("searchqueryid")

    confirmDelete(
        () => {
            requestOptional(
                "Delete",
                "Publications",
                {
                    type: "DELETE",
                    useArea: true,
                    area: "admin",
                    data: {
                        id: dataItem["Id"],
                        searchQueryId: searchQueryId
                    },
                    success: () => {
                        onDeleteMvcTableItem(grid, dataItem);
                        displayMessage(getResource("DeleteSuccess"), messageType.success);
                    }
                });
            return true;
        });
}

function onMainPictureCheckBoxChange(e) {
    let sender = $(e.currentTarget);
    if (!sender) {
        return;
    }

    let listView = $(e.currentTarget).closest(".k-listview").data("kendoListView") as kendo.ui.ListView;
    if (!listView) {
        return;
    }

    let dataItem = listView.dataSource.get(sender.data("uid"));
    if (!dataItem) {
        return;
    }

    dataItem["IsMain"] = e.currentTarget.checked;

    let checkboxes = $(".imageDiv>label>input[type='checkbox']");
    if (!checkboxes) {
        return;
    }

    checkboxes.each((index, elem) => {
        if (elem != e.currentTarget) {
            $(elem).prop("checked", false);
        }
    });

    listView.dataItems().forEach((item) => {
        if (item != dataItem) {
            item["IsMain"] = false;
        }
    });

    listView.refresh();
}

function onRemoveFileClick(e) {
    e.preventDefault();

    let sender = $(e.currentTarget);
    let element = sender.closest("[role='listitem']");
    let listView = sender.closest(".k-listview").data("kendoListView");
    let selectedItem = listView.dataItem(element);

    if (!selectedItem) {
        return;
    }

    confirmDelete(
        () => {
            listView.dataSource.remove(selectedItem);
            return true;
        });
}

// https://docs.telerik.com/aspnet-core/knowledge-base/upload-resize-image-before-upload
function openResizeImage(e: kendo.ui.UploadSelectEvent) {
    let uploader = e.sender;
    let file = e.files[0];
    let id = kendo.guid(), buttonId = kendo.guid();
    let content =
        '<div class="form-control fullwidth">' +
        `<img id='${id}'></img>` +
        '</div>' +
        '<div class="clear"></div>' +
        '<div class="form-control fullwidth">' +
        `<button id='${buttonId}' class='bttn main caps right' type='button'>${getResource("Save")}</button>` +
        '</div>';

    openKendoWindowContent(content, { title: getResource("ResizeImage"), width: '80%', height: '80%' });

    $(`#${buttonId}`).one("click", function (evt) {
        let kendoWindow = $(evt.currentTarget).closest(".k-window-content").data("kendoWindow") as kendo.ui.Window;
        let cropper = $(`#${id}`).data("cropper");
        if (cropper.cropped === true) {
            let isIE = navigator.userAgent.indexOf("MSIE ") > -1 || navigator.userAgent.indexOf("Trident/") > -1;

            if (isIE) {
                if (!HTMLCanvasElement.prototype.toBlob) {
                    Object.defineProperty(HTMLCanvasElement.prototype, 'toBlob', {
                        value: function (callback, type, quality) {
                            let dataURL = this.toDataURL(type, quality).split(',')[1];
                            setTimeout(function () {

                                let binStr = atob(dataURL),
                                    len = binStr.length,
                                    arr = new Uint8Array(len);

                                for (let i = 0; i < len; i++) {
                                    arr[i] = binStr.charCodeAt(i);
                                }

                                callback(new Blob([arr], { type: type || 'image/png' }));

                            });
                        }
                    });
                }
            }


            let croppedCanvas = cropper.getCroppedCanvas();
            croppedCanvas.toBlob(function (blob) {
                blob.lastModifiedDate = new Date();
                blob.name = file.name;

                // Replace the original files with the new files.
                try {
                    file.size = blob.size;
                    file.rawFile = new File([blob], file.name);
                } catch (err) {
                    let textFileAsBlob = new Blob([blob], { type: 'application/xml' });
                    let navigator: any = window.navigator;

                    navigator.msSaveBlob(textFileAsBlob, file.name);
                }

                // Force to upload modify image - async.autoUpload must be set to false
                uploader.upload();

                if (kendoWindow) {
                    kendoWindow.close();
                }
            });
        }
    })

    let reader = new FileReader();
    reader.onload = function (e: ProgressEvent) {
        let image = $(`#${id}`);
        image.attr("src", this.result as string);
        image.cropper({
            dragMode: 'move',
            cropBoxResizable: 0,
            data: {
                width: uploader.element.data("width"),
                height: uploader.element.data("height")
            }
        });
    }
    reader.readAsDataURL(file.rawFile);
}

init();

export function placeholderAttachmentPositionChange(element) {
    return element.clone().css("opacity", 0.1);
}

export function hintAttachmentPositionChange(element) {
    return element.clone().removeClass("k-state-selected");
}

export function onAttachmentPositionChange(e) {
    let listView = $(e.sender.element).data("kendoListView");
    if (!listView) {
        return;
    }

    let dataSource = listView.dataSource,
        newIndex = e.newIndex,
        dataItem = dataSource.getByUid(e.item.data("uid"));

    dataSource.remove(dataItem);
    dataSource.insert(newIndex, dataItem);
}

export function onPublicationAttachmentSuccessUpload(e: kendo.ui.UploadSuccessEvent) {

    let upload = e.sender as kendo.ui.Upload,
        listView = $(upload.options.dropZone).data("kendoListView") as kendo.ui.ListView;

    if (e.operation == "upload" && e.response && e.response.files && e.response.files.length > 0) {
        for (let i = 0; i < e.response.files.length; i++) {
            if (listView.dataSource.data().length < 30) {
                listView.dataSource.add(e.response.files[i]);
            } else {
                displayMessage(getResource("MaximumImageCount"), messageType.error);
                return false;
            }
        }
    }
}

export function onImageSelect(e: kendo.ui.UploadSelectEvent) {
    // A timeout so you do not break the built-in features and throw errors.
    // Note that the file size that is shown by the Upload will therefore be wrong.
    // Consider using a template to hide it: https://demos.telerik.com/aspnet-mvc/upload/templates.
    setTimeout(function () {
        openResizeImage(e);
    });
}