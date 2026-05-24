using System.Text.Json;
using System.Collections.Generic;
using System.IO;

namespace GuildfordBsac.Web.Models
{
    public class TeamService
    {
        private readonly string _path;
        private List<TeamMemberViewModel>? _teamMembers;

        public TeamService(string path)
        {
            _path = path;
        }

        public void Load()
        {
            _teamMembers = File.Exists(_path)
                ? JsonSerializer.Deserialize<List<TeamMemberViewModel>>(File.ReadAllText(_path)) ?? new List<TeamMemberViewModel>()
                : new List<TeamMemberViewModel>();
        }

        public List<TeamMemberViewModel> TeamMembers
        {
            get
            {
                if (_teamMembers == null) Load();
                return _teamMembers!;
            }
        }
    }
}
