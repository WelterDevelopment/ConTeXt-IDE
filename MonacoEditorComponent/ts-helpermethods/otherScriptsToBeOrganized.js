///<reference path="../monaco-editor/monaco.d.ts" />
var registerHoverProvider = function (languageId) {
    return monaco.languages.registerHoverProvider(languageId, {
        provideHover: function (model, position) {
            return Parent.callEvent("HoverProvider" + languageId, [JSON.stringify(position)]).then(function (result) {
                if (result) {
                    return JSON.parse(result);
                }
            });
        }
    });
};
var addAction = function (action) {
    action.run = function (ed) {
        Parent.callAction("Action" + action.id);
    };
    editor.addAction(action);
};
var addCommand = function (keybindingStr, handlerName, context) {
    return editor.addCommand(parseInt(keybindingStr), function () {
        Parent.callAction(handlerName);
    }, context);
};
var createContext = function (context) {
    if (context) {
        contexts[context.key] = editor.createContextKey(context.key, context.defaultValue);
    }
};
var updateContext = function (key, value) {
    contexts[key].set(value);
};
var updateContent = function (content) {
    // Need to ignore updates from us notifying of a change
    if (content != model.getValue()) {
        model.setValue(content);
    }
};
var updateDecorations = function (newHighlights) {
    if (newHighlights) {
        decorations = editor.deltaDecorations(decorations, newHighlights);
    }
    else {
        decorations = editor.deltaDecorations(decorations, []);
    }
};
var updateStyle = function (innerStyle) {
    var style = document.getElementById("dynamic");
    style.innerHTML = innerStyle;
};
var getOptions = function () {
    var opt = null;
    try {
        opt = JSON.parse(Parent.getJsonValue("Options"));
    }
    finally {
    }
    if (opt != null && typeof opt === "object") {
        return opt;
    }
    return {};
};
var updateOptions = function (opt) {
    if (opt != null && typeof opt === "object") {
        editor.updateOptions(opt);
    }
};
var updateLanguage = function (language) {
    monaco.editor.setModelLanguage(model, language);
};
var changeTheme = function (theme, highcontrast) {
    var newTheme = 'vs';
    if (highcontrast == "True" || highcontrast == "true") {
        newTheme = 'hc-black';
    }
    else if (theme == "Dark") {
        newTheme = 'vs-dark';
    }
    monaco.editor.setTheme(newTheme);
};
var keyDown = function (event) {
    //Debug.log("Key Down:" + event.keyCode + " " + event.ctrlKey);
    var result = Keyboard.keyDown(event.keyCode, event.ctrlKey, event.shiftKey, event.altKey, event.metaKey);
    if (result) {
        event.cancelBubble = true;
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        return false;
    }
};
//# sourceMappingURL=otherScriptsToBeOrganized.js.map