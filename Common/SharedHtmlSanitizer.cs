namespace GuildfordBsac.Web.Common
{
    using Ganss.Xss;
    using System.Collections.Generic;

    // Conservative allow-list: formatting and links only — no class/style/id attributes to prevent CSS injection
    internal static class SharedHtmlSanitizer
    {
        internal static readonly HtmlSanitizer Instance = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string> { "b", "i", "em", "strong", "a", "br", "p", "ul", "ol", "li" },
            AllowedAttributes = new HashSet<string> { "href", "target" },
            AllowedSchemes = new HashSet<string> { "http", "https" }
        });
    }
}
