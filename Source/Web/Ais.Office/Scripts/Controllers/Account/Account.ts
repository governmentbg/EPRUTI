import { requestOptional } from "scripts/Utilities/core";


function init() {
};

function refreshData(fromValue, toValue, action) {
    requestOptional(
        action,
        "Account",
        {
            type: "GET",
            data: {
                from: kendo.toString(fromValue, "d"),
                to: kendo.toString(toValue, "d"),
                officeId: $("#OfficeId").val()
            },
            success: (res) => {
                $("#donut-charts-wrapper").html(res);
            }
        });
}

init();

export function periodFromChange(e: kendo.ui.DatePickerChangeEvent) {
    let fromValue = e.sender.value();
    let toValue = $("#TaskByPeriodTo").data("kendoDatePicker").value();
    let action = e.sender.element.hasClass("central") ? "GetDashBoardDonutsDataCentral" : "GetDashBoardDonutsDataOffice" 
    refreshData(fromValue, toValue, action);
}

export function periodToChange(e: kendo.ui.DatePickerChangeEvent) {
    let toValue = e.sender.value();
    let fromValue = $("#TaskByPeriodFrom").data("kendoDatePicker").value();
    let action = e.sender.element.hasClass("central") ? "GetDashBoardDonutsDataCentral" : "GetDashBoardDonutsDataOffice" 
    refreshData(fromValue, toValue, action);
}
