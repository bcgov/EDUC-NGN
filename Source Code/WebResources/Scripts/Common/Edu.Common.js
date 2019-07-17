var Edu = Edu || {};

Edu.Common = Edu.Common || {};

Edu.Common.removeCurlyBraces = function (str) {
    return str.replace(/[{}]/g, "");
}

Edu.Common.getLookup = function (executionContext, fieldName) {
    var formContext = executionContext.getFormContext();
    var lookupFieldObject = formContext.entity.attributes.get(fieldName);
    if (lookupFieldObject.getValue() != null && lookupFieldObject.getValue()[0] != null) {
        entityId = lookupFieldObject.getValue()[0].id;
        entityName = lookupFieldObject.getValue()[0].entityType;
        entityLabel = lookupFieldObject.getValue()[0].name;
        var obj = {
            id: entityId,
            type: entityName,
            value: entityLabel
        };
        return obj;
    }
}

Edu.Common.isBlank = function (str) {
    return (!str || /^\s*$/.test(str));
}

Edu.Common.validatePhone = function (executionContext, fieldName) {
    var formContext = executionContext.getFormContext();
    var regex = /^\(?(\d{3})\)?[- ]?(\d{3})[- ]?(\d{4})\s?(x\d*)?$/;
    var phoneNumber = formContext.getAttribute(fieldName).getValue();

    if (phoneNumber === null) {
        formContext.getControl(fieldName).clearNotification();
        return;
    }

    if (!regex.test(phoneNumber)) {
        formContext.getControl(fieldName).setNotification('Invalid value, please enter in the following format (000) 000-0000 x0000');
    } else {
        formContext.getControl(fieldName).clearNotification();
    }
}