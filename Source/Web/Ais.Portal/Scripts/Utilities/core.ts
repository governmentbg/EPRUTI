import { getResource } from "scripts/Utilities/resources";
declare let globalVariables: any;

function init(): void {
    initAjaxAction();
    initAjaxPreFilter();
    predefineMethods();
    customBinding();
    initAjaxValidator();
    onDocumentReady();
    initDirtyForms();
    initMainMenu();
}

function initMainMenu(): void {
    let menu = $("nav#mainMenu");
    if (!menu) {
        return;
    };

    let url = location.href;
    menu.find("ul li.active").removeClass("active");
    let split = ["", "?", "/index"];
    for (var i = 0; i < split.length; i++) {
        url = split[i] ? url.split("#")[0].split(split[i].length > 1 ? new RegExp(split[i], "ig") : split[i])[0] : url;
        let active = selectMenuItem(menu, url);
        if (active) {
            break;
        }
    }
}

function selectMenuItem(menu: JQuery, url: string): JQuery {
    let item: JQuery = null;
    if (!menu) {
        return item;
    }

    let anchors = menu.find("ul li a");
    let culture = $("html").attr("lang").slice(0, 2);
    let regexExpresion = `(\/${culture})(.*)`;
    let matches = url.match(regexExpresion);
    if (!matches || matches.length < 2) {
        return;
    }

    let urlMatch = matches[2];
    if (anchors) {
        for (var i = 0; i < anchors.length; i++) {
            let element = $(anchors[i]);
            if (element.attr("href") && element.attr("href") !== "#") {
                let match = element.attr("href").match(regexExpresion);
                let anchorMatch = match && match.length > 2 ? match[2] : null;
                if (anchorMatch && urlMatch && anchorMatch.toLowerCase() === urlMatch.toLowerCase()) {
                    item = element;
                    element.parents("li").addClass("active");
                    break;
                }
            }
        }
    }

    return item;
}

function predefineMethods(): void {
    Date.prototype.toString = function () {
        return new Date(this).toJSON();
    }

    if (!String.prototype.trim) {
        String.prototype.trim = function () {
            return this.replace(/^\s+|\s+$/g, '');
        };
    }

    if (!String.prototype.format) {
        String.prototype.format = function () {
            let formatted = this;
            for (let i = 0; i < arguments.length; i++) {
                let regexp = new RegExp('\\{' + i + '\\}', 'gi');
                formatted = formatted.replace(regexp, arguments[i]);
            }
            return formatted;
        };
    }

    if (!String.prototype.endsWith) {
        String.prototype.endsWith = function (searchString, position: any) {
            let subjectString = toString();
            if (position === undefined || position > subjectString.length) {
                position = subjectString.length;
            }
            position -= searchString.length;
            let lastIndex = subjectString.indexOf(searchString, position);
            return lastIndex !== -1 && lastIndex === position;
        };
    }

    if (!Array.prototype.indexOf) {
        Array.prototype.indexOf = function (what, i) {
            i = i || 0;
            let L = length;
            while (i < L) {
                if (this[i] === what) return i;
                ++i;
            }
            return -1;
        };
    }

    if (!Array.prototype.remove) {
        Array.prototype.remove = function () {
            let what, a = arguments, L = a.length, ax;
            while (L > 1 && length) {
                what = a[--L];
                while ((ax = this.indexOf(what)) !== -1) {
                    this.splice(ax, 1);
                }
            }

            return this;
        }
    }

    if (!Array.prototype.filter) {
        Array.prototype.find = function (predicate) {
            if (this == null) {
                throw new TypeError('Array.prototype.find called on null or undefined');
            }
            if (typeof predicate !== 'function') {
                throw new TypeError('predicate must be a function');
            }
            let list = Object(this);
            let length = list.length >>> 0;
            let thisArg = arguments[1];
            let value;

            for (let i = 0; i < length; i++) {
                value = list[i];
                if (predicate.call(thisArg, value, i, list)) {
                    return value;
                }
            }
            return undefined;
        };
    }

    if (!Array.prototype.find) {
        Object.defineProperty(
            Array.prototype,
            'find',
            {
                value: function (predicate) {
                    // 1. Let O be ? ToObject(this value).
                    if (this == null) {
                        throw new TypeError('"this" is null or not defined');
                    }

                    let o = Object(this);

                    // 2. Let len be ? ToLength(? Get(O, "length")).
                    let len = o.length >>> 0;

                    // 3. If IsCallable(predicate) is false, throw a TypeError exception.
                    if (typeof predicate !== 'function') {
                        throw new TypeError('predicate must be a function');
                    }

                    // 4. If thisArg was supplied, let T be thisArg; else let T be undefined.
                    let thisArg = arguments[1];

                    // 5. Let k be 0.
                    let k = 0;

                    // 6. Repeat, while k < len
                    while (k < len) {
                        // a. Let Pk be ! ToString(k).
                        // b. Let kValue be ? Get(O, Pk).
                        // c. Let testResult be ToBoolean(? Call(predicate, T, « kValue, k, O »)).
                        // d. If testResult is true, return kValue.
                        let kValue = o[k];
                        if (predicate.call(thisArg, kValue, k, o)) {
                            return kValue;
                        }
                        // e. Increase k by 1.
                        k++;
                    }

                    // 7. Return undefined.
                    return undefined;
                }
            });
    }
}

