using Newtonsoft.Json;
using SoloHero.Core.Save;

namespace SoloHero.Game.Infrastructure
{
    public sealed class NewtonsoftSaveSerializer : ISaveSerializer
    {
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
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
    }
}
