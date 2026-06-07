// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Highlight all code blocks on the page. highlightAll will handle <pre><code> blocks
hljs.highlightAll();

// Additionally, ensure any code elements inside post-content are highlighted (defensive)
document.querySelectorAll('div.post-content pre code').forEach((block) => {
    try { hljs.highlightElement(block); } catch (e) { /* ignore */ }
});

// -----------------------------
// Live preview for answer textarea
// -----------------------------
function htmlEncode(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;')
              .replace(/</g, '&lt;')
              .replace(/>/g, '&gt;')
              .replace(/"/g, '&quot;')
              .replace(/'/g, '&#39;');
}

function renderCodeFencePreview(text) {
    if (text == null) return '';

    const fenceRegex = /```(?:([^\r\n]+)\r?\n)?([\s\S]*?)```/g;

    let lastIndex = 0;
    let out = '';
    let m;

    while ((m = fenceRegex.exec(text)) !== null) {
        // Append the plain text before this match, HTML-encoded
        const before = text.slice(lastIndex, m.index);
        out += htmlEncode(before);

        const lang = m[1] ? m[1].trim() : null;
        const code = m[2] || '';
        const encodedCode = htmlEncode(code);

        if (lang) {
            const cls = htmlEncode(lang);
            out += `<pre><code class="language-${cls}">${encodedCode}</code></pre>`;
        } else {
            out += `<pre><code>${encodedCode}</code></pre>`;
        }

        lastIndex = fenceRegex.lastIndex;
    }

    // Append remaining tail
    out += htmlEncode(text.slice(lastIndex));

    return out;
}

// Debounce helper
function debounce(fn, wait) {
    let t = null;
    return function(...args) {
        clearTimeout(t);
        t = setTimeout(() => fn.apply(this, args), wait);
    };
}

function attachLivePreview(textareaSelector, previewSelector) {
    const ta = document.querySelector(textareaSelector);
    const preview = document.querySelector(previewSelector);
    if (!ta || !preview) return;

    const update = () => {
        const rendered = renderCodeFencePreview(ta.value);
        preview.innerHTML = rendered;

        // highlight any code inside the preview
        preview.querySelectorAll('pre code').forEach((block) => {
            try { hljs.highlightElement(block); } catch (e) { /* ignore */ }
        });
    };

    const debounced = debounce(update, 150);
    ta.addEventListener('input', debounced);

    // initialize preview with current value (if any)
    update();
}

// Attach to the answer textarea preview
document.addEventListener('DOMContentLoaded', function() {
    attachLivePreview('#answer-content', '#answer-preview');
});

