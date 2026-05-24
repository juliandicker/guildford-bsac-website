namespace GuildfordBsac.Web.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class FacebookPostModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("story")]
        public string? Story { get; set; }

        [JsonPropertyName("created_time")]
        public DateTime CreatedTime { get; set; }

        [JsonPropertyName("full_picture")]
        public string? FullPicture { get; set; }

        [JsonPropertyName("permalink_url")]
        public string PermalinkUrl { get; set; } = string.Empty;
    }
}
