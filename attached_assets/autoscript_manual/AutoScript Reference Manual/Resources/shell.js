// Event fires when the body of the shell fully loads. 
function onBodyLoaded(frameName, navTreeId) {
    addScrollToTopToNavTree(navTreeId);
    setFrameTarget(frameName);
}

// Adds scroll to top action to all links in nav tree menu.
function addScrollToTopToNavTree(navTreeId) {
    var links = window.document.getElementById(navTreeId).getElementsByTagName("a");

    for (var index = 0; index < links.length; index++) {
        links[index].onclick = function () { window.scroll({ top: 0, behavior: "smooth" }) };
    }
}

// Sets frame location based on the url parameter 'target'.
function setFrameTarget(frameName) {
    var frame = getFrame(frameName);
    var target = findGetParameter("target");

    if (frame && target) {
        frame.location = target;
    }
}

// Gets frame by its name.
function getFrame(frameName) {
    return window.frames[frameName];
}

// Parses get parameter from the url.
function findGetParameter(parameterName) {
    var result = null;
    var items = location.search.substr(1).split("&");
    for (var index = 0; index < items.length; index++) {
        window.tmp = items[index].split("=");
        if (window.tmp[0] === parameterName) result = decodeURIComponent(window.tmp[1]);
    }
    return result;
}

// Event fires when the iframe fully loads.
function onFrameLoaded(frame, navtreeId) {
    hookFrameChange(frame, navtreeId);
}

// Last visited frame location.
var lastLocation;

// Hooks message event that fires when the frame changes.
function hookFrameChange(frame, navtreeId) {
    if (frame) {
        hookMessageEvent(function (messageArg) {
            var parsedObject = JSON.parse(messageArg.data);
            resizeFrame(frame, parseInt(parsedObject.height));

            lastLocation = parsedObject.location;
            expandTreeMenuToShowCurrent(parsedObject.location, navtreeId);
        });
    }
}

// Resizes the frame based on the information from the message argument.
function resizeFrame(frame, height) {
    var heightWithMargin = height + 60; // margin to be sure
    frame.style.height = heightWithMargin + "px";
}

// Expands tree menu to show current page.
function expandTreeMenuToShowCurrent(currentPage, navtreeId) {
    var link = findNavTreeLinkByHref(currentPage, navtreeId);
    expandLink(link);
}

// Finds the first link in navtree based on its href. If there are no links, returns null. 
function findNavTreeLinkByHref(href, navtreeId) {
    var replacedHref = href.replace(" ", "%20");
    var links = window.document.getElementById(navtreeId).getElementsByTagName("a");

    for (var i = 0, l = links.length; i < l; i++) {
        if (links[i].href === href || links[i].href === replacedHref) {
            return links[i];
        }
    }

    return null;
}

// Expands tree menu from link element.
function expandLink(link) {
    if (link) {
        // if the link has label as its parent, we need to go one layer higher
        if (link.parentElement && link.parentElement.tagName.toLowerCase() === "label") {
            expandTreeNode(link.parentElement.parentElement);
        } else {
            expandTreeNode(link.parentElement);
        }
    }
}

// Recursive function that expands and shows all related tree node elements. Always has to start on the li element.
function expandTreeNode(parent) {
    if (parent && parent.tagName.toLowerCase() === "li") {
        showElement(parent);
        var input = parent.getElementsByTagName("input")[0];

        if (input) {
            input.checked = true;
        }

        var grandParent = parent.parentElement;

        if (grandParent && grandParent.tagName.toLowerCase() === "ul") {
            expandTreeNode(grandParent.parentElement);
        }
    }
}

// Sends information about window resize to the iframe.
// Information must be sent as a message to avoid cross origin frame.
function sendResizeInfo(frameName) {
    var frame = getFrame(frameName);

    if (frame && frame.postMessage) {
        frame.postMessage("WindowResized", "*");
    }
}

// Filters menu based on filterInputId text box content. If the filter phrase is null or empty, 
// shows all menu items and expands the current.
function filterMenu(filterInputId, navtreeId) {
    hideAllMenuItems(navtreeId);
    condenseAllMenuItems(navtreeId);

    var filterPhrase = window.document.getElementById(filterInputId).value.toLowerCase();

    if (filterPhrase) {
        var links = window.document.getElementById(navtreeId).getElementsByTagName("a");

        for (var i = 0, l = links.length; i < l; i++) {
            var textValue = links[i].textContent || links[i].innerText;

            if (textValue.toLowerCase().indexOf(filterPhrase) >= 0) {
                expandLink(links[i]);
            }
        }
    } else {
        showAllMenuItems(navtreeId);

        if (lastLocation) {
            expandTreeMenuToShowCurrent(lastLocation, navtreeId);
        }
    }
}

// Hides all list elements in menu.
function hideAllMenuItems(navtreeId) {
    var listItems = window.document.getElementById(navtreeId).getElementsByTagName("li");

    for (var i = 0, l = listItems.length; i < l; i++) {
        hideElement(listItems[i]);
    }
}

// Hides given element (sets display to none).
function hideElement(element) {
    element.style.display = "none";
}

// Shows all list elements in menu.
function showAllMenuItems(navtreeId) {
    var listItems = window.document.getElementById(navtreeId).getElementsByTagName("li");

    for (var i = 0, l = listItems.length; i < l; i++) {
        showElement(listItems[i]);
    }
}

// Shows given element (sets display to empty string).
function showElement(element) {
    element.style.display = "";
}

// Condenses (retracts) all menu items.
function condenseAllMenuItems(navtreeId) {
    var inputItems = window.document.getElementById(navtreeId).getElementsByTagName("input");

    for (var i = 0, l = inputItems.length; i < l; i++) {
        inputItems[i].checked = false;
    }
}