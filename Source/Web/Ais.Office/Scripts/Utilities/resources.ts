let resources: any = {};
let isLoaded: boolean = false;
async function init() {
    let culture = $('html').attr('lang');
    let resourceKey = `resources_${culture}`;
    let storeData = window.sessionStorage.getItem(resourceKey);
    if (storeData) {
        resources = JSON.parse(storeData);
    }

    if (!isLoaded) {
        isLoaded = true;
        resources = await $.get(`/${culture.split('-')[0]}/Resources/ReadResources`);
        $.DirtyForms.message = resources.ConfirmLeaveMessage;
        window.sessionStorage.setItem(resourceKey, JSON.stringify(resources));
    }
}

init();

export function getResource(key: string): string {
    return resources[key] as string ?? key;
}