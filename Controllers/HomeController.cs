namespace GuildfordBsac.Web.Controllers
{
    using Ganss.Xss;
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Properties;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class HomeController : Controller
    {
        private static readonly string[] AllowedFaqFiles = { "faqs.json", "faqsContact.json" };
        private static readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

        private readonly FacebookService _facebook;
        private readonly IWebHostEnvironment _env;
        private readonly AppSettings _settings;
        private readonly ILogger<HomeController> _logger;

        public HomeController(FacebookService facebook, IWebHostEnvironment env, IOptions<AppSettings> settings, ILogger<HomeController> logger)
        {
            _facebook = facebook;
            _env = env;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ActionResult> Index()
        {
            var membershipRatesService = new MembershipRatesService(
                Path.Combine(_env.ContentRootPath, "App_Data", "membershiprates.json"));

            var teamService = new TeamService(
                Path.Combine(_env.ContentRootPath, "App_Data", "team.json"));

            var model = new HomeViewModel();
            model.MembershipRates = membershipRatesService.Current;
            model.TeamMembers = teamService.TeamMembers;
            model.RecentPosts = await _facebook.GetRecentPostsAsync("1027783460591236", limit: 5);

            return View(model);
        }

        public ActionResult Faqs()
        {
            var model = LoadFaqModel("faqs.json");
            return View(model);
        }

        public ActionResult PrivacyPolicy()
        {
            return View();
        }

        public ActionResult CodeOfConduct()
        {
            return View();
        }

        public ActionResult ContactUs()
        {
            var model = new ContactUsViewModel
            {
                Contact = new ContactViewModel(),
                Faqs = LoadFaqModel("faqsContact.json")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Contact(ContactViewModel model)
        {
            var reCaptchaResponse = await ReCaptcha.ValidateAsync(HttpContext, _settings.RecaptchaSecret);

            if (!reCaptchaResponse.Success)
            {
                ViewData.ModelState.AddModelError("reCAPTCHA", "You failed to pass the captcha test");
            }

            var errors = new List<string>();

            if (!String.IsNullOrEmpty(model.Emailx))
            {
                errors.Add("There has been a technical error sending this message. Please contact by telephone.");
            }
            else if (!ModelState.IsValid)
            {
                foreach (var modelState in ViewData.ModelState.Values)
                {
                    errors.AddRange(modelState.Errors.Select(error => error.ErrorMessage));
                }
            }
            else
            {
                try
                {
                    var gHelper = new GoogleApiHelper(
                        _settings.ServiceAccount.ClientEmail,
                        _settings.ServiceAccount.PrivateKey,
                        _settings.ContactEmail,
                        _settings.ContactEmailBcc);

                    if (!gHelper.SendMessage(model.Name!, model.Emaily!, model.Subject!, model.Message!))
                    {
                        errors.Add("There has been a technical error sending this message. Please contact by telephone.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send contact form email");
                    errors.Add("There has been a technical error sending this message. Please contact by telephone.");
                }
            }

            return Json(new { success = errors.Count == 0, errors });
        }

        private FaqsViewModel LoadFaqModel(string filename)
        {
            if (!AllowedFaqFiles.Contains(filename))
                throw new ArgumentException($"Invalid FAQ file: {filename}");

            var physicalPath = Path.Combine(_env.ContentRootPath, "App_Data", filename);
            var json = System.IO.File.ReadAllText(physicalPath);
            var model = JsonConvert.DeserializeObject<FaqsViewModel>(json)!;

            foreach (var faq in model.Faqs)
                faq.Answer = _sanitizer.Sanitize(faq.Answer);

            return model;
        }
    }
}
