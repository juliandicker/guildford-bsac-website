using GuildfordBsac.Web.Models;
using GuildfordBsac.Web.Services;

namespace GuildfordBsac.Web.Tests;

public class MembershipRatesServiceTests
{
    private static MembershipRatesService MakeService(params (DateTime effectiveDate, decimal monthlyRate)[] rates)
    {
        return new MembershipRatesService(rates
            .Select(r => new MembershipRatesViewModel { EffectiveDate = r.effectiveDate, ClubMembershipMonthlyRate = r.monthlyRate })
            .ToList());
    }

    [Fact]
    public void ReturnsRateActiveOnDate()
    {
        var service = MakeService(
            (new DateTime(2022, 3, 1), 15.5m),
            (new DateTime(2023, 3, 1), 16.5m),
            (new DateTime(2024, 3, 1), 17.5m));

        var rate = service.GetMembershipRatesByActiveDate(new DateTime(2023, 6, 1));

        Assert.Equal(16.5m, rate.ClubMembershipMonthlyRate);
    }

    [Fact]
    public void ReturnsLatestRateWhenMultiplePrecede()
    {
        var service = MakeService(
            (new DateTime(2022, 3, 1), 15.5m),
            (new DateTime(2023, 3, 1), 16.5m),
            (new DateTime(2024, 3, 1), 17.5m));

        var rate = service.GetMembershipRatesByActiveDate(new DateTime(2025, 1, 1));

        Assert.Equal(17.5m, rate.ClubMembershipMonthlyRate);
    }

    [Fact]
    public void ExactEffectiveDateExcluded_ReturnsPreviousRate()
    {
        // EffectiveDate < date (strict less-than), so querying on the exact date returns the prior rate
        var service = MakeService(
            (new DateTime(2022, 3, 1), 15.5m),
            (new DateTime(2023, 3, 1), 16.5m));

        var rate = service.GetMembershipRatesByActiveDate(new DateTime(2023, 3, 1));

        Assert.Equal(15.5m, rate.ClubMembershipMonthlyRate);
    }
}

public class MembershipRatesViewModelTests
{
    // Uses 2024 rates from App_Data/membershiprates.json as fixture values
    private static MembershipRatesViewModel Rate2024() => new()
    {
        BsacFullMemberAnnualRate = 68.5m,
        ClubJoiningFeeWithTraining = 75.0m,
        DiveCrewCost = 519.0m,
        ClubMembershipMonthlyRate = 17.5m
    };

    [Fact]
    public void AnnualRate_IsMonthlyRateTimesTwelve()
    {
        Assert.Equal(210.0m, Rate2024().ClubMembershipAnnualRate);
    }

    [Fact]
    public void RenewalTotalCost_IsAnnualPlusBsac()
    {
        Assert.Equal(278.5m, Rate2024().RenewalTotalCost);
    }

    [Fact]
    public void OceanDiverTotalCost_IsAnnualPlusJoiningFeePlusBsac()
    {
        Assert.Equal(353.5m, Rate2024().OceanDiverTraineeOneYearTotalCost);
    }

    [Fact]
    public void DiveCrewTotalCost_IsDiveCrewCostPlusRenewal()
    {
        Assert.Equal(797.5m, Rate2024().DiveCrewTotalCost);
    }
}
