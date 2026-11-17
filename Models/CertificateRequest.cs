using System.Text.Json.Serialization;
using GiddhTemplate.Converters;

namespace GiddhTemplate.Models
{
    public class CertificateRequest
    {
        public string name { get; set; } = string.Empty;

        [JsonConverter(typeof(NumberToStringConverter))]
        public string percentage { get; set; } = string.Empty;

        public string completedAt { get; set; } = string.Empty;
        public string validUntil { get; set; } = string.Empty;
        public string? templateType { get; set; }

        public string completedAtDisplay => FormatDisplayDate(completedAt);
        public string validUntilDisplay => FormatDisplayDate(validUntil);

        private static string FormatDisplayDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (DateTime.TryParse(value, out var parsedDate))
            {
                return parsedDate.ToString("dd MMMM yyyy");
            }

            return value;
        }
    }
}
