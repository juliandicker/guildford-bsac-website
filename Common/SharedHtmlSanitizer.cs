namespace GuildfordBsac.Web.Common
{
    using Ganss.Xss;

    // Conservative allow-list: formatting and links only.
    // Uses mutable instance API rather than HtmlSanitizerOptions constructor to ensure
    // AllowedSchemes is enforced for href (UriAttributes must be populated after construction).
    internal static class SharedHtmlSanitizer
    {
        internal static readonly HtmlSanitizer Instance = CreateSanitizer();

        private static HtmlSanitizer CreateSanitizer()
        {
            var s = new HtmlSanitizer();

            s.AllowedTags.Clear();
            foreach (var tag in new[] { "b", "i", "em", "strong", "a", "br", "p", "ul", "ol", "li" })
                s.AllowedTags.Add(tag);

            s.AllowedAttributes.Clear();
            s.AllowedAttributes.Add("href");
            s.AllowedAttributes.Add("target");

            // Restrict href/src to safe schemes only; javascript: and data: are excluded
            s.AllowedSchemes.Clear();
            s.AllowedSchemes.Add("http");
            s.AllowedSchemes.Add("https");

            s.AllowedCssProperties.Clear();

            return s;
        }
    }
}
