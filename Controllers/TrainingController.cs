namespace GuildfordBsac.Web.Controllers
{
    using GuildfordBsac.Web.Services;
    using Microsoft.AspNetCore.Mvc;

    public class TrainingController : Controller
    {
        private readonly IMembershipRatesService _membershipRates;

        public TrainingController(IMembershipRatesService membershipRates)
        {
            _membershipRates = membershipRates;
        }

        public ActionResult Index()
        {
            return View(_membershipRates.Current);
        }
    }
}
