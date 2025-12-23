function hookMessageEvent(messageHandler) {
    var eventMethod = window.addEventListener ? "addEventListener" : "attachEvent";
    var eventer = window[eventMethod];
    var messageEvent = eventMethod === "attachEvent" ? "onmessage" : "message";

    if (eventer != null) {
        eventer(messageEvent, messageHandler);
    }
}