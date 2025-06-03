export { };

declare global {
    interface Window {
        getCookie(key: string): string;
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
        traditional?: boolean;
        async?: boolean;
        global?: boolean;
        area?: string;
        useArea?: boolean;
        contentType?: string | false | undefined;
        cache?: boolean;
        headers?: any;
        postAsJson?: boolean = false;
        processData?: boolean | undefined;
        dataType?: 'xml' | 'html' | 'script' | 'json' | 'jsonp' | 'text' | string | undefined;
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

    declare namespace kendo {
        function setDefaults(option: string, val: string): void;
    }

    declare namespace kendo.ui {
        interface WindowOptions {
            objectTitle?: string,
            classAttribute?: string
        }
    }

    declare var resources: {
        getResource: (key: string) => string;
    }

    declare var SCS: {
        signBase64XML: (data: string) => Promise;
        signXML: (data: string) => Promise;
        Base64Decode: (data: any) => string;
    }
}