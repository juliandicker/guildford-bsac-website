namespace GuildfordBsac.Web.Models
{
    using System;
    using System.Globalization;
    using System.Text.Json;
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
        [JsonConverter(typeof(FacebookDateTimeConverter))]
        public DateTimeOffset CreatedTime { get; set; }

        [JsonPropertyName("full_picture")]
        public string? FullPicture { get; set; }

        [JsonPropertyName("permalink_url")]
        public string PermalinkUrl { get; set; } = string.Empty;
    }

    // Facebook returns created_time as "2026-05-01T10:30:00+0000" — the UTC offset
    // uses no colon (+HHmm) which System.Text.Json rejects for both DateTime and
    // DateTimeOffset. Normalise to +HH:mm before parsing.
    internal sealed class FacebookDateTimeConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString() ?? "";
            // +0000 → +00:00  (insert colon if last 5 chars are ±HHMM with no colon)
            if (value.Length >= 5)
            {
                var tail = value[^5..];
                if ((tail[0] == '+' || tail[0] == '-') && tail[1..] is { } digits && digits.Length == 4 && !digits.Contains(':'))
                    value = value[..^2] + ":" + value[^2..];
            }
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("O"));
    }
}
