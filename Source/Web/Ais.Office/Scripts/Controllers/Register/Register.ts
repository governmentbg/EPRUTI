export function onRegisterDataChange(e: JQuery.TriggeredEvent): void {
    let sender = $(e.currentTarget);

    let selected = sender.val();

    if (selected) {
        $("#RegisterData_RegisterNumber").show();
    } else {
        $("#RegisterData_RegisterNumber").hide();
    }
    
}
