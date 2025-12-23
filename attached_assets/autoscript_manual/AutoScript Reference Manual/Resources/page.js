// Event fires when the DOM content fully loads.
function onContentLoaded(shellPath) {
    redirectIfNotInFrame(shellPath);
    sendFrameData();
    hookWindowResize();
    createCopyButtons();
}

// Opens page in shell html view with target content in frame if the page is not in frame.
function redirectIfNotInFrame(shellPath) {
    if (window.top === window.self) { // not in a frame
        var target = encodeURIComponent(window.location.href);
        window.location.href = shellPath + "?target=" + target;
    }
}

// Sends frame data as a message.
// Frame data must be sent as a message to avoid cross origin frame.
function sendFrameData() {
    if (parent.postMessage) {
        var jsonMessage = JSON.stringify({ "height": window.document.body.scrollHeight, "location": window.location.href });
        parent.postMessage(jsonMessage, "*");
    }
}

// Hooks message event that fires when the window resizes.
function hookWindowResize() {
    hookMessageEvent(function() {
        sendFrameData();
    });
}

const COPY_CLASSNAME = "copied";

// Creates copy button to all pre elements (code examples).
function createCopyButtons() {
    var preElements = window.document.getElementsByTagName("pre");

    Array.prototype.forEach.call(preElements, function(preElement) {
        var copyButton = document.createElement("button");
        copyButton.title = "Copy snippet to clipboard";
        copyButton.onclick = function () {
            var childElements = preElement.childNodes
            if (childElements && childElements.length > 0) {
                var range = document.createRange();
                range.setStart(childElements.item(0), 0) // Start range with the first child
                range.setEndAfter(childElements.item(childElements.length - 1), 0) // To include all child nodes, set end of range to node after the last child
                window.getSelection().removeAllRanges(); // clear current selection
                window.getSelection().addRange(range); // to select text
                document.execCommand("copy");
                window.getSelection().removeAllRanges(); // to deselect

                copyButton.classList.add(COPY_CLASSNAME);
                setTimeout(function () { copyButton.classList.remove(COPY_CLASSNAME) }, 2000);
            }
        }
        preElement.appendChild(copyButton);
    });
}