namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Properties;
    using GuildfordBsac.Web.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Options;
    using System.Text.Json;
    using Rotativa.AspNetCore;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class YearPlannerController : Controller
    {
        private readonly AppSettings _settings;
        private readonly ICalendarService _calendar;

        public YearPlannerController(IOptions<AppSettings> settings, ICalendarService calendar)
        {
            _settings = settings.Value;
            _calendar = calendar;
        }

        public async Task<ActionResult> Index(int? year, bool agenda = true, CancellationToken cancellationToken = default)
        {
            return View(await GetModelAsync(year ?? DateTime.Now.Year, agenda, cancellationToken));
        }

        private async Task<YearPlanner2ViewModel> GetModelAsync(int year, bool agenda, CancellationToken cancellationToken)
        {
            return new YearPlanner2ViewModel
            {
                Year = year,
                ShowAgenda = agenda,
                CalendarData = JsonSerializer.Serialize(await _calendar.GetCalendarsAsync(year, cancellationToken))
            };
        }

        [EnableRateLimiting("pdf")]
        public async Task<ActionResult> Pdf(int? year, bool agenda = true, CancellationToken cancellationToken = default)
        {
            return new ViewAsPdf("Index", await GetModelAsync(year ?? DateTime.Now.Year, agenda, cancellationToken))
            {
                CustomSwitches = _settings.WkHtmlPdfCustomSwitches
            };
        }

        [Microsoft.AspNetCore.OutputCaching.OutputCache(Duration = 6000, VaryByQueryKeys = new[] { "year", "agenda" })]
        public async Task<ActionResult> Png(int? year, bool agenda = true, CancellationToken cancellationToken = default)
        {
            return new ViewAsImage("Index", await GetModelAsync(year ?? DateTime.Now.Year, agenda, cancellationToken))
            {
                PageWidth = 2250,
                PageHeight = 1550,
                Format = Rotativa.AspNetCore.Options.ImageFormat.png
            };
        }
    }
}
