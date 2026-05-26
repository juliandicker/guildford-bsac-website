namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System.Collections.Generic;

    public interface ITeamService
    {
        List<TeamMemberViewModel> TeamMembers { get; }
    }
}