function onDocumentReady(): void {

    // Init choose culture
    initCulture();

    // https://docs.telerik.com/kendo-ui/styles-and-layout/sass-themes/font-icons-migration?_ga=2.157137405.753777278.1696839885-1946725180.1689083986&_gl=1*1d6g2hz*_ga*MTk0NjcyNTE4MC4xNjg5MDgzOTg2*_ga_9JSNBCSF54*MTY5Njg1OTMwNi44NC4xLjE2OTY4NTk5MTEuMi4wLjA.#continuing-with-font-icons
    kendo.setDefaults('iconType', 'font');
    $(function () {
        // Use kendo for select inputs
        $("select").each(function () {
            var element = $(this);
            if (!element.closest(".k-dropdownlist")) {
                element.kendoDropDownList();
            }
        });

        // Widgets Are Hidden after Postbacks When Using jQuery Validation
        // http://docs.telerik.com/kendo-ui/aspnet-mvc/troubleshoot/troubleshooting-validation
        $(".k-widget").removeClass("input-validation-error");

        // Set call resize of all not support auto-resizing kendo controlls who used
        //https://docs.telerik.com/kendo-ui/styles-and-layout/using-kendo-in-responsive-web-pages#non-autoresizing-widgets-in-container
        $(window).on('resize',
            function () {
                kendo.resize("body", false);
            });

        $(document).on("submit", "form", function (e) {
            let sender = $(e.currentTarget);
            if (sender.find('.input-validation-error').length < 1 && sender.data("ajax") !== true) {
                if (sender.data("submitted") === true) {
                    e.preventDefault();
                    return false;
                }

                sender.data("submitted", true);
                return true;
            }
        });

        $("#searchTablebutton").on('click', (e) => {
            if (e.originalEvent?.isTrusted) {
                var headerheight = $('header').height();
                $(".whiteboxresult").show().css("min-height", "85vh");
                $('html, body').animate({
                    scrollTop: $(".whiteboxresult").offset().top - headerheight + 20
                }, 777);
            }
        });

        initTooltips();

        initDomChangeObserver();

        initMasonryLayout();
    });
}

function initMasonryLayout(): void {
    if (window.innerWidth > 1240) {
        let wrapper = $(".js-masonry-layout");
        if (wrapper.length != 1) {
            return;
        }

        wrapper.children().addClass("msrItem").end().msrItems({
            colums: 2,
            margin: 10,
        });

        let time;
        $(window).on("resize", function (e) {
            clearTimeout(time);
            time = setTimeout(function () {
                wrapper.msrItems("refresh");
            }, 200);
        });

        let config = {
            attributes: true,
            childList: true,
            subtree: true
        };

        let observer = new MutationObserver(function (mutations, obs) {
            observer.disconnect();
            wrapper.msrItems("refresh");
            observer.observe(wrapper[0], config);
        });

        observer.observe(wrapper[0], config);
    }
}

function initDomChangeObserver(): void {
    let observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            if (mutation.type === 'childList') {
                if (mutation.addedNodes && mutation.addedNodes.length > 0) {
                    mutation.addedNodes.forEach((item) => {
                        initTooltips(item);
                    });
                }
            }
        });
    });

    observer.observe(
        $("body")[0],
        {
            childList: true,
        });
}

function initTooltips(wrapper: any = null): void {
    $(wrapper ? wrapper : "body").find(`.k-tooltip-bottom[title!=''], .k-tooltip-right[title!=''], .k-tooltip-left[title!=''], .k-tooltip-top[title!='']`).each((index, item) => {
        let tooltip = $(item);
        let tooltipType = tooltip.attr("class").split(' ').find((item) => item.startsWith("k-tooltip-"));
        tooltip.kendoTooltip(
            {
                position: tooltipType.split('-').pop()
            })
    });
}

function initCulture(): void {
    let html = $('html');
    let currentCulture = html.attr('lang');
    let currencySymbol = html.attr('currency');
    let decimals: number = 6;
    let datePaterns = {
        d: "dd.MM.yyyy",
        D: "dd MMMM yyyy",
        F: "dd MMMM yyyy HH:mm:ss",
        g: "dd.MM.yyyy HH:mm",
        G: "dd.MM.yyyy HH:mm:ss",
    };
    switch (currentCulture.toLocaleLowerCase()) {
        case "bg":
        case "bg-bg":
            {
                kendo.cultures[currentCulture] = $.extend(
                    true,
                    {},
                    kendo.cultures[currentCulture],
                    {
                        name: currentCulture,
                        numberFormat: {
                            ",": " ",
                            ".": ".",
                            currency: {
                                ",": " ",
                                ".": ".",
                                symbol: currencySymbol
                            },
                            percent: {
                                ",": " ",
                                ".": ".",
                            },
                            decimals: decimals,
                        },
                        calendars: {
                            standard: {
                                patterns: datePaterns
                            }
                        },
                    });

                break;
            }

        case "en":
        case "en-us":
            {
                kendo.cultures[currentCulture] = $.extend(
                    true,
                    {},
                    kendo.cultures[currentCulture],
                    {
                        name: currentCulture,
                        numberFormat: {
                            currency: {
                                pattern: ["-n $", "n $"],
                                symbol: currencySymbol
                            },
                            decimals: decimals
                        },
                        calendars: {
                            standard: {
                                patterns: datePaterns
                            }
                        }
                    });

                break;
            }
    }

    kendo.culture(currentCulture);
}

