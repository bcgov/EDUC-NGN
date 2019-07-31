/*
 * Requires /Scripts/Common/Edu.Common.js
 */

var Edu = Edu || {};

Edu.ShowHideVendorTypeSection = function (executionContext) {
    var formContext = executionContext.getFormContext();  

    //Get Vendor
    if (formContext.getAttribute("edu_vendor").getValue() !== null) {
        var vendorId = Edu.Common.removeCurlyBraces(formContext.getAttribute("edu_vendor").getValue()[0].id);
        var vendorOptions = "?$select=edu_vendortype";

        formContext.ui.tabs.get("tab_vendor").setVisible(true);
        //get Vendor Type
        Xrm.WebApi.retrieveRecord("account", vendorId, vendorOptions).then(
            function success(result) {
                if (result.edu_vendortype === 100000000) {
                    //Show tab_vendor_section_telus
                    formContext.ui.tabs.get("tab_general").sections.get("tab_vendor_section_telus").setVisible(true);
                    formContext.ui.tabs.get("tab_general").sections.get("tab_vendor_section_av").setVisible(false);
                }
                else {
                    //Show tab_vendor_section_av
                    formContext.ui.tabs.get("tab_general").sections.get("tab_vendor_section_telus").setVisible(false);
                    formContext.ui.tabs.get("tab_general").sections.get("tab_vendor_section_av").setVisible(true);
                }
            },
            function (error) {
                alert("There was an error reading the Vendor Type.");
            });

    }
    else {
        //hide vendor tab
        formContext.ui.tabs.get("tab_vendor").setVisible(false);
        formContext.ui.tabs.get("tab_general").sections.get("tab_vendor_section_telus").setVisible(false);
        formContext.ui.tabs.get("tab_general").sections.get("tab_vendor_section_av").setVisible(false);
    }


}