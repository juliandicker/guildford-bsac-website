namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Models;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using System.IO;

    public class TrainingController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public TrainingController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public ActionResult Index()
        {
            var membershipRatesService = new MembershipRatesService(
                Path.Combine(_env.ContentRootPath, "App_Data", "membershiprates.json"));

            return View(membershipRatesService.Current);
        }
    }
}
