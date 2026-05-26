namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System;
    using System.Collections.Generic;

    public interface IMembershipRatesService
    {
        IReadOnlyList<MembershipRatesViewModel> MembershipRates { get; }
        MembershipRatesViewModel Current { get; }
        MembershipRatesViewModel GetMembershipRatesByActiveDate(DateTime date);
    }
}
