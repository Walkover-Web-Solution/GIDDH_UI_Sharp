using System.Text.Json;
using System.Text.Json.Serialization;

namespace GiddhTemplate.Converters
{
    public class NumberToStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetDecimal(out var value))
                {
                    return value.ToString();
                }

                if (reader.TryGetInt64(out var longValue))
                {
                    return longValue.ToString();
                }

                return reader.GetDouble().ToString();
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            throw new JsonException($"Unable to convert token type '{reader.TokenType}' to string.");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
