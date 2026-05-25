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
            if (!File.Exists(_path))
                throw new FileNotFoundException($"Required data file not found: {_path}");

            _teamMembers = JsonSerializer.Deserialize<List<TeamMemberViewModel>>(File.ReadAllText(_path))
                ?? new List<TeamMemberViewModel>();
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
