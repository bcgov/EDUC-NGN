﻿function OrderTypeFieldLogic(executionContext) {

    var formContext = executionContext.getFormContext();

    var fn_site = "edu_site";
    var fn_schdist = "edu_schooldistrict";
    var fn_regsiteservice = "edu_regardingsiteservice";
    var fn_reqservice = "edu_requestedservice";
    var fn_ordertype = "edu_ordertype";

    var InternetNew = 100000003;
    var InternetUpgrade = 100000004;
    var InternetDowngrade = 100000005;
    var PortAdd = 100000008;
    var PortRemove = 100000009;
    var WANNew = 100000002;
    var WANUpgrade = 100000000;
    var WANDowngrade = 100000001;
    var ClosureOfSite = 100000006;
    var ClosureOfService = 100000011;
    var Other = 100000007;

    var orderTypeVal = formContext.getAttribute(fn_ordertype).getValue();

    switch (orderTypeVal) {
        case InternetNew:
        case WANNew:
            formContext.getAttribute(fn_site).setRequiredLevel("required");
            formContext.getAttribute(fn_schdist).setRequiredLevel("required");
            formContext.getAttribute(fn_regsiteservice).setRequiredLevel("none");
            formContext.getAttribute(fn_reqservice).setRequiredLevel("required");
            break;

        case WANDowngrade:
        case InternetUpgrade:
        case InternetDowngrade:
        case PortRemove:
        case PortAdd:
        case WANUpgrade:
            formContext.getAttribute(fn_site).setRequiredLevel("required");
            formContext.getAttribute(fn_schdist).setRequiredLevel("required");
            formContext.getAttribute(fn_regsiteservice).setRequiredLevel("required");
            formContext.getAttribute(fn_reqservice).setRequiredLevel("required");
            break;

        case ClosureOfSite:
            formContext.getAttribute(fn_site).setRequiredLevel("required");
            formContext.getAttribute(fn_schdist).setRequiredLevel("required");
            formContext.getAttribute(fn_regsiteservice).setRequiredLevel("none");
            formContext.getAttribute(fn_reqservice).setRequiredLevel("none");
            break;

        case ClosureOfService:
            formContext.getAttribute(fn_site).setRequiredLevel("required");
            formContext.getAttribute(fn_schdist).setRequiredLevel("required");
            formContext.getAttribute(fn_regsiteservice).setRequiredLevel("required");
            formContext.getAttribute(fn_reqservice).setRequiredLevel("none");
            break;

        case Other:
            formContext.getAttribute(fn_site).setRequiredLevel("none");
            formContext.getAttribute(fn_schdist).setRequiredLevel("none");
            formContext.getAttribute(fn_regsiteservice).setRequiredLevel("none");
            formContext.getAttribute(fn_reqservice).setRequiredLevel("none");
            break;

        default:
            formContext.getAttribute(fn_site).setRequiredLevel("none");
            formContext.getAttribute(fn_schdist).setRequiredLevel("none");
            formContext.getAttribute(fn_regsiteservice).setRequiredLevel("none");
            formContext.getAttribute(fn_reqservice).setRequiredLevel("none");
    }
}