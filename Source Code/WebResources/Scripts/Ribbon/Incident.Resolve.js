var Edu = window.Edu || {};

var globalContext = Xrm.Utility.getGlobalContext();
var clientUrl = globalContext.getClientUrl();
var webAPIPath = "/api/data/v9.1";
var GlobalFormContext = {};

Edu.ResolveIncident = function (primaryControl) {
    GlobalFormContext = primaryControl;

    var globalContext = Xrm.Utility.getGlobalContext();
    var clientUrl = globalContext.getClientUrl();

    //TODO: Get case type and if Service Request then set data to true;
    var showCreateCase = false;

    if (GlobalFormContext.getAttribute("edu_casetype").getValue() == 100000001) {
        showCreateCase = true;
    }
    var url = "edu_/Apps/CaseResolution.html?data="+showCreateCase;

    Alert.showWebResource(url, 400, 330, "Resolve Case", [
        new Alert.Button("Resolve", saveResolvedIncident, true, true),
        new Alert.Button("Cancel")
    ], clientUrl, true, null);
}

saveResolvedIncident = function () {

    //need to validate form response first
    var validationResult = Alert.getIFrameWindow().validate();

    if (validationResult) {
        debugger;
        //Then get values and call CloseIncidentRequest or Action to create Order and then close
        var reason = Alert.getIFrameWindow().document.getElementById("txtResolutionReason").value;
        var time = Alert.getIFrameWindow().document.getElementById("txtResolutionTime").value;
        var createOrder = Alert.getIFrameWindow().document.getElementById("chkCreateOrder").checked;

        Edu.ResolveCase(reason, time, createOrder);
        //Close Popup
        Alert.hide();
        return;
    }

}


Edu.ResolveCase = function (resolution, time, createOrder) {
    //debugger;
    var Id = GlobalFormContext.data.entity.getId().replace('{', '').replace('}', '');

    var caseUri = clientUrl + webAPIPath + "/incidents("+Id+")";
        var parameters = {
            CreateOrder: createOrder,
            Resolution: resolution,
            Time: time
    }

        Edu.request("POST", caseUri + "/Microsoft.Dynamics.CRM.edu_ResolveCase", parameters).then(function (value) {
            //alert(value);
            //TODO: Refresh Page
            GlobalFormContext.data.refresh(false);
            // expected output: "foo"
        }, function (err) {
            alert("There was an error resolving the case.  Details: "+err); // Error: "It broke"
        });
}

/**  
 * @function request  
 * @description Generic helper function to handle basic XMLHttpRequest calls.  
 * @param {string} action - The request action. String is case-sensitive.  
 * @param {string} uri - An absolute or relative URI. Relative URI starts with a "/".  
 * @param {object} data - An object representing an entity. Required for create and update actions.  
 * @param {object} addHeader - An object with header and value properties to add to the request  
 * @returns {Promise} - A Promise that returns either the request object or an error object.  
 */  
Edu.request = function (action, uri, data, addHeader) {  
    if (!RegExp(action, "g").test("POST PATCH PUT GET DELETE")) { // Expected action verbs.  
        throw new Error("Sdk.request: action parameter must be one of the following: " +  
            "POST, PATCH, PUT, GET, or DELETE.");  
    }  
    if (!typeof uri === "string") {  
        throw new Error("Sdk.request: uri parameter must be a string.");  
    }  
    if ((RegExp(action, "g").test("POST PATCH PUT")) && (!data)) {  
        throw new Error("Sdk.request: data parameter must not be null for operations that create or modify data.");  
    }  
    if (addHeader) {  
        if (typeof addHeader.header != "string" || typeof addHeader.value != "string") {  
            throw new Error("Sdk.request: addHeader parameter must have header and value properties that are strings.");  
        }  
    }  
  
    // Construct a fully qualified URI if a relative URI is passed in.  
    if (uri.charAt(0) === "/") {  
        uri = clientUrl + webAPIPath + uri;  
    }  
  
    return new Promise(function (resolve, reject) {  
        var request = new XMLHttpRequest();  
        request.open(action, encodeURI(uri), true);  
        request.setRequestHeader("OData-MaxVersion", "4.0");  
        request.setRequestHeader("OData-Version", "4.0");  
        request.setRequestHeader("Accept", "application/json");  
        request.setRequestHeader("Content-Type", "application/json; charset=utf-8");  
        if (addHeader) {  
            request.setRequestHeader(addHeader.header, addHeader.value);  
        }  
        request.onreadystatechange = function () {
            debugger;
            if (this.readyState === 4) {  
                request.onreadystatechange = null;  
                switch (this.status) {  
                    case 200: // Success with content returned in response body.  
                    case 204: // Success with no content returned in response body.  
                    case 304: // Success with Not Modified  
                        resolve(this);  
                        break;  
                    default: // All other statuses are error cases.  
                        var error;  
                        try {  
                            error = JSON.parse(request.response).error;  
                        } catch (e) {  
                            error = new Error("Unexpected Error");  
                        }  
                        reject(error);  
                        break;  
                }  
            }  
        };  
        request.send(JSON.stringify(data));  
    });  
};  