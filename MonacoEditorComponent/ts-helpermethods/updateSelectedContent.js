var updateSelectedContent = function (content) {
    var selection = editor.getSelection();
    if (content != model.getValueInRange(selection)) {
        modifingSelection = true;
        var range = new monaco.Range(selection.startLineNumber, selection.startColumn, selection.endLineNumber, selection.endColumn);
        var op = { identifier: { major: 1, minor: 1 }, range: range, text: content, forceMoveMarkers: true };
        model.pushEditOperations([], [op], null);
        var newEndLineNumber = selection.startLineNumber + content.split('\r').length - 1;
        var newEndColumn = (selection.startLineNumber === selection.endLineNumber)
            ? selection.startColumn + content.length
            : content.length - content.lastIndexOf('\r');
        selection = selection.setEndPosition(newEndLineNumber, newEndColumn);
        selection = selection.setEndPosition(selection.endLineNumber, selection.endColumn);
        modifingSelection = false;
        editor.setSelection(selection);
    }
};
