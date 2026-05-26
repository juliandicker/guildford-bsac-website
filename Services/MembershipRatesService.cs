namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    public class MembershipRatesService : IMembershipRatesService
    {
        public IReadOnlyList<MembershipRatesViewModel> MembershipRates { get; }

        public MembershipRatesService(string path) => MembershipRates = Load(path);

        internal MembershipRatesService(List<MembershipRatesViewModel> rates) => MembershipRates = rates;

        private static List<MembershipRatesViewModel> Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required data file not found: {path}");

            return JsonSerializer.Deserialize<List<MembershipRatesViewModel>>(File.ReadAllText(path))
                ?? new List<MembershipRatesViewModel>();
        }

        public MembershipRatesViewModel Current => GetMembershipRatesByActiveDate(DateTime.Now);

        public MembershipRatesViewModel GetMembershipRatesByActiveDate(DateTime date)
        {
            return MembershipRates
                .Where(mr => mr.EffectiveDate < date)
                .MaxBy(mr => mr.EffectiveDate)
                ?? throw new InvalidOperationException(
                    $"No membership rates are effective before {date:yyyy-MM-dd}. " +
                    "Ensure membershiprates.json contains at least one entry with an EffectiveDate in the past.");
        }
    }
}
