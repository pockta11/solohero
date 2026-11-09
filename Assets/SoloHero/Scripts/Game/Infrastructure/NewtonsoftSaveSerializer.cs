using System;
using System.Globalization;
using Newtonsoft.Json;
using SoloHero.Core.Save;
using SoloHero.Core.Security;

namespace SoloHero.Game.Infrastructure
{
    public sealed class NewtonsoftSaveSerializer : ISaveSerializer
    {
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            // D-184: the masked value types are written and read as the plain numbers the save always held.
            Converters = { new SecureDoubleConverter(), new SecureIntConverter(), new SecureLongConverter() }
        };

        public string ToJson(SaveDataV2 data)
        {
            return JsonConvert.SerializeObject(data, _settings);
        }

        public SaveDataV2 FromV2Json(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<SaveDataV2>(json, _settings);
        }

        public PlayerDataV1 FromV1Json(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<PlayerDataV1>(json, _settings);
        }

        /// <summary>A number token (integer, float, numeric string) as a double; null and anything else as 0.</summary>
        private static double ReadNumber(JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                    string text = Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
                    if (reader.Value is double d) return d;
                    if (reader.Value is long l) return l;
                    // Anything else (a BigInteger past long, a decimal, a numeric string) through its invariant text.
                    return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ? parsed : 0d;
                default:
                    return 0d;
            }
        }

        /// <summary>An integer token that fits a long (a bigger one falls back to the double path, clamped).</summary>
        private static bool TryWhole(JsonReader reader, out long whole)
        {
            try
            {
                whole = Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                whole = 0L;
                return false;
            }
        }

        private sealed class SecureDoubleConverter : JsonConverter<SecureDouble>
        {
            public override void WriteJson(JsonWriter writer, SecureDouble value, JsonSerializer serializer) => writer.WriteValue(value.Value);

            public override SecureDouble ReadJson(JsonReader reader, Type objectType, SecureDouble existingValue, bool hasExistingValue,
                JsonSerializer serializer) => ReadNumber(reader);
        }

        private sealed class SecureIntConverter : JsonConverter<SecureInt>
        {
            public override void WriteJson(JsonWriter writer, SecureInt value, JsonSerializer serializer) => writer.WriteValue(value.Value);

            public override SecureInt ReadJson(JsonReader reader, Type objectType, SecureInt existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Integer && TryWhole(reader, out long whole))
                    return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, whole));

                double value = ReadNumber(reader);
                return double.IsNaN(value) ? 0 : (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Math.Round(value)));
            }
        }

        private sealed class SecureLongConverter : JsonConverter<SecureLong>
        {
            public override void WriteJson(JsonWriter writer, SecureLong value, JsonSerializer serializer) => writer.WriteValue(value.Value);

            public override SecureLong ReadJson(JsonReader reader, Type objectType, SecureLong existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Integer && TryWhole(reader, out long whole)) return whole;
                double value = ReadNumber(reader);
                return double.IsNaN(value) ? 0L : (long)Math.Max(long.MinValue, Math.Min(long.MaxValue, Math.Round(value)));
            }
        }
    }
}
