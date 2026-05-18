namespace GuildfordBsac.Web.Models
{
    using Newtonsoft.Json;
    using System;

    public class FacebookPostModel
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("story")]
        public string? Story { get; set; }

        [JsonProperty("created_time")]
        public DateTime CreatedTime { get; set; }

        [JsonProperty("full_picture")]
        public string? FullPicture { get; set; }

        [JsonProperty("permalink_url")]
        public string PermalinkUrl { get; set; } = string.Empty;
    }
}
