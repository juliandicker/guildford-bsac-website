namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Configuration;
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Options;
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Rotativa.AspNetCore;

    public class YearPlannerController : Controller
    {
        private const int PngPageWidthPixels = 2250;
        private const int PngPageHeightPixels = 1550;

        private readonly AppSettings _settings;
        private readonly ICalendarService _calendar;
        private readonly PngRenderLock _pngLock;

        public YearPlannerController(IOptions<AppSettings> settings, ICalendarService calendar, PngRenderLock pngLock)
        {
            _settings = settings.Value;
            _calendar = calendar;
            _pngLock = pngLock;
        }

        [EnableRateLimiting("yearplanner")]
        public async Task<ActionResult> Index(int? year, bool agenda = true, CancellationToken cancellationToken = default)
        {
            return View(await GetModelAsync(ClampYear(year), agenda, cancellationToken));
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
            return new ViewAsPdf("Index", await GetModelAsync(ClampYear(year), agenda, cancellationToken))
            {
                CustomSwitches = _settings.WkHtmlPdfCustomSwitches
            };
        }

        [EnableRateLimiting("yearplanner")]
        [Microsoft.AspNetCore.OutputCaching.OutputCache(Duration = 6000, VaryByQueryKeys = new[] { "year", "agenda" })]
        public async Task<IActionResult> Png(int? year, bool agenda = true, CancellationToken cancellationToken = default)
        {
            var viewAsImage = new ViewAsImage("Index", await GetModelAsync(ClampYear(year), agenda, cancellationToken))
            {
                PageWidth = PngPageWidthPixels,
                PageHeight = PngPageHeightPixels,
                Format = Rotativa.AspNetCore.Options.ImageFormat.png
            };
            return new LockedViewAsImage(viewAsImage, _pngLock);
        }

        private static int ClampYear(int? year)
        {
            var current = DateTime.Now.Year;
            var requested = year ?? current;
            return Math.Clamp(requested, current - 5, current + 1);
        }

        // Wraps ViewAsImage so the MVC pipeline executes it, while still serialising
        // concurrent Rotativa subprocess invocations behind PngRenderLock.
        private sealed class LockedViewAsImage : IActionResult
        {
            private readonly ViewAsImage _inner;
            private readonly PngRenderLock _lock;

            public LockedViewAsImage(ViewAsImage inner, PngRenderLock @lock)
            {
                _inner = inner;
                _lock = @lock;
            }

            public async Task ExecuteResultAsync(ActionContext context)
            {
                await _lock.WaitAsync(context.HttpContext.RequestAborted);
                try
                {
                    await _inner.ExecuteResultAsync(context);
                }
                finally
                {
                    _lock.Release();
                }
            }
        }
    }
}
