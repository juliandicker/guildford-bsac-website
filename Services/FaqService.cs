namespace GuildfordBsac.Web.Services
{
    using GuildfordBsac.Web.Common;
    using GuildfordBsac.Web.Models;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class FaqService : IFaqService
    {
        private static readonly string[] AllowedFiles = { "faqs.json", "faqsContact.json" };

        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        public FaqService(IWebHostEnvironment env, IMemoryCache cache)
        {
            _env = env;
            _cache = cache;
        }

        public async Task<FaqsViewModel> GetFaqsAsync(string filename)
        {
            if (!AllowedFiles.Contains(filename))
                throw new ArgumentException($"Invalid FAQ file: {filename}", nameof(filename));

            return await _cache.GetOrCreateAsync($"Faqs_{filename}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                var physicalPath = Path.Combine(_env.ContentRootPath, "App_Data", filename);
                var json = await File.ReadAllTextAsync(physicalPath);
                var model = JsonSerializer.Deserialize<FaqsViewModel>(json)!;
                foreach (var faq in model.Faqs)
                    faq.Answer = SharedHtmlSanitizer.Instance.Sanitize(faq.Answer);
                return model;
            }) ?? new FaqsViewModel();
        }
    }
}
