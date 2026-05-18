namespace GuildfordBsac.Web.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class FacebookPageResponseModel
    {
        [JsonProperty("data")]
        public List<FacebookPostModel> Data { get; set; } = new List<FacebookPostModel>();
    }
}