function initAjaxValidator(): void {
    // Override the default ignore setting after including the scripts to enable validation of hidden elements. For instance, widgets like the Kendo UI DropDownList and NumericTextBox have a hidden input to keep the value.
    // http://docs.telerik.com/kendo-ui/aspnet-mvc/validation#use-dataAnnotation-attributes
    $.validator.setDefaults({
        ignore: ".ignore"
    });

    // Globalized Dates and Numbers Are Not Recognized As Valid When Using jQuery Validation
    // http://docs.telerik.com/kendo-ui/aspnet-mvc/troubleshoot/troubleshooting-validation
    $.extend($.validator.methods, {
        date(value, element) {
            return this.optional(element)
                || kendo.parseDate(value) != null
                || ($(element).data("role") == "datepicker" && $(element).data("kendoDatePicker").value() != null)
                || ($(element).data("role") == "datetimepicker" && $(element).data("kendoDateTimePicker").value() != null);
        },
        number(value, element) {
            return this.optional(element) || kendo.parseFloat(value) != null;
        },
        requiredCollection(value, element) {
            return this.optional(element) || (value != null && value.length > 0);
        }
    });

    $.validator.unobtrusive.adapters.add("requiredCollection", [], function (options) {
        options.rules.requiredCollection = {};
        options.messages["requiredCollection"] = options.message;
    });
}

function customBinding(): void {
    rebindEvent(
        "click",
        ".closeKendoWindow-js",
        function (e) {
            e.preventDefault();
            let window = $(this).closest(".k-window-content").data("kendoWindow");
            if (window) {
                window.wrapper.find(".k-svg-i-x,.k-i-x").closest("button").trigger("click");
            }
        });
    rebindEvent(
        "click",
        "a.js-trigger-popup",
        (e: JQuery.EventBase) => {
            e.preventDefault();
            let sender = $(e.currentTarget);
            let url = sender.attr("href");
            if (!url) {
                return;
            }

            let title = sender.data("title");
            if (!title) {
                title = sender.attr("title") ? sender.attr("title") : sender.text();
            }

            let windowOptions: kendo.ui.WindowOptions = {
                title: title,
                modal: true,
                width: sender.data("width"),
                height: sender.data("height")
            };
            openKendoWindowUrl(url, null, windowOptions);
        });
    rebindEvent(
        "click",
        "#logout",
        (e: JQuery.EventBase) => {
            e.preventDefault();
            let sender = $(e.currentTarget);
            let url = sender.attr("href");
            let actions = [
                {
                    text: resources.getResource("Exit"),
                    action: function () {
                        requestOptionalUrl(url, { type: "POST", useArea: false })
                        return true;
                    },
                    primary: true
                },
                {
                    text: resources.getResource("Stay")
                }
            ];

            let dialog = createKendoDialog({ actions: actions });
            dialog.title(resources.getResource("Logоut"));
            dialog.content(resources.getResource("ConfirmLogout"));
            dialog.open();
        });
    rebindEvent(
        "keydown",
        ".click-on-enter-js",
        (e: JQuery.EventBase) => {
            if (e.key === "Enter") {
                let sender = $(e.currentTarget);
                sender.closest(sender.data("wrapper")).find(sender.data("element")).trigger("click");
                return false;
            }
        });
    rebindEvent(
        "click",
        "[ajax-button-submit]",
        onAjaxButtonSubmit);

    rebindEvent("click", ".js-trigger-dropdown", onMyProfileClick);
}

function initAjaxAction(): void {
    $(document).ajaxStart(() => {
        showProgress();
    });

    // Hook global ajax complete - hide loading progress
    $(document).ajaxStop(() => {
        hideProgress();
    });
}

function getProgressElement(): JQuery {
    return $('body');
}

function initAjaxPreFilter(): void {
    $.ajaxPrefilter((options, originalOptions, jqXHR) => {
        let type;

        if (originalOptions.type !== undefined) {
            type = originalOptions.type;
        } else {
            type = options.type;
        }

        if (!type || type.toUpperCase() !== 'POST') {
            return;
        }

        if (!options.headers) {
            options.headers = {};
        }

        if (!options.headers["X-CSRF-TOKEN"]) {
            options.headers["X-CSRF-TOKEN"] = $("input[name^='__RequestVerificationToken']:first").val() as string;
        }
    });
}

export function openKendoWindowContent(content: string, options?: kendo.ui.WindowOptions) {
    if (!content) {
        return;
    }

    if (!options) {
        options = {};
    }

    options.deactivate = (e: kendo.ui.WindowEvent) => {
        // Destroy maps in current window
        e.sender.element.find(".map-js").each((i, elem) => {
            let mapElement = $(elem);
            let map = mapElement.data("map");
            if (map) {
                map.OlMapInstance.setTarget(null);
                mapElement.remove();
            }
        });

        // Destroy window
        kendo.destroy(e.sender.element);
        e.sender.destroy();
        e.sender.element.empty();
    }

    let kendoWindow = $(`<div class=\"${options.classAttribute ? options.classAttribute : ''}\"/>`).appendTo('body').kendoWindow(options).data("kendoWindow");
    kendoWindow.wrapper.find(".k-window-titlebar-actions button").each((i, item) => { $(item).attr("title", getResource($(item).attr("aria-label"))) });
    kendoWindow.content(content).center().open();
    let dirtyCheckForm = $(".k-window-content>form.dirty-check:last");
    if (dirtyCheckForm && !dirtyCheckForm.hasClass("dirtylisten")) {
        dirtyCheckForm.dirtyForms()
    }
}

