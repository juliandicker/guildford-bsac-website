using System;
using System.ComponentModel.DataAnnotations;

namespace GuildfordBsac.Web.Models
{
    public class MembershipRatesViewModel
    {
        public DateTime EffectiveDate { get; set; }
        public DateTime AgmApprovedDate { get; set; }
        public decimal BsacFullMemberAnnualRate { get; set; }
        public decimal ClubJoiningFee { get; set; }
        public decimal DiveCrewCost { get; set; }
        public decimal TryDiveCost { get; set; }
        public decimal ClubJoiningFeeWithTraining { get; set; }
        public decimal ClubMembershipMonthlyRate { get; set; }
        public decimal ClubMembershipAnnualRate
        {
            get
            {
                return ClubMembershipMonthlyRate * 12;
            }
        }
        public decimal OceanDiverTraineeOneYearTotalCost
        {
            get
            {
                return ClubMembershipAnnualRate + ClubJoiningFeeWithTraining + BsacFullMemberAnnualRate;
            }
        }

        public decimal RenewalTotalCost
        {
            get
            {
                return ClubMembershipAnnualRate + BsacFullMemberAnnualRate;
            }
        }

        public decimal DiveCrewTotalCost
        {
            get
            {
                return DiveCrewCost + RenewalTotalCost;
            }
        }
    }
}
