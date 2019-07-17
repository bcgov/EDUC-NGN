/*To Do: Add method to filter school district contacts*/

var Edu = window.Edu || {};

//Hardcoded to Case Lookup View 
//This is a managed system view and should not be able to be deleted, so hardcoding should be on
var CASE_LOOKUP_VIEW = "{A2D479C5-53E3-4C69-ADDD-802327E67A0D}";


Edu.filterSDContacts = function (executionContext) {
    //get form context
    var formContext = executionContext.getFormContext();
    //limit lookup to only contacts
    formContext.getControl("customerid").setEntityTypes(["contact"]);

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

    //Get Service Cost
    if (formContext.getAttribute("edu_service").getValue() != null) {
        var serviceId = (formContext.getAttribute("edu_service").getValue()[0].id).replace(/[{}]/g, "");
        var serviceOptions = "?$select=edu_cost";

        //Get and Set Service Cost
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

// Function to hide and show Sections depending on Service Type (Service and Incident)
// Runs: On Load and On change of Case Type
Edu.hideShowCaseSection = function (executionContext) {

    // Get the Form Context
    var formContext = executionContext.getFormContext();

    //Get the Case Category
    var caseCat = formContext.getAttribute("edu_casetype").getValue();
    var tabSummary = formContext.ui.tabs.get("tab_summary");
    var secService = tabSummary.sections.get("sec_seriveoverview"); 
    var secIncident = tabSummary.sections.get("sec_incidentoverview");
    var typeValService = 100000001;
    var typeValticket = 100000000;
    var typeValFujitsu = 100000002;

    // Depending on Category, show and hide fields
    if (caseCat == typeValService) {
        //case is service type
        secService.setVisible(true);
        secIncident.setVisible(false);
    } else if ((caseCat == typeValticket) || (caseCat == typeValFujitsu)) {
        //case is incident type
        secService.setVisible(false);
        secIncident.setVisible(true);
    } else {
        //case has no type
        secService.setVisible(false);
        secIncident.setVisible(false);
    }
}