function isEmpty(val: any): boolean {
    return (val === undefined || val == null || val.length <= 0) ? true : false;
}

function initDirtyForms(): void {

    //// За случаи когато цъкне на линк който не е взет от anchor елемент, а примерно от bookmarks на браузъра се пали beforeunload евент,
    ////а там ням как да се сложи къстъм диалог/месидж - https://stackoverflow.com/questions/38879742/is-it-possible-to-display-a-custom-message-in-the-beforeunload-popup
    $.DirtyForms.dialog = {
        open: function (choice: any = {}, message: any = null, ignoreClass: any = null) {
            let actions = [
                {
                    text: "Да",
                    action: function () {

                        let kwindows = $(".k-window-content");
                        if (kwindows && kwindows.length > 0) {
                            let kwindow = $(kwindows[kwindows.length - 2]).data("kendoWindow");
                            if (kwindow) {
                                //// бъг при затвавяне на прозорец, когато друг е отворен https://github.com/telerik/kendo-ui-core/issues/7796
                                setTimeout(function () {
                                    kwindow.close();

                                });
                            }
                        }

                        if (choice.commit) {
                            choice.proceed = true;
                            choice.commit(new Event("commit"));
                        }
                    },
                },
                {
                    text: "Не",
                    primary: true
                }
            ];

            let dialog = createKendoDialog(
                {
                    actions: actions,
                    close: function (e) {
                        dialog.destroy();
                        choice.commit(new Event("commit"));
                    }
                });
            dialog.title("Внимание!");
            dialog.content("Излизане?");
            dialog.open();
            choice.bindEnterKey = true;
            choice.staySelector = '.k-dialog-buttongroup>.k-button-solid-primary,.k-dialog-close';
            choice.proceedSelelector = ".k-dialog-buttongroup>.k-button-solid-base";
        },
    };

    window.onload = function () {
        addDirtyCheckToForms();
    }
};

function checkForDirtyFormInKendoWindow(e: kendo.ui.WindowCloseEvent, formSelector: any): void {

    if (e.userTriggered && $(formSelector).dirtyForms('isDirty')) {
        e.preventDefault();
        openDirtyFormsDialog();
    }
}

function openDirtyFormsDialog(): void {
    $.DirtyForms.dialog.open();
}

function parseDate(time: any): Date | null {
    if (!time) {
        return null;
    }

    // For timeSpan with MVC default json searialization
    if (time.hasOwnProperty("Hours")) {
        return new Date(
            0,
            0,
            0,
            time.Hours,
            time.Minutes,
            time.Seconds,
            time.Milliseconds);
    }

    return kendo.parseDate(time);
}

function onAjaxButtonSubmit(e: JQuery.EventBase): boolean {
    e.preventDefault();
    let sender = $(e.currentTarget);
    let wrapper = sender.closest("[action]");

    // TODO - base implamentation - create new method to serialize wrapper to json
    let obj = {};
    wrapper.find("input[name], selector[name], textarea[name], select[name]").each((index, item) => {
        obj[$(item).attr("name")] = $(item).val();
    });

    requestOptionalUrl(
        wrapper.attr("action"),
        {
            type: wrapper.data("ajax-method")
                ? wrapper.data("ajax-method")
                : wrapper.attr("method")
                    ? wrapper.attr("method")
                    : "get",
            success: function (data) {
                let update = wrapper.data("ajax-update");
                if (update) {
                    $(update).html(data);
                }
            },
            data: obj
        });

    return false;
}

function onMyProfileClick(e) {
    e.preventDefault();

    let that = $(this);
    if ($('body').hasClass('blackshade'))
        $('body').removeClass('blackshade');
    else
        $('body').addClass('blackshade');
    setTimeout(function () {
        var coords = $(that).offset(), target = $($(that).attr('href'));
        $(that).toggleClass('opened');
        target.css({
            left: coords.left - target.width() + $(that).innerWidth(),
            top: coords.top + 47
        }).toggle().toggleClass('toggled'); //marker
    }, 10);
}

function loopThroughObjRecurs(obj, propExec) {
    let self = this;
    for (var k in obj) {
        if (typeof obj[k] === 'object' && obj[k] !== null && !(obj[k] instanceof Date)) {
            loopThroughObjRecurs(obj[k], propExec)
        } else if (obj.hasOwnProperty(k)) {
            propExec(obj, k)
        }
    }
}

init();

export function triggerAjaxButtonOnEnter(e: JQuery.KeyPressEvent): void {
    if (e.which == 13) {
        $(e.currentTarget).closest("div[data-ajax='true']").find("button[ajax-button-submit]").trigger("click");
    }
}

export function copyToClipboard(str: string) {
    const el = document.createElement('textarea');
    el.value = str;
    el.setAttribute('readonly', '');
    el.style.position = 'absolute';
    el.style.left = '-9999px';
    document.body.appendChild(el);
    el.select();
    document.
        execCommand('copy');
    document.body.removeChild(el);
}

export function addDirtyCheckToForms(): void {
    let dirtyCheckForm = $("form.dirty-check");
    if (dirtyCheckForm.length && !dirtyCheckForm.hasClass("dirtylisten")) {
        dirtyCheckForm.dirtyForms();
    }
}

