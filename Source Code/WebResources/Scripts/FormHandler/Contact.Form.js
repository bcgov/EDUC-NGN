var Edu = Edu || {};

Edu.filterParentCustomer = function (executionContext) {
    var formContext = executionContext.getFormContext();  

    formContext.getControl("parentcustomerid").setEntityTypes(["account"]);
}

Edu.ShowHideContactRoles = function (executionContext) {
    var formContext = executionContext.getFormContext();  
    
    if (formContext.getAttribute("customertypecode").getValue() === 1) {
        //alert(formContext.getAttribute("customertypecode").getValue());
        formContext.ui.tabs.get("SUMMARY_TAB").sections.get("SUMMARY_TAB_section_contact_roles").setVisible(true);
    }
    else {
        formContext.ui.tabs.get("SUMMARY_TAB").sections.get("SUMMARY_TAB_section_contact_roles").setVisible(false);
    }
}



