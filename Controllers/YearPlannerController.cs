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

    public class YearPlannerController : Controller
    {
        private readonly AppSettings _settings;
        private readonly ICalendarService _calendar;

        public YearPlannerController(IOptions<AppSettings> settings, ICalendarService calendar)
        {
            _settings = settings.Value;
            _calendar = calendar;
        }

        public ActionResult Index(int? year, bool agenda = true)
        {
            return View(GetModel(year ?? DateTime.Now.Year, agenda));
        }

        private YearPlanner2ViewModel GetModel(int year, bool agenda = true)
        {
            return new YearPlanner2ViewModel
            {
                Year = year,
                ShowAgenda = agenda,
                CalendarData = JsonSerializer.Serialize(_calendar.GetCalendars(year))
            };
        }

        [EnableRateLimiting("pdf")]
        public ActionResult Pdf(int? year, bool agenda = true)
        {
            return new ViewAsPdf("Index", GetModel(year ?? DateTime.Now.Year, agenda))
            {
                CustomSwitches = _settings.WkHtmlPdf_CustomSwitches
            };
        }

        [Microsoft.AspNetCore.OutputCaching.OutputCache(Duration = 6000, VaryByQueryKeys = new[] { "year", "agenda" })]
        public ActionResult Png(int? year, bool agenda = true)
        {
            return new ViewAsImage("Index", GetModel(year ?? DateTime.Now.Year, agenda))
            {
                PageWidth = 2250,
                PageHeight = 1550,
                Format = Rotativa.AspNetCore.Options.ImageFormat.png
            };
        }
    }
}
