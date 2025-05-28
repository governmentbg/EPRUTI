import { getPathToActionMethod, rebindEvent } from "scripts/Utilities/core";


function init() {
    rebindEvent("click", ".event-calendar", onCalendarDateClick)
};

function onCalendarDateClick(e: JQuery.EventBase) {
    let value = $(e.currentTarget).data("date");

    let url = getPathToActionMethod(
        "Index",
        "Publication",
        {
            area: "",
            useArea: false
        });

    let params = $.param({
        type: "Event",
        forDate: kendo.toString(value, "d"),
    });

    location.href = `${url}?${params}`;
}

init();

