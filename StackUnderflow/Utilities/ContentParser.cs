using System.Text.RegularExpressions;
using System.Net;

namespace StackUnderflow.Utilities;

public static class ContentParser
{
    // Replace triple-backtick fenced code blocks with HTML <pre><code> blocks.
    // Detects optional language identifier after the opening fence (e.g. ```csharp).
    // The code inside the fence is HTML-escaped.
    public static string RenderCodeBlocks(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        // Pattern explanation:
        // ``` optionally followed by a language id on the same line, then newline
        // capture everything (including newlines) lazily until the next ```
        var pattern = new Regex(@"```(?:([^\r\n]+)\r?\n)?(.*?)```", RegexOptions.Singleline);

        string Evaluator(Match m)
        {
            var lang = m.Groups[1].Success ? m.Groups[1].Value.Trim() : null;
            var code = m.Groups[2].Value;

            // HTML-encode the code block so it displays literally
            var encoded = WebUtility.HtmlEncode(code);

            if (!string.IsNullOrEmpty(lang))
            {
                // Add a language class so client-side highlighters can use it
                var cls = WebUtility.HtmlEncode(lang);
                return $"<pre><code class=\"language-{cls}\">{encoded}</code></pre>";
            }

            return $"<pre><code>{encoded}</code></pre>";
        }

        var result = new System.Text.StringBuilder();
        var lastIndex = 0;

        foreach (Match match in pattern.Matches(content))
        {
            result.Append(WebUtility.HtmlEncode(content[lastIndex..match.Index]));
            result.Append(Evaluator(match));
            lastIndex = match.Index + match.Length;
        }

        result.Append(WebUtility.HtmlEncode(content[lastIndex..]));
        return result.ToString();
    }
}
