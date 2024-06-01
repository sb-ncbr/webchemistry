// init "global" properties

var __mqPatternQueryQueryEditorData = {
    helpChangedCallbacks: { }
};

$(function () {
    ace.config.set("basePath", AceEditorConfig.path);

    var config = ace.require("ace/config");
    var snippetManager = ace.require("ace/snippets").snippetManager;
    ace.config.loadModule("ace/snippets/python", function (m) {
        if (m) {
            m.snippets = snippetManager.parseSnippetFile(m.snippetText);
            snippetManager.unregister(m.snippets, m.scope);

            if (QueriesAutoCompletion) {
                m.snippets = _.pluck(QueriesAutoCompletion, 'Snippet').sort(function (a, b) {
                    var x = a.name, y = b.name;
                    return (x === y ? 0 : (x > y ? -1 : 1));
                });
                snippetManager.register(m.snippets, m.scope);
            }
        }
    });

    var tools = ace.require("ace/ext/language_tools"),
        lastTooltip = null;
    tools.snippetCompleter.getDocTooltip = function (item) {
        var help = QueriesAutoCompletion && QueriesAutoCompletion[item.caption];

        if (lastTooltip !== item && help) {
            _.forEach(__mqPatternQueryQueryEditorData.helpChangedCallbacks, function (c) { c(this); }, help);
        }

        lastTooltip = item;
        if (!item.docHTML && help) {
            item.docHTML = help.Tooltip;
        }
    };
    tools.setCompleters([tools.snippetCompleter]);
});

function PatternQueryQueryEditor(opts) {
    "use strict";

    opts = opts || { };

    var self = this,
        editor = null,
        editorTarget = null,
        callbacks = {},
        lastValue = { value: '' };

    var inputChanged = function (callbacks, lastValue) {
        return function (o, editor) {
            var value = editor.session.getValue();
            if (value === lastValue.value) return;
            lastValue.value = value;
            _.forEach(callbacks['changed'] || [], function (c) { c(this); }, value);
        };
    }(callbacks, lastValue);

    var onFocus = function (o, editor) {
        //editor.gotoLine(1);
        _.forEach(callbacks['focus'] || [], function (c) { c(); });
    };

    var onBlur = function (o, editor) {
        _.forEach(callbacks['blur'] || [], function (c) { c(); });
    };

    function __helpCallback(help) {
        _.forEach(callbacks['help'] || [], function (c) { c(this); }, help);
    }

    this.init = function (target) {
        if (editor !== null) return;

        editorTarget = target;
        editor = ace.edit(target);
        editor.session.setMode("ace/mode/python");
        editor.setTheme("ace/theme/mq_editor");
        editor.setOptions({
            enableBasicAutocompletion: true,
            enableSnippets: true,
            enableLiveAutocompletion: true,
            highlightActiveLine: false,
            maxLines: 5,
            showGutter: true,
            fontSize: opts.fontSize || "14px",
            showPrintMargin: false
        });

        editor.commands.addCommand({
            name: 'ignoreNewline',
            bindKey: { win: 'Ctrl-Enter', mac: 'Ctrl-Enter|Cmd-Enter' },
            exec: function () {
                _.forEach(callbacks['enterPress'] || [], function (c) { c(); });
                return undefined;
            },
            readOnly: true // false if this command should not apply in readOnly mode
        });

        //editor.onPaste = function (text) {
        //    if (this.$readOnly)
        //        return;

        //    if (/[\r\n]/.test(text)) {
        //        text = text.replace(/[\r\n]/g, ' ').replace(/\s+/g, ' ');
        //    }

        //    var e = { text: text };
        //    this._signal("paste", e);
        //    this.insert(e.text, true);
        //};

        editor.on('input', inputChanged);
        editor.on('focus', onFocus);
        editor.on('blur', onBlur);
        __mqPatternQueryQueryEditorData.helpChangedCallbacks[editorTarget] = __helpCallback;

        //editor.commands.addCommand({
        //    name: 'bringHelp',
        //    bindKey: 'Ctrl-H',
        //    exec: function (editor) {
        //        console.log(lastTooltip);
        //        editor.completer.showPopup(editor);
        //        if (editor.completer && lastTooltip) {
        //            editor.completer.updateDocTooltip(lastTooltip);
        //        }
        //    },
        //    readOnly: true 
        //});   
    };

    this.destroy = function () {
        if (editor === null) return;

        delete __mqPatternQueryQueryEditorData.helpChangedCallbacks[editorTarget];
        editor.commands.removeCommand('ignoreNewline');
        editor.off('input', inputChanged);
        editor.off('focus', onFocus);
        editor.off('blur', onBlur);
        callbacks = {};
        lastValue.value = '';
        editor.destroy();
        editor = null;
    };

    this.on = function (event, callback) {
        var xs = callbacks[event];
        if (!xs) {
            xs = [];
            callbacks[event] = xs;
        }
        xs.push(callback || function () { });
    };

    this.off = function (event) {
        delete callbacks[event];
    };

    this.getValue = function () {
        if (editor === null) return undefined;
        return editor.session.getValue();
    };

    this.setValue = function (value) {
        if (editor === null) return;
        var old = editor.session.getValue();
        //if (/[\r\n]/.test(value)) {
        //    value = value.replace(/[\r\n]/g, ' ').replace(/\s+/g, ' ');
        //}
        if (value !== old) {
            editor.session.setValue(value);
        }
    };

    this.insertValue = function (value) {
        if (editor === null || !value) return;
        //if (/[\r\n]/.test(value)) {
        //    value = value.replace(/[\r\n]/g, ' ').replace(/\s+/g, ' ');
        //}
        //var old = editor.session.getValue();
        //console.log(editor.session, value);
        editor.insert(value);
    };

    this.setReadOnly = function (readonly) {
        if (editor === null) return;
        editor.setReadOnly(readonly);
    };

    this.focus = function () {
        if (editor === null) return;
        editor.focus();
    };

    this.blur = function () {
        if (editor === null) return;
        editor.blur();
        if (editor.completer) {
            editor.completer.detach();
        }
    };

    this.closeCompleter = function () {
        if (editor === null) return;
        if (editor.completer) {
            editor.completer.detach();
        }
    };
}