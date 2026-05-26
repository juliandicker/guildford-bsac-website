namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class HomeController : Controller
    {
        private readonly IFacebookService _facebook;
        private readonly ILogger<HomeController> _logger;
        private readonly IReCaptchaValidator _captcha;
        private readonly IGoogleApiHelper _googleApi;
        private readonly IMembershipRatesService _membershipRates;
        private readonly ITeamService _team;
        private readonly IFaqService _faq;

        public HomeController(IFacebookService facebook, ILogger<HomeController> logger, IReCaptchaValidator captcha, IGoogleApiHelper googleApi, IMembershipRatesService membershipRates, ITeamService team, IFaqService faq)
        {
            _facebook = facebook;
            _logger = logger;
            _captcha = captcha;
            _googleApi = googleApi;
            _membershipRates = membershipRates;
            _team = team;
            _faq = faq;
        }

        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var model = new HomeViewModel
            {
                MembershipRates = _membershipRates.Current,
                TeamMembers = _team.TeamMembers,
                RecentPosts = await _facebook.GetRecentPostsAsync(limit: 5, cancellationToken)
            };

            return View(model);
        }

        public async Task<ActionResult> Faqs()
        {
            return View(await _faq.GetFaqsAsync("faqs.json"));
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult PrivacyPolicy()
        {
            return View();
        }

        public ActionResult CodeOfConduct()
        {
            return View();
        }

        public async Task<ActionResult> ContactUs()
        {
            var model = new ContactUsViewModel
            {
                Contact = new ContactViewModel(),
                Faqs = await _faq.GetFaqsAsync("faqsContact.json")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("contact")]
        public async Task<JsonResult> Contact(ContactViewModel model, CancellationToken cancellationToken)
        {
            var reCaptchaResponse = await _captcha.ValidateAsync(HttpContext, cancellationToken);

            if (!reCaptchaResponse.Success)
            {
                ViewData.ModelState.AddModelError("reCAPTCHA", "You failed to pass the captcha test");
            }

            var errors = new List<string>();

            if (!string.IsNullOrEmpty(model.Emailx))
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
                    if (!await _googleApi.SendMessageAsync(model.Name!, model.Emaily!, model.Subject!, model.Message!, cancellationToken))
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult Error(int? statusCode = null)
        {
            Response.StatusCode = statusCode ?? 500;
            return View("Error", statusCode?.ToString());
        }

    }
}
