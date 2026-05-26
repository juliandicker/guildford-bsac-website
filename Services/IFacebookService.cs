namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IFacebookService
    {
        Task<List<FacebookPostModel>> GetRecentPostsAsync(int limit = 5, CancellationToken cancellationToken = default);
    }
}
