var snippets = document.querySelectorAll('.snippet, pre[class*="language-"]');

[].forEach.call(snippets, function (snippet) {
    snippet.firstChild.insertAdjacentHTML('beforebegin', '<a class="btn" data-clipboard-snippet><i class="far fa-copy"></i></a>');
});
var clipboardSnippets = new ClipboardJS('[data-clipboard-snippet]', {
    target: function (trigger) {
        return trigger.nextElementSibling;
    }
});
clipboardSnippets.on('success', function (e) {
    e.clearSelection();
});


Prism.plugins.toolbar.registerButton('copy-to-clipboard', function (env) {
    var link = document.createElement('div');
    link.innerHTML = '<a class="btn"><i class="far fa-copy"></i></a>'

    if (!ClipboardJS) {
        callbacks.push(registerClipboard);
    } else {
        registerClipboard();
    }

    return link;

    function registerClipboard() {
        var clip = new ClipboardJS(link, {
            'text': function () {
                return env.code;
            }
        });
    }

});
