function SpiderEyeBridge(exfn, convertPayloadToJson) {
    var convertPayloadFn = convertPayloadToJson ? JSON.stringify : x => x;

    this.updateTitle = function (title) {
        exfn(convertPayloadFn({
            type: "title",
            parameters: title
        }));
    };

    var callbackIds = 0;
    var callbacks = {};
    this.invokeApi = function (id, parameters, callback) {
        if (typeof id !== "string") {
            throw new Error("id must be a string");
        }

        if (typeof callback !== "function") {
            throw new Error("callback must be a function");
        }

        var callbackId = callbackIds++;
        callbacks[callbackId] = callback;
        exfn(convertPayloadFn({
            type: "api",
            id: id,
            parameters: parameters,
            callbackId: callbackId
        }));
    };

    this._endApiCall = function (callbackId, result) {
        var callback = callbacks[callbackId];
        if (callback) {
            callback(result);
            delete callbacks[callbackId];
        }
    };

    var events = {};
    this.addEventHandler = function (name, callback) {
        if (typeof name !== "string") {
            throw new Error("name must be a string");
        }

        if (typeof callback !== "function") {
            throw new Error("callback must be a function");
        }

        events[name] = callback;
    };

    this.removeEventHandler = function (name) {
        if (typeof name === "string") {
            delete events[name];
        }
    };
    
    this._sendEventAsync = async function (name, callbackId, value) {
        var result = undefined;
        var error = undefined;
        var callback = events[name];
        if (!callback) {
            exfn(convertPayloadFn({
                type: "eventCallback",
                id: name,
                callbackId: callbackId,
                parameters: {
                    success: false,
                    noSubscriber: true
                }
            }));
            return;
        }

        try {
            result = await callback(value);
        } catch (e) {
            if (e instanceof Error) {
                error = {
                    message: e.message,
                    name: e.name,
                    stack: e.stack
                };
            } else {
                error = { message: String(e) };
            }
        }

        exfn(convertPayloadFn({
            type: "eventCallback",
            id: name,
            callbackId: callbackId,
            parameters: {
                result: result,
                hasResult: typeof result !== "undefined",
                error: error,
                success: typeof error === "undefined"
            }
        }));
    };
    
    this._sendEvent = function (name, callbackId, value) {
        this._sendEventAsync(name, callbackId, value);
    }
}
