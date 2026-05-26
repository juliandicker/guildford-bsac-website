namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Models;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class FaqService : IFaqService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FaqService> _logger;

        public FaqService(IWebHostEnvironment env, IMemoryCache cache, ILogger<FaqService> logger)
        {
            _env = env;
            _cache = cache;
            _logger = logger;
        }

        public async Task<FaqsViewModel> GetFaqsAsync(FaqType faqType)
        {
            var filename = faqType == FaqType.Contact ? "faqsContact.json" : "faqs.json";

            return await _cache.GetOrCreateAsync($"Faqs_{filename}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                try
                {
                    var physicalPath = Path.Combine(_env.ContentRootPath, "App_Data", filename);
                    var json = await File.ReadAllTextAsync(physicalPath);
                    var model = JsonSerializer.Deserialize<FaqsViewModel>(json)!;
                    foreach (var faq in model.Faqs)
                        faq.Answer = SharedHtmlSanitizer.Instance.Sanitize(faq.Answer);
                    return model;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load FAQ file {Filename}", filename);
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    return new FaqsViewModel();
                }
            }) ?? new FaqsViewModel();
        }
    }
}
