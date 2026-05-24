using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace GuildfordBsac.Web.Models
{
    public class MembershipRatesService
    {
        private string _path = null!;
        private List<MembershipRatesViewModel> _membershipRates = null!;

        public MembershipRatesService(string path)
        {
            _path = path;
        }

        public void Load()
        {
            _membershipRates = File.Exists(_path)
                ? JsonSerializer.Deserialize<List<MembershipRatesViewModel>>(File.ReadAllText(_path)) ?? new List<MembershipRatesViewModel>()
                : new List<MembershipRatesViewModel>();
        }

        public List<MembershipRatesViewModel> MembershipRates
        {
            get
            {
                if (_membershipRates == null) Load();
                return _membershipRates!;
            }
            internal set { _membershipRates = value; }
        }

        public MembershipRatesViewModel Current
        {
            get
            {
                return GetMembershipRatesByActiveDate(DateTime.Now);
            }
        }

        public MembershipRatesViewModel GetMembershipRatesByActiveDate(DateTime date)
        {
            var rates = MembershipRates
                .Where(mr => mr.EffectiveDate < date)
                .OrderByDescending(mr => mr.EffectiveDate)
                .ToList();

            if (rates.Count == 0)
                throw new InvalidOperationException(
                    $"No membership rates are effective before {date:yyyy-MM-dd}. " +
                    "Ensure membershiprates.json contains at least one entry with an EffectiveDate in the past.");

            return rates[0];
        }
    }
}
