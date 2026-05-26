namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Models;
    using System.Threading.Tasks;

    public enum FaqType { General, Contact }

    public interface IFaqService
    {
        Task<FaqsViewModel> GetFaqsAsync(FaqType faqType);
    }
}
