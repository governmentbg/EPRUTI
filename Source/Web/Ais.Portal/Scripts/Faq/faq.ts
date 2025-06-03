import { rebindEvent, requestOptional, } from "scripts/Utilities/core";

function init() {
    rebindEvent("click", ".searchByWord-js", searchFaq);
}

function searchFaq(e): void {
    e.preventDefault();
    let wrapper = $(e.currentTarget).closest(".faq-tab-wrapper");
    let word = wrapper.find("#SearchWord").val()
    let categoryid = wrapper.find("#CategoryId").val();

    requestOptional(
        "Search",
        "Faq",
        {
            type: "POST",
            useArea: false,
            data: {
                categoryid: categoryid,
                searchword: word
            },
            success: (res) => {
                wrapper.find("#faq-result").html(res);
            }
        });
}

init();
