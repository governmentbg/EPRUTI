export { };

declare global {

    interface Navigator {
        msSaveBlob?: (blob: any, defaultName?: string) => boolean
    }

    interface String {
        format(...replacements: string[]): string;
        endsWith(...replacements: string[]): any;
    }

    interface Array<T> {
        remove(): T;
        find(predicate: (search: T) => boolean): T;
        findLastIndex(predicate: (value: T, index: number, obj: T[]) => unknown, thisArg?: any ): number
    }

    interface RequestOptionalParameters {
        success?: Function;
        error?: Function;
        complete?: Function;
        data?: any;
        type?: string;
        xhrFields?: any;
        traditional?: boolean;
        async?: boolean;
        global?: boolean;
        area?: string;
        useArea?: boolean;
        contentType?: string;
        cache?: boolean;
        headers?: any;
        postAsJson?: boolean = false;
        processData?: boolean | undefined;
        dataType?: 'xml' | 'html' | 'script' | 'json' | 'jsonp' | 'text' | string | undefined;
        method?: 'POST' | 'GET';
    }

    interface AreaSettings {
        area?: string;
        useArea?: boolean;
    }

    interface JQueryStatic {
        DirtyForms: any;
    }

    interface JQuery {
        dirtyForms: any;
        msrItems: any;
    }

    interface JQuery<TElement> {
        cropper(options?: CropperOptions): JQuery<TElement>;
        cropper(method: string, ...arguments: any[]): JQuery<TElement>;
        selector: JQuery<TElement>;
        print();
    }

    declare namespace kendo {
        function setDefaults(option: string, val: string): void;
    }

    declare namespace kendo.ui {
        interface WindowOptions {
            objectTitle?: string;
            withoutPrint?: boolean,
            openMaximize?: boolean,
            classAttribute?: string
        }
    }

    declare namespace kendo.data {
        interface DataSource {
            transport: {
                parameterMap?: Function
            }
        }
    }

    declare var SCS: {
        signBase64XML: (data: string) => Promise;
        signXML: (data: string) => Promise;
        Base64Decode: (data: any) => string;
        posSendPaymentPort: (portName: string, amount: string) => Promise;
        selectSigner: () => Promise;
        signDigestSID: (data: any, sid: any) => Promise;
    }
}