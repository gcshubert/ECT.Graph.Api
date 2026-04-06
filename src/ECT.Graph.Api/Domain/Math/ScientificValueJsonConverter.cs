using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECT.Graph.Api.Domain.Math;

public sealed class ScientificValueJsonConverter : JsonConverter<ScientificValue>
{
    public override ScientificValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetDouble();
            return new ScientificValue(value);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (double.TryParse(text, out var numericValue))
                return new ScientificValue(numericValue);

            throw new JsonException($"Invalid ScientificValue string: '{text}'");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            double coeff = 0;
            double exp = 0;
            bool hasCoeff = false;
            bool hasExp = false;

            if (root.TryGetProperty("Coefficient", out var coeffProp) || root.TryGetProperty("coefficient", out coeffProp))
            {
                coeff = coeffProp.GetDouble();
                hasCoeff = true;
            }

            if (root.TryGetProperty("Exponent", out var expProp) || root.TryGetProperty("exponent", out expProp))
            {
                exp = expProp.GetDouble();
                hasExp = true;
            }

            if (hasCoeff && hasExp)
            {
                return new ScientificValue(coeff, exp);
            }

            if (root.TryGetProperty("Value", out var valueProp) || root.TryGetProperty("value", out valueProp))
            {
                var value = valueProp.GetDouble();
                return new ScientificValue(value);
            }

            throw new JsonException("ScientificValue object must contain either (Coefficient and Exponent) or Value.");
        }

        throw new JsonException($"Unsupported token type for ScientificValue: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, ScientificValue value, JsonSerializerOptions options)
    {
        // Preserve coefficient/exponent in JSON so clients can roundtrip scientific notation without precision loss.
        writer.WriteStartObject();
        writer.WriteNumber("coefficient", value.Coefficient);
        writer.WriteNumber("exponent", value.Exponent);
        writer.WriteEndObject();
    }
}
