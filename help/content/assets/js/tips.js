var currentTip = null;
var currentTipElement = null;

function hideTip(evt, name, unique) {
    var el = document.getElementById(name);
    el.style.display = "none";
    currentTip = null;
}

function findPos(obj) {
    // no idea why, but it behaves differently in webbrowser component
    if (window.location.search == "?inapp")
        return [obj.offsetLeft + 10, obj.offsetTop + 30];

    var curleft = 0;
    var curtop = obj.offsetHeight;
    while (obj) {
        curleft += obj.offsetLeft;
        curtop += obj.offsetTop;
        obj = obj.offsetParent;
    };
    return [curleft, curtop];
}

function hideUsingEsc(e) {
    if (!e) { e = event; }
    hideTip(e, currentTipElement, currentTip);
}

function showTip(evt, name, unique, owner) {
    document.onkeydown = hideUsingEsc;
    if (currentTip == unique) return;
    currentTip = unique;
    currentTipElement = name;

    var el = document.getElementById(name);
    var parent = (document.documentElement == null) ? document.body : document.documentElement;
    el.style.position = "absolute";
    el.style.display = "block";
}