export function getBaseUrl(): string {
    return globalVariables.BaseUrl;
}

export function rebindEvent(event: string, selector: string, callback: any) {
    $(document).off(event, selector);
    $(document).on(event, selector, callback);
}

export function requestOptional(action: string, controller: string, optional?: RequestOptionalParameters): JQueryXHR {
    let url = getPathToActionMethod(
        action,
        controller,
        {
            area: optional.area ? optional.area : globalVariables.area,
            useArea: optional.useArea
        });

    return requestOptionalUrl(url, optional);
}

export async function requestOptionalAsync(action: string, controller: string, optional?: RequestOptionalParameters): Promise<any> {
    return new Promise((resolve, reject) => {
        optional.success = (data) => resolve(data);
        optional.error = (data) => reject(data);
        requestOptional(action, controller, optional);
    });
}


export function getPathToActionMethod(action: string, controller: string, areaSettings?: AreaSettings): string {
    let controllerArea = globalVariables ? globalVariables.ControllerArea : null;
    let currentCulture = globalVariables ? globalVariables.currentCulture : null;

    let area: string,
        useArea: boolean;

    if (areaSettings != null) {
        area = areaSettings.area;
        useArea = areaSettings.useArea;
    }

    let base = getBaseUrl();

    let areaData = "";

    area = !area || area.length === 0
        ? typeof controllerArea !== "undefined" ? controllerArea : ""
        : area;

    if ((useArea == undefined || useArea === true) && area) {
        areaData = area + "/";
    }

    let currentCultureData = "";
    if (typeof currentCulture !== "undefined" && currentCulture) {
        currentCultureData = currentCulture + "/";
    }

    return base + currentCultureData + areaData + controller + "/" + action + "/";
}

export function requestOptionalUrl(url: string, optional?: RequestOptionalParameters): JQueryXHR {
    if (!url) {
        return;
    }

    if (optional == null) {
        optional = {};
    }

    if (optional.headers == null) {
        optional.headers = {};
    }

    let isPost = typeof optional.type !== 'undefined' && optional.type != null && optional.type.toUpperCase() === 'POST';
    let isDelete = typeof optional.type !== 'undefined' && optional.type != null && optional.type.toUpperCase() === 'DELETE';

    if (optional && optional.headers && !optional.headers["X-CSRF-TOKEN"] && (isPost || isDelete)) {
        optional.headers["X-CSRF-TOKEN"] = $("input[name^='__RequestVerificationToken']:first").val() as string;
    }

    if (isPost) {
        if (optional.data) {
            optional.data = optional.postAsJson == true ? JSON.stringify(optional.data) : optional.data;
        }

        if (!optional.contentType) {
            optional.contentType = optional.postAsJson == true ? "application/json; charset=UTF-8" : "application/x-www-form-urlencoded; charset=UTF-8";
        }
    }

    if (optional.data) {
        loopThroughObjRecurs(optional.data, (obj, prop) => {
            if (obj[prop] instanceof Date) {
                obj[prop] = obj[prop].toJSON();
            }
        });
    }

    return $.ajax(
        url,
        {
            data: optional.data,
            contentType: optional.contentType ? optional.contentType : "application/x-www-form-urlencoded",
            headers: optional.headers,
            success: function (d, e, t) {
                if (optional.success) {
                    optional.success(d, e, t);
                }
            },
            error: function (d, e, t) {
                if (optional.error) {
                    optional.error(d, e, t);
                }
            },
            complete: function (e, t) {
                if (optional.complete) {
                    optional.complete(e, t);
                }
            },
            async: (typeof optional.async === "undefined" || optional.async == null) ? true : optional.async,
            type: optional.type || "GET",
            traditional: optional.traditional === true,
            global: (typeof optional.global === "undefined" || optional.global == null) ? true : optional.global,
            processData: optional.processData,
            dataType: optional.dataType
        });
}

export async function requestOptionalUrlAsync(url: string, optional?: RequestOptionalParameters): Promise<any> {
    if (!optional) {
        optional = {};
    }

    return new Promise((resolve, reject) => {
        optional.success = (data) => resolve(data);
        optional.error = (data) => reject(data);
        requestOptionalUrl(url, optional);
    });
}

export function persistGridOptions(gridSelector: string, localStorageKey: string): void {
    let grid = $(gridSelector).data("kendoGrid") as kendo.ui.Grid;
    if (!grid) {
        return;
    }

    let storageFields = localStorage[localStorageKey];
    if (storageFields) {
        let visibleFields = JSON.parse(storageFields);
        if (!visibleFields || !Array.isArray(visibleFields)) {
            return;
        }

        let reorder: any[] = [];
        for (let i = 0; i < grid.columns.length; i++) {
            let column = grid.columns[i];

            let index = visibleFields.map(object => object.column).indexOf((column.field ? column.field : column.title));
            if (index >= 0) {
                reorder.push({
                    index: index,
                    column: column,
                    width: visibleFields[index].width
                });
            } else {
                grid.hideColumn(column);
            }
        }

        reorder = reorder.sort((a, b) => a.index - b.index);
        for (let j = 0; j < reorder.length; j++) {
            let data = reorder[j];
            grid.reorderColumn(data.index, data.column);

            if (data.width && data.width > 0) {
                grid.resizeColumn(data.column, data.width);
            }
        }
    }
}

export function saveGridColumns(e: kendo.ui.GridEvent): void {
    let grid = e.sender;
    if (!grid) {
        return;
    }

    let reorderEvent = e as kendo.ui.GridColumnReorderEvent;
    if (reorderEvent && reorderEvent.newIndex && reorderEvent.column) {
        e.preventDefault();
        grid.reorderColumn(reorderEvent.newIndex, reorderEvent.column);
    }

    let fields = grid.columns.filter(f => f.hidden != true).map(m => ({ column: m.field ? m.field : m.title, width: parseInt(m.width as string) }));

    localStorage[grid.element.data("storagekey")] = kendo.stringify(fields);
}

export function openKendoWindow(action: string, controller: string, optional?: RequestOptionalParameters, windowOptions?: kendo.ui.WindowOptions, enable: boolean = true): void {
    if (!optional) {
        optional = {};
    }

    let oldSuccessCallback = optional.success;
    if (!oldSuccessCallback) {
        optional.success = function (data) {
            if (data) {
                if (!enable) {
                    data = enableInputs($(data), enable);
                }

                openKendoWindowContent(data, windowOptions);
            }
        };
    }

    let oldCloseCallback = windowOptions ? windowOptions.close : null;
    windowOptions.close = function (e: kendo.ui.WindowCloseEvent) {
        checkForDirtyFormInKendoWindow(e, $(".k-window-content").find("form"))
        if (oldCloseCallback) {
            oldCloseCallback(e);
        }
    }

    requestOptional(action, controller, optional);
}

export function openKendoDialog(action: string, controller: string, optional?: RequestOptionalParameters, dialogOptions: kendo.ui.DialogOptions = null, divClass: string = null): void {
    if (!optional) {
        optional = {};
    }

    let oldSuccessCallback = optional.success;
    if (!oldSuccessCallback) {
        optional.success = function (data) {
            let dialog = createKendoDialog(dialogOptions, divClass);
            dialog.content(data);
            dialog.open();
        };
    }

    requestOptional(action, controller, optional);
}

export function openKendoWindowUrl(url: string, optional?: RequestOptionalParameters, windowOptions?: kendo.ui.WindowOptions, enable: boolean = true): void {
    if (!optional) {
        optional = {};
    }

    let oldSuccessCallback = optional.success;
    if (!oldSuccessCallback) {
        optional.success = function (data) {
            if (!enable) {
                data = enableInputs($(data), enable);
            }

            openKendoWindowContent(data, windowOptions);
        };
    }

    let oldCloseCallback = windowOptions.close;
    windowOptions.close = function (e: kendo.ui.WindowCloseEvent) {
        if (oldCloseCallback) {
            oldCloseCallback(e);
        }
    }

    requestOptionalUrl(url, optional);
}

export function onKendoWindowSuccessCallback(data): void {
    let window = $(this).closest(".k-window-content").data("kendoWindow") as kendo.ui.Window;
    if (!window) {
        return;
    }

    let success = !isEmpty(data) && data.success === true;
    if (success === false) {
        let content = data["result"] ? data["result"] : data;
        if (content) {
            window.content(content);
        }
    } else {
        let keys = Object.keys(data);
        for (let i = 0; i < keys.length; i++) {
            let key = keys[i];
            $(window.element).data(key, data[key]);
        }

        if (data.refreshgrid && data.searchqueryid) {
            let grid = $(kendo.format("div[data-searchqueryid='{0}']", data.searchqueryid)).data("kendoGrid") as kendo.ui.Grid;
            if (grid) {
                grid.dataSource.read();
            }
        }

        window.close();
    }
}

export function onKendoDialogSuccessCallback(data: any): void {
    let dialog = $(this).closest(".k-dialog-content").data("kendoDialog") as kendo.ui.Dialog;
    if (!dialog) {
        return;
    }

    let success = !isEmpty(data) && data.success === true;
    if (success === false) {
        dialog.content(data.result);
    } else {
        Object.keys(data).forEach(function (key) {
            $(dialog.element).data(key, data[key]);
        });

        dialog.close();
    }
}

export function enableInputs(wrapper: JQuery, enable: boolean): JQuery<HTMLElement> {
    if (!enable) {
        wrapper.find("form").each(
            (i, form) => {
                let formElement = $(form);
                formElement.replaceWith(formElement.html());
            });
    }

    wrapper.find("input, textarea, select").each(
        (i, item) => {
            let jqueryElement = $(item);
            let role = jqueryElement.data("role");
            let kendoType: string = "";
            if (role) {
                switch (role) {

                    case "autocomplete":
                        {
                            kendoType = "kendoAutoComplete";
                            break;
                        }

                    case "datepicker":
                        {
                            kendoType = "kendoDatePicker";
                            break;
                        }

                    case "dropdownlist":
                        {
                            kendoType = "kendoDropDownList";
                            break;
                        }

                    case "timepicker":
                        {
                            kendoType = "kendoTimePicker";
                            break;
                        }

                    case "numerictextbox":
                        {
                            kendoType = "kendoNumericTextBox";
                            break;
                        }
                }

                if (kendoType) {
                    jqueryElement.data(kendoType).enable(enable);
                }
            } else {
                if (jqueryElement.attr("type") === "submit" && !enable) {
                    jqueryElement.remove();
                } else {
                    jqueryElement.prop('disabled', !enable);
                    if (jqueryElement.hasClass("k-textbox")) {
                        if (enable) {
                            jqueryElement.removeClass("k-state-disabled");
                        } else {
                            jqueryElement.addClass("k-state-disabled");
                        }
                    }
                }
            }
        });

    return wrapper;
}

