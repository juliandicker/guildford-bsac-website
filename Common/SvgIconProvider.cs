namespace GuildfordBsac.Web.Common
{
    using Microsoft.AspNetCore.Hosting;
    using System.IO;

    public interface ISvgIconProvider
    {
        string ChevronLeft { get; }
        string ChevronRight { get; }
        string PrinterFill { get; }
    }

    public class SvgIconProvider : ISvgIconProvider
    {
        public string ChevronLeft { get; }
        public string ChevronRight { get; }
        public string PrinterFill { get; }

        public SvgIconProvider(IWebHostEnvironment env)
        {
            var iconBase = Path.Combine(env.WebRootPath, "Content", "Images", "icons");
            ChevronLeft = File.ReadAllText(Path.Combine(iconBase, "chevron-left.svg"));
            ChevronRight = File.ReadAllText(Path.Combine(iconBase, "chevron-right.svg"));
            PrinterFill = File.ReadAllText(Path.Combine(iconBase, "printer-fill.svg"));
        }
    }
}
