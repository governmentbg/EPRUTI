////get keys from - https://github.com/telerik/kendo-ui-core/blob/master/src/messages/kendo.messages.en-US.js
(function ($, undefined) {

    /* PivotGrid messages */

    if (kendo.ui.PivotGrid) {
        kendo.ui.PivotGrid.prototype.options.messages =
            $.extend(true, kendo.ui.PivotGrid.prototype.options.messages, {
                "measureFields": "Пусни полета тук",
                "columnFields": "Пусни колони тук",
                "rowFields": "Пусни редове тук"
            });
    }

    /* PivotFieldMenu messages */

    if (kendo.ui['PivotFieldMenu']) {
        kendo.ui["PivotFieldMenuV2"].prototype.options.messages =
            $.extend(true, kendo.ui["PivotFieldMenuV2"].prototype.options.messages, {
                "info": "Покажи полета със стойност която:",
                "filterFields": "Филтер полета",
                "filter": "Филтрирай",
                "include": "Включи полета...",
                "title": "Полета да включват",
                "clear": "Премахни",
                "ok": "Ок",
                "cancel": "Откажи",
                "operators": {
                    "contains": "Съдържа",
                    "doesnotcontain": "Не съдържа",
                    "startswith": "Започва с",
                    "endswith": "Завършва на",
                    "eq": "Е равно на",
                    "neq": "Не е равно на",
                }
            });
    }

    /* PivotConfiguratorV2 messages */

    if (kendo.ui.PivotConfiguratorV2) {
        kendo.ui.PivotConfiguratorV2.prototype.options.messages =
            $.extend(true, kendo.ui.PivotConfiguratorV2.prototype.options.messages, {
                "title": "Настройки",
                "cancelButtonText": "Отказ",
                "applyButtonText": "Приложи",
                "measures": "Избери полета за начална настройка",
                "columns": "Избери полета за начална настройка",
                "rows": "Избери полета за начална настройка"
            });
    }

    /* PivotFieldMenuV2 messages */

    if (kendo.ui['PivotFieldMenuV2']) {
        kendo.ui['PivotFieldMenuV2'].prototype.options.messages =
            $.extend(true, kendo.ui['PivotFieldMenuV2'].prototype.options.messages, {
                "apply": "Приложи",
                "sortAscending": "Сортирай възходящо",
                "sortDescending": "Сортирай низходящо",
                "filterFields": "Филтрирай полета",
                "filter": "Филтрирай",
                "include": "Включи полета...",
                "clear": "Премахни",
                "reset": "Нулирай",
                "moveToColumns": "Премести в Колони",
                "moveToRows": "Премести в Редове",
                "movePrevious": "Премести преди",
                "moveNext": "Премести след",
                "filterOperatorsDropDownLabel": "Регионални оператори за филтриране",
                "filterValueTextBoxLabel": "Регионална стойност за филтриране",
                "operators": {
                    "eq": "Е равно на",
                    "neq": "Не е равно на",
                    "startswith": "Започва с",
                    "contains": "Съдържа",
                    "doesnotcontain": "Не съдържа",
                    "endswith": "Завършва на"
                }
            });
    }

    if (kendo.ui.FileManager) {
        kendo.ui.FileManager.prototype.options.messages =
            $.extend(true, kendo.ui.FileManager.prototype.options.messages, {
                toolbar: {
                    createFolder: "Нова папка",
                    upload: "Качи",
                    sortDirection: "Сортиране посока",
                    sortDirectionAsc: "Сортиране възходящ",
                    sortDirectionDesc: "Сортиране низходящ",
                    sortField: "Сортирай по",
                    nameField: "Име",
                    sizeField: "Големина",
                    typeField: "Тип",
                    dateModifiedField: "Дата на промяна",
                    dateCreatedField: "Дата на създаване",
                    listView: "Списък",
                    gridView: "Таблица",
                    search: "Търси",
                    details: "Виж детайли",
                    detailsChecked: "включено",
                    detailsUnchecked: "изключено",
                    "delete": "Изтриване",
                    rename: "Преименувай"
                },
                views: {
                    nameField: "Name",
                    sizeField: "File Size",
                    typeField: "Type",
                    dateModifiedField: "Date Modified",
                    dateCreatedField: "Дата на създаване",
                    items: "Елементи",
                    listLabel: "Файлов мениджър списък",
                    gridLabel: "Файлов мениджър таблица",
                    treeLabel: "Файлов мениджър дърво"
                },
                dialogs: {
                    upload: {
                        title: "Качи файлове",
                        clear: "Изчисти",
                        done: "Готово"
                    },
                    moveConfirm: {
                        title: "Потвърждане",
                        content: "<p class='k-text-center'>Искате ли да преместите или копирате?</p>",
                        okText: "Копирай",
                        cancel: "Премести",
                        close: "затвори"
                    },
                    deleteConfirm: {
                        title: "Потвърждаване",
                        content: "<p class='k-text-center'>Сигурни ли сте, че искате да изтриете избраните файлове?<br/>Не можете да върнете назад.</p>",
                        okText: "Изтриване",
                        cancel: "Отказ",
                        close: "Затвори"
                    },
                    renamePrompt: {
                        title: "Prompt",
                        content: "<p class='k-text-center'>Новo име за файл.</p>",
                        okText: "Преименувай",
                        cancel: "Откажи",
                        close: "затвори"
                    }
                },
                previewPane: {
                    noFileSelected: "Няма избран файл",
                    extension: "Тип",
                    size: "Големина",
                    created: "Дата на създаване",
                    createdUtc: "Дата на създаване UTC",
                    modified: "Дата на промяна",
                    modifiedUtc: "Дата на промяна UTC",
                    items: "Елементи"
                }
            });
    }

    if (kendo.ui.Upload) {
        kendo.ui.Upload.prototype.options.localization =
            $.extend(true, kendo.ui.Upload.prototype.options.localization, {
                "cancel": "Спри",
                "clearSelectedFiles": "Изчисти файловете",
                "dropFilesHere": "Преместете с мишката файлове тук за да ги качите",
                "invalidMaxFileSize": "Размерът на файла е твърде голям.",
                "invalidMinFileSize": "Размерът на файла е твърде малък.",
                "invalidFileExtension": "Този тип файл не е разрешен.",
                "remove": "Премахни",
                "retry": "Опитай отново",
                "select": "Избери...",
                "statusFailed": "грешка",
                "statusUploaded": "качен",
                "statusUploading": "качва се",
                "uploadSelectedFiles": "Качи файловете",
                "headerStatusUploaded": "Готово",
                "headerStatusUploading": "Качване..."
            });
    }

    if (kendo.ui.FilterMenu) {
        kendo.ui.FilterMenu.prototype.options.messages =
            $.extend(true, kendo.ui.FilterMenu.prototype.options.messages, {
                "buttonTitle": "{0} филтрирай колони123"
            });
    }

})(window.kendo.jQuery);