export function createKendoDialog(options: kendo.ui.DialogOptions = null, divClass: string = null): kendo.ui.Dialog {
    options = options != null ? options : {};
    options.visible = options.visible ? options.visible : false;
    options.closable = options.closable ? options.closable : true;
    options.modal = options.modal ? options.modal : true;
    options.close = (e: kendo.ui.DialogCloseEvent) => {
        e.sender.destroy();
        e.sender.element.empty();
    };

    let dialog = $(divClass ? '<div class="' + divClass + '"/>' : '<div />')
        .appendTo('body')
        .kendoDialog(options)
        .data("kendoDialog");
    return dialog;
}

export function setDirtyForm(formId: string): void {
    if (!formId) {
        return;
    }

    let form = $(kendo.format("#{0}", formId));
    if (form && !form.hasClass("dirty")) {
        form.addClass("dirty");
    }
}

export function filterData(e) {
    let filters = e && e.filter && e.filter.filters && e.filter.filters.length > 0
        ? e.filter.filters
        : null;

    let data = {};
    if (filters) {
        for (let i = 0; i < filters.length; i++) {
            data[filters[i].field] = filters[i].value;
        }
    }

    return data;
}

export const initUploadTemplate = (sender: kendo.ui.Upload, file: any): string => {
    file['prefix'] = sender.element.data("prefix")
    return kendo.template(sender.options.template as string)({ files: [file] });
}

export const onUpload = (e: kendo.ui.UploadUploadEvent) => {
    e.data = kendo.antiForgeryTokens();
}

export const onUploadSuccess = (e: kendo.ui.UploadSuccessEvent) => {
    if (e.operation === "upload") {
        for (let i = 0; i < e.files.length; i++) {
            let file = e.files[i];
            let attachment = Array.isArray(e.response.files) ? e.response.files[i] : e.response.files;
            if (attachment) {
                Object.getOwnPropertyNames(attachment).forEach((key) => {
                    if (!file[key]) {
                        file[key.toLowerCase()] = attachment[key];
                    }
                })

                $(`li[data-uid='${e.files[i]["uid"]}']`).html(initUploadTemplate(e.sender, file));
            }
        }
    }
}

export const getFileSizeMessage = (file) => {
    let size = 0;
    if (typeof file.size == 'number') {
        size = file.size;
    } else {
        return '';
    }

    return formatFileSizeMessage(size);
}

export const formatFileSizeMessage = (size: number) => {
    if (!size || size <= 0) {
        return '';
    }

    let totalSize = size / 1024;
    if (totalSize < 1024) {
        return totalSize.toFixed(2) + ' KB';
    } else {
        return (totalSize / 1024).toFixed(2) + ' MB';
    }
}

export function formatDate(data: any, format: string = null, defaultVal: string = ""): string {
    let date = parseDate(data);
    if (!date) {
        return defaultVal ? defaultVal : "";
    }

    return kendo.toString(date, format ? format : "g");
}

export function getLastKWindowObjectTitle(): string {
    let kWindow = $(".k-window-content:visible:last").data("kendoWindow") as kendo.ui.Window;
    return kWindow ? kWindow.options.objectTitle : null;
}

export function userRoleChange(e: kendo.ui.DropDownListChangeEvent): void {
    let actions = [
        {
            text: resources.getResource("Change"),
            action: function () {
                requestOptional(
                    "ChangeRole",
                    "Account",
                    {
                        type: "POST",
                        useArea: false,
                        data: {
                            roleId: e.sender.dataItem()["RoleId"],
                            groupId: e.sender.dataItem()["UserGroupId"]
                        }
                    });
                return true;
            },
            primary: true
        },
        {
            text: resources.getResource("Cancel")
        }
    ];

    let dialog = createKendoDialog({ actions: actions });
    dialog.title(resources.getResource("ChangeProfileRole"));
    dialog.content(resources.getResource("ConfirmChangeProfileRole"));
    dialog.open();
}

export function initFormSteps(wrapperSelector: string = null) {
    const stepIndexSeparator = ". ";
    let wrapper = wrapperSelector ? $(wrapperSelector) : $(document);
    wrapper.find(".step-box .step-box-head .step-box-title:visible").each((index, elem) => {
        let text = $(elem).text();
        if (!$(elem).attr("title")) {
            $(elem).attr("title", text);
        }

        $(elem).text(`${index + 1}${stepIndexSeparator}${$(elem).attr("title")}`);
    });
}

export function applicationXmlUploadSuccess(e: kendo.ui.UploadSuccessEvent) {
    if (e.operation == "upload" && e.files.length == 1) {
        let loadUrl = e.sender.element.data("load");
        requestOptionalUrl(
            loadUrl,
            {
                type: "GET",
                data: {
                    url: e.response.files["url"]
                }
            });
    }
}

export const downloadUrl = (uri: string, name: string = null) => {
    if (!uri) {
        return;
    }

    let link = document.createElement("a");
    link.download = name;
    link.href = uri;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

export const b64toBlob = (b64Data, contentType = 'application/zip', sliceSize = 512) => {
    const byteCharacters = atob(b64Data);
    const byteArrays = [];

    for (let offset = 0; offset < byteCharacters.length; offset += sliceSize) {
        const slice = byteCharacters.slice(offset, offset + sliceSize);

        const byteNumbers = new Array(slice.length);
        for (let i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i);
        }

        const byteArray = new Uint8Array(byteNumbers);
        byteArrays.push(byteArray);
    }

    const blob = new Blob(byteArrays, { type: contentType });
    return blob;
}

export function onSettlementChange(e: kendo.ui.DropDownListChangeEvent): void {
    let sender = e.sender;
    let postCodeSelector = sender.element.data("postcodeinputselector");

    requestOptional(
        "GetPostCode",
        "Address",
        {
            useArea: false,
            data: {
                ekatteId: sender.dataItem() && sender.dataItem().Id ? sender.dataItem().Id : null
            },
            success: (data) => {
                $(postCodeSelector).val(data);
            }
        }
    );


    let regionDropDown = $(sender.element.data("regionelementselector")).data("kendoDropDownList");
    if (!regionDropDown) {
        return;
    }
    let label = regionDropDown.element.closest("div").find("label");
    if (!label) {
        return;
    }

    let sofiaId = sender.element.data("sofiaid");
    let varnaId = sender.element.data("varnaid");
    if (sender.value() == sofiaId || sender.value() == varnaId) {
        label.addClass("required");
    } else {
        label.removeClass("required");
    }
}

export function onCountryDataBound(e: kendo.ui.DropDownListChangeEvent): void {
    e.sender.trigger("change");
}

export function onCountryChange(e: kendo.ui.DropDownListChangeEvent): void {
    let sender = e.sender;
    let provinceDropDown = $(sender.element.data("provinceselector")).data("kendoDropDownList");
    let municipalityDropDown = $(sender.element.data("municipalityselector")).data("kendoDropDownList");
    let settlementDropDown = $(sender.element.data("settlementselector")).data("kendoDropDownList");
    let regionDropDown = $(sender.element.data("regionselector")).data("kendoDropDownList");
    let quarterInput = sender.element.closest("form").find("input[name*='Quarter']");
    let descriptionInput = $(sender.element.data("descriptionselector"));

    // Bulgaria id
    let defaultId = sender.element.data("default");
    if (sender.dataItem().Id != defaultId) {
        if (provinceDropDown) {
            provinceDropDown.enable(false);
            $("input[name*='Province.Name']").prop("disabled", true);
            provinceDropDown.element.closest("div.form-input").hide();
        }

        if (municipalityDropDown) {
            municipalityDropDown.enable(false);
            $("input[name*='Municipality.Name']").prop("disabled", true);
            municipalityDropDown.element.closest("div.form-input").hide();
        }

        if (settlementDropDown) {
            settlementDropDown.enable(false);
            $("input[name*='Settlement.Name']").prop("disabled", true);
            settlementDropDown.element.closest("div.form-input").hide();
        }

        if (regionDropDown) {
            regionDropDown.enable(false);
            $("input[name*='Region.Name']").prop("disabled", true);
            regionDropDown.element.closest("div.form-input").hide();
        }

        if (quarterInput) {
            quarterInput.prop("disabled", true);
            quarterInput.closest("div.form-input").hide();
        }

        if (descriptionInput) {
            descriptionInput.prop("disabled", false);
            descriptionInput.closest("div.form-input").show();
        }
    } else {
        if (provinceDropDown) {
            provinceDropDown.enable(true);
            $("input[name*='Province.Name']").prop("disabled", false);
            provinceDropDown.element.closest("div.form-input").show();
        }

        if (municipalityDropDown) {
            municipalityDropDown.enable(true);
            $("input[name*='Municipality.Name']").prop("disabled", false);
            municipalityDropDown.element.closest("div.form-input").show();
        }

        if (settlementDropDown) {
            settlementDropDown.enable(true);
            $("input[name*='Settlement.Name']").prop("disabled", false);
            settlementDropDown.element.closest("div.form-input").show();
        }

        if (regionDropDown) {
            regionDropDown.enable(true);
            $("input[name*='Region.Name']").prop("disabled", false);
            regionDropDown.element.closest("div.form-input").show();
        }

        if (quarterInput) {
            quarterInput.prop("disabled", false);
            quarterInput.closest("div.form-input").show();
        }

        if (descriptionInput) {
            descriptionInput.prop("disabled", true);
            descriptionInput.closest("div.form-input").hide();
        }
    }
}

let progressCount = 0;

export function showProgress(): void {
    if (progressCount == 0) {
        let element = getProgressElement();
        kendo.ui.progress(element, true);
        element.addClass("no-scroll");
    }
    progressCount++;
}

export function hideProgress(): void {
    if (progressCount > 0) {
        progressCount--;
    }

    if (progressCount == 0) {
        let element = getProgressElement();
        $("body").removeClass("no-scroll");
        ($ as any).validator.unobtrusive.parse(document);
        kendo.ui.progress(element, false);
    }
}

export function GetFileUrl(file: any): string {

    if (file && file.Url != null) {
        return `${getPathToActionMethod("Download", "Attachment")}?${$.param({ urls: file.Url })}`;
    }

    if (file && file.Id != null) {
        return `${getPathToActionMethod("Download", "Attachment")}?${$.param({ ids: file.Id })}`;
    }

    return null;
}

export function clearCache(): void {
    window.sessionStorage.clear();
    window.localStorage.clear();
    location.reload();
}