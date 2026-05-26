namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;

    public class TeamService : ITeamService
    {
        public IReadOnlyList<TeamMemberViewModel> TeamMembers { get; }

        public TeamService(string path) => TeamMembers = Load(path);

        private static IReadOnlyList<TeamMemberViewModel> Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required data file not found: {path}");

            return JsonSerializer.Deserialize<List<TeamMemberViewModel>>(File.ReadAllText(path))
                ?? new List<TeamMemberViewModel>();
        }
    }
}
