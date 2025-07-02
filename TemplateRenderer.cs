using System.Collections.Concurrent;
using System.Text;
using System.Net;

namespace FeatherLite;

public static class TemplateRenderer
{
    private static readonly ConcurrentDictionary<string, TemplateTokens> _templateCache = new();
    private static readonly string _templatesPath = Path.Combine(AppContext.BaseDirectory, "Templates");

    private record TemplateTokens(string[] Parts, string[] Variables);

    public static string RenderTemplate(string templateName, Dictionary<string, string>? variables = null)
    {
        var templateKey = templateName.ToLowerInvariant();

        if (!_templateCache.TryGetValue(templateKey, out var tokens))
        {
            var templatePath = Path.Combine(_templatesPath, $"{templateName}.html");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template not found: {templatePath}");
            }

            var templateContent = File.ReadAllText(templatePath);
            tokens = TokenizeTemplate(templateContent);
            _templateCache[templateKey] = tokens;
        }

        return RenderTokenizedTemplate(tokens, variables);
    }

    private static TemplateTokens TokenizeTemplate(string template)
    {
        var parts = new List<string>();
        var vars = new List<string>();
        var current = 0;

        while (current < template.Length)
        {
            var start = template.IndexOf("{{", current);
            if (start == -1)
            {
                parts.Add(template.Substring(current));
                break;
            }

            parts.Add(template.Substring(current, start - current));

            var end = template.IndexOf("}}", start + 2);
            if (end == -1)
            {
                throw new InvalidOperationException($"Unclosed variable tag starting at position {start}");
            }

            var varName = template.Substring(start + 2, end - start - 2);
            vars.Add(varName);

            current = end + 2;
        }

        return new TemplateTokens(parts.ToArray(), vars.ToArray());
    }

    private static string RenderTokenizedTemplate(TemplateTokens tokens, Dictionary<string, string>? variables)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < tokens.Parts.Length; i++)
        {
            sb.Append(tokens.Parts[i]);

            if (i < tokens.Variables.Length)
            {
                var varName = tokens.Variables[i];
                var rawValue = variables?.GetValueOrDefault(varName) ?? $"{{{{{varName}}}}}";

                var escapedValue = WebUtility.HtmlEncode(rawValue);
                sb.Append(escapedValue);
            }
        }

        return sb.ToString();
    }

    public static void ClearCache()
    {
        _templateCache.Clear();
    }
}
