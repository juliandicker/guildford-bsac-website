namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Properties;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using System.Text.Json;
    using Rotativa.AspNetCore;
    using System;

    public class YearPlannerController : Controller
    {
        private static readonly string[] _calendarIds = new string[] {
            "c_eo5g5mq6klkuhoc9us9r7e26fc@group.calendar.google.com",
            "c_1v9tf1n7rali13nvf95ak4lc7k@group.calendar.google.com",
            "c_670f59ed3f5r9fbd4molm2225s@group.calendar.google.com",
            "c_2cedqq8lnc5d016ggctuse8nko@group.calendar.google.com",
            "c_3orvtp2a88fve2ffsca6vd8tqc@group.calendar.google.com"
        };

        private readonly AppSettings _settings;
        private readonly IGoogleApiHelper _googleApi;

        public YearPlannerController(IOptions<AppSettings> settings, IGoogleApiHelper googleApi)
        {
            _settings = settings.Value;
            _googleApi = googleApi;
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
                CalendarData = JsonSerializer.Serialize(_googleApi.GetCalendars(year, _calendarIds))
            };
        }

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
