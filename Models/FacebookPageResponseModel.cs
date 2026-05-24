namespace GuildfordBsac.Web.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class FacebookPageResponseModel
    {
        [JsonPropertyName("data")]
        public List<FacebookPostModel> Data { get; set; } = new List<FacebookPostModel>();
    }
}
