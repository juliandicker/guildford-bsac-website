using System.Collections.Generic;

namespace GuildfordBsac.Web.Models
{
    public class HomeViewModel
    {
        public MembershipRatesViewModel MembershipRates { get; set; } = null!;
        public List<FacebookPostModel> RecentPosts { get; set; } = new List<FacebookPostModel>();
        public List<TeamMemberViewModel> TeamMembers { get; set; } = new List<TeamMemberViewModel>();
    }
}
