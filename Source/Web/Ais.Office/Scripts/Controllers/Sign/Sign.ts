import { requestOptionalAsync, rebindEvent, requestOptionalUrl } from "scripts/Utilities/core";
import { displayMessage, messageType } from "scripts/Utilities/notification";
import { getResource } from "scripts/Utilities/resources";
import * as signalr from "scripts/Utilities/signalr";

async function getSignInfo(wrapper: JQuery): Promise<any> {
    let signer = await SCS.selectSigner();
    let pdfViewer = kendo.widgetInstance(wrapper.parent().find(".k-widget.k-pdf-viewer")) as kendo.ui.PDFViewer;
    let signInfo = await requestOptionalAsync(
        "PrepareDigitSignPdf",
        "Sign",
        {
            type: "POST",
            data: {
                signerName: signer.signerName,
                signerCert: signer.signerCert,
                reason: wrapper.find('[name=Reason]').val(),
                filePath: wrapper.find('[name=Url]').val(),
                fileId: wrapper.find('[name=FileId]').val(),
                entryType: wrapper.find('[name=EntryType]').val(),
                id: wrapper.find('[name=Id]').val(),
                visual: true,
                page: pdfViewer["pages"].length
            }
        });

    return {
        sid: signer.sid,
        hash: signInfo.hash,
        tempPdfId: signInfo.tempPdfId,
    }
}

async function init() {
    rebindEvent("click", ".sign-js", async (e: JQuery.EventBase) => {
        e.preventDefault();
        e.stopPropagation();
        let sender = $(e.currentTarget);
        try {

            sender.attr('disabled', true.toString());

            let form = sender.closest("form");
            let signInfo = await getSignInfo(form);
            if (!signInfo.hash) {
                displayMessage(getResource("InvalidSignHash"), messageType.error);
            }

            let data = await SCS.signDigestSID(signInfo.hash, signInfo.sid);
            if (data.errorCode) {
                displayMessage(getResource("InvalidSignHash"), messageType.error);
            }

            form.find('[name=Signature]').val(data.signature);
            form.find('[name=TempPdfId]').val(signInfo.tempPdfId);
            form.trigger("submit");
        } finally {
            sender.removeAttr('disabled');
        }
    });
    signalr.attachToHub((connection) => {
        connection.off('ReloadDigitSignPdf');
        connection.on('ReloadDigitSignPdf', (url) => {
            var lastWindow = kendo.widgetInstance($('[data-role="window"]:last')) as kendo.ui.Window;
            if (lastWindow) {
                requestOptionalUrl(
                    url,
                    {
                        success: (data) => {
                            if (!data) {
                                return;
                            }

                            lastWindow.element.html(data);
                            lastWindow.maximize();
                        }
                    }
                )
            }
            else {
                window.location.href = url;
            }
        });
    });
}

init();