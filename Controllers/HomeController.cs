namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Models;
    using GuildfordBsac.Web.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class HomeController : Controller
    {
        private readonly IFacebookService _facebook;
        private readonly IContactFormService _contactForm;
        private readonly IMembershipRatesService _membershipRates;
        private readonly ITeamService _team;
        private readonly IFaqService _faq;

        public HomeController(IFacebookService facebook, IContactFormService contactForm, IMembershipRatesService membershipRates, ITeamService team, IFaqService faq)
        {
            _facebook = facebook;
            _contactForm = contactForm;
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
            return View(await _faq.GetFaqsAsync(FaqType.General));
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
                Faqs = await _faq.GetFaqsAsync(FaqType.Contact)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("contact")]
        public async Task<JsonResult> Contact(ContactViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var fieldErrors = ViewData.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors = fieldErrors });
            }

            var errors = await _contactForm.SubmitAsync(model, HttpContext, cancellationToken);
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
