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

        public void Save()
        {
            var json = JsonSerializer.Serialize(MembershipRates);
            File.WriteAllText(_path, json);
        }

        public List<MembershipRatesViewModel> MembershipRates
        {
            get
            {
                if (_membershipRates == null) Load();
                return _membershipRates!;
            }

            set { _membershipRates = value; }
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
            return MembershipRates
                .Where(mr => mr.EffectiveDate < date)
                .OrderByDescending(mr => mr.EffectiveDate)
                .First();
        }
    }
}
