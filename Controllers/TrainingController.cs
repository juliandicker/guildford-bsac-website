namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Models;
    using Microsoft.AspNetCore.Mvc;

    public class TrainingController : Controller
    {
        private readonly MembershipRatesService _membershipRates;

        public TrainingController(MembershipRatesService membershipRates)
        {
            _membershipRates = membershipRates;
        }

        public ActionResult Index()
        {
            return View(_membershipRates.Current);
        }
    }
}
