import { displayMessage, messageType } from 'scripts/Utilities/notification';
import { getResource } from 'scripts/Utilities/resources';

const domain: string = "https://localhost";
const timeout: number = 2000;
const minimalVersionNumber: number = 1.0;
const ports: number[] = [53952, 53953, 53954, 53955];
let apiUrl: string = null;

interface VersionReponse {
    version: string;
    httpMethods: string;
    contentTypes: string;
    signatureTypes: string[];
    selectorAvailable: boolean;
    hashAlgorithms: string;
}

interface SignerSelector {
    Issuers: string[];
    akis: string[];
    keyUsages: string[];
}

interface SignerRequest {
    Selector?: SignerSelector;
    showValidCerts?: boolean;
}

interface CodeReponse {
    status: string;
    reasonCode: string;
    reasonText: string;
}

interface SignerReponse extends CodeReponse {
    chain?: string[];
}

interface SignReponse extends CodeReponse {
    signatures?: string[];
}

interface SignRequest {
    version?: string;
    contents: string[];
    signedContents: string[];
    signedContentsCert: string[];
    contentType: string;
    hashAlgorithm?: string;
    signatureType?: string;
    signerCertificateB64: string;
    confirmText?: string;
}

interface StatusReponse {
    status: string;
    statusCode: string;
    totalCount: number;
    currentCount: number;
}

function validateReasonCode(response: CodeReponse, silent: boolean = false): boolean {
    let message: string;
    let code = Number(response.reasonCode);
    switch (code) {
        case 200:
            {
                return true;
            }
        default: {
            message = `BISSReasonCode${code}`;
        }
    }

    if (!silent && message) {
        displayMessage(getResource(message), messageType.warning);
    }

    return false;
}

export const validate = (silent: boolean = false, force: boolean = false): boolean => {
    if (force) {
        apiUrl = null;
    }

    if (!apiUrl) {
        let oldVersionFlag: boolean = false;
        for (var i = 0; i < ports.length; i++) {
            let url = `${domain}:${ports[i]}/`;
            $.ajax({
                type: "GET",
                url: `${url}version`,
                crossDomain: true,
                dataType: "json",
                async: false,
                global: false,
                timeout: timeout,
                success: function (result) {
                    let response = result as VersionReponse;
                    if (Number(response.version) < minimalVersionNumber) {
                        oldVersionFlag = true;
                    }
                    else {
                        apiUrl = url;
                    }
                },
            });

            if (apiUrl || oldVersionFlag) {
                break;
            }
        }

        if (oldVersionFlag && silent) {
            displayMessage(getResource("OldBISSVersionMessage"), messageType.warning);
        }
    }

    return (apiUrl && apiUrl.length > 0) == true;
}

export const signer = (filter: SignerRequest = null, successCallback: (response: SignerReponse) => void): void => {
    if (!validate()) {
        return;
    }

    $.ajax({
        type: "POST",
        url: `${apiUrl}getsigner`,
        crossDomain: true,
        data: filter ? JSON.stringify(filter) : null,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            let response = result as SignerReponse;
            if (!validateReasonCode(response)) {
                return;
            }

            if (successCallback) {
                successCallback(response);
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            let signReponse = JSON.parse(jqXHR.responseText) as SignReponse;
            validateReasonCode(signReponse);
        }
    });
}

export const signWithCert = (data: SignRequest, successCallback: (response: SignReponse) => void): void => {
    if (!validate()) {
        return;
    }

    $.ajax({
        type: "POST",
        url: `${apiUrl}sign`,
        crossDomain: true,
        data: JSON.stringify(data),
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            let response = result as SignReponse;
            if (!validateReasonCode(response)) {
                return;
            }

            if (successCallback) {
                successCallback(response);
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            let signReponse = JSON.parse(jqXHR.responseText) as SignReponse;
            validateReasonCode(signReponse);
        }
    });
}

export const sign = (data: SignRequest, filter: SignerRequest = null, successCallback: (response: SignReponse) => void): void => {
    signer(
        filter,
        (response) => {
            data.signerCertificateB64 = response.chain[0];
            signWithCert(
                data,
                successCallback);
        });
}

export const status = (completeCallback: (response: StatusReponse) => void) => {
    if (!validate()) {
        return;
    }

    $.ajax({
        type: "GET",
        url: `${apiUrl}status`,
        crossDomain: true,
        cache: false,
        dataType: "json",
        complete: function (result) {
            let response = (result.responseJSON || null) as StatusReponse;
            if (completeCallback && response) {
                completeCallback(response);
            }
        }
    });
}