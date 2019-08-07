/*To Do: Add method to filter school district contacts*/

var Edu = window.Edu || {};

//Hardcoded to Case Lookup View 
//This is a managed system view and should not be able to be deleted, so hardcoding should be on
var CASE_LOOKUP_VIEW = "{A2D479C5-53E3-4C69-ADDD-802327E67A0D}";
var CASE_BPF_STATUS;
Edu.caseLoad = function (executionContext) {
    //get form context
    //debugger;
    var formContext = executionContext.getFormContext();
    //get current bpf status
    CASE_BPF_STATUS = formContext.data.process.getStatus();
    formContext.data.process.addOnProcessStatusChange(EduStatusOnChange);
}

EduStatusOnChange = function (executionContext) {
    var formContext = executionContext.getFormContext();
    //var stagename = Xrm.Page.data.process.getActiveStage().getName();
    //debugger;
    var status = formContext.data.process.getStatus();
    
    if (status == 'finished' && status !== CASE_BPF_STATUS) {
        //Mscrm.OpportunityCommandActions.opportunityClose(1);
        //alert('finished clicked');
        Edu.Case.ResolveIncident(formContext);
    }
}

Edu.filterSDContacts = function (executionContext) {

    //get form context
    var formContext = executionContext.getFormContext();

    //Get Case Type
    var type = formContext.getAttribute("edu_casetype").getValue();

    //If type is Service Request or Incident
    if (type === 100000001 || type === 100000000) {
        //limit lookup to only contacts
        formContext.getControl("customerid").setEntityTypes(["contact"]);
    }

    //if school district isn't populated, don't continue
    if (formContext.getAttribute("edu_schooldistrict").getValue() === null) return;

    var viewId = CASE_LOOKUP_VIEW;
    var schoolDistrictId = formContext.getAttribute("edu_schooldistrict").getValue()[0].id;

    var fetchXml = "<fetch version=\"1.0\" mapping=\"logical\" distinct=\"true\">" +
        "<entity name=\"contact\">" +
        "<attribute name=\"fullname\" />" +
        "<attribute name=\"telephone1\" />" +
        "<attribute name=\"contactid\" />" +
        "<order attribute=\"fullname\" descending=\"false\" />" +
        "<link-entity name=\"edu_schooldistrictcontact\" from=\"edu_contact\" to=\"contactid\" link-type=\"inner\" alias=\"ab\">" +
        "<filter type=\"and\">" +
        "<condition attribute=\"edu_schooldistrict\" operator=\"eq\"  value=\"" + schoolDistrictId + "\" />" +
        "</filter>" +
        "</link-entity>" +
        "</entity>" +
        "</fetch>";

    var layoutXml = "<grid name=\"resultset\" object=\"2\" jump=\"fullname\" select=\"1\" preview=\"1\" icon=\"1\">" +
        "<row name=\"result\" id=\"contactid\">" +
        "<cell name=\"fullname\" width=\"200\" />" +
        "</row></grid>";

    formContext.getControl("customerid").addCustomView(viewId,
        "contact", "Custom View", fetchXml, layoutXml, true);

}

// Function to show and hide the request tab depending on the category lookup
// Runs: On Load and On Change of Case Sub-Category
Edu.hideShowPSIrequestTab = function (executionContext) {

    // Get the Form Context and declare variables
    var formContext = executionContext.getFormContext();
    var subCat = formContext.getAttribute("edu_subcategory").getValue();
    var tabPsi = formContext.ui.tabs.get("tab_psiteluscheck");

    // if the subcategory has a value
    if (subCat != null) {
        var subCatName = subCat[0].name;
        //check the value's name is New Service
        if (subCatName === "New Service") {
            tabPsi.setVisible(true);
        } else {
            tabPsi.setVisible(false);
        }
    } else {
        //default = hide
        tabPsi.setVisible(false);
    }
}

// Function to retrive the service cost on a Case from the Service and enter it to monthly cost
// Runs: On change of Selected Service
Edu.RetriveServiceCost = function (executionContext) {

    // Get the Form Context
    var formContext = executionContext.getFormContext();

    // Retrieve Vendor Ref
    var vendorRef = formContext.getAttribute("edu_vendor").getValue();
    var serviceCost = formContext.getAttribute("edu_service").getValue();

    // Ensure that the vendor lookup has a value
    if (vendorRef != null) {

        // Retrieve service cost if the vendor is Telus and the service cost is empty (fires once)
        if (vendorRef.name == "Telus" && serviceCost == null) {

            // Get Service Cost
            if (formContext.getAttribute("edu_service").getValue() != null) {
                var serviceId = (formContext.getAttribute("edu_service").getValue()[0].id).replace(/[{}]/g, "");
                var serviceOptions = "?$select=edu_cost";

                // Get and Set Service Cost
                Xrm.WebApi.retrieveRecord("edu_service", serviceId, serviceOptions).then(
                    function success(result) {
                        formContext.getAttribute("edu_totalmonthlycost").setValue(result.edu_cost);
                    },
                    function (error) {
                        alert("There was an error reading the Service & Cost.");
                    }
                );
            }
        }
    }
}

// Function to hide and show Sections depending on Service Type (Service and Incident)
// Runs: on load and on change of Case Type
Edu.hideShowCaseSection = function (executionContext) {

    // Get the Form Context
    var formContext = executionContext.getFormContext();

    // Get the Case Category
    var caseCat = formContext.getAttribute("edu_casetype").getValue();
    var tabSummary = formContext.ui.tabs.get("tab_summary");
    var secService = tabSummary.sections.get("sec_seriveoverview");
    var secIncident = tabSummary.sections.get("sec_incidentoverview");
    var tabModelAndCosts = formContext.ui.tabs.get("tab_ModelandCosts");
    var tabSDApproval = formContext.ui.tabs.get("tab_sdapproval");
    var tabServiceReq = formContext.ui.tabs.get("tab_servicerequest");
    var typeValService = 100000001;
    var typeValticket = 100000000;
    var typeValFujitsu = 100000002;

    // Depending on Category, show and hide fields
    switch (caseCat) {
        case typeValService:
            // Case is of type service request
            secService.setVisible(true);
            secIncident.setVisible(false);
            tabModelAndCosts.setVisible(true);
            tabSDApproval.setVisible(true);
            tabServiceReq.setVisible(true);
            break;

        case typeValticket:
        case typeValFujitsu:
            // Case is of type incident
            secService.setVisible(false);
            secIncident.setVisible(true);
            tabModelAndCosts.setVisible(false);
            tabSDApproval.setVisible(false);
            tabServiceReq.setVisible(false);
            break;

        //Case has no type
        default:
            secService.setVisible(false);
            secIncident.setVisible(false);
            tabModelAndCosts.setVisible(false);
            tabSDApproval.setVisible(false);
            tabServiceReq.setVisible(false);
    }
}

// Function to lock and clear the subcatehory depending on category
// Runs: on load and on change of case catgory
Edu.caseCategoryOnLoadandChange = function (executionContext) {

    var formContext = executionContext.getFormContext();
    var categoryAtr = formContext.getAttribute("edu_category");
    var subCategoryAtr = formContext.getAttribute("edu_subcategory");
    var subCategoryCtrl = formContext.getControl("edu_subcategory");
    var categoryVal = categoryAtr.getValue();

    if (categoryVal == null) {
        //clear and disable subcategory field
        subCategoryAtr.setValue(null);
        subCategoryCtrl.setDisabled(true);
    } else {
        //enable subcat field
        subCategoryCtrl.setDisabled(false);
    }
}