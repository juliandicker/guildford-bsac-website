namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System.Threading.Tasks;

    public interface IFaqService
    {
        Task<FaqsViewModel> GetFaqsAsync(string filename);
    }
}
