using System.Threading.Tasks;
using SoloHero.Core.Save;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    public sealed class LocalBackupStore : ISaveStore
    {
        public const string V2Key = "player_data_v2";
        public const string V1Key = "player_data_backup";

        private readonly string _key;

        public LocalBackupStore(string key)
        {
            _key = key;
        }

        public Task<string> LoadJsonAsync()
        {
            string json = PlayerPrefs.GetString(_key, "");
            if (string.IsNullOrEmpty(json)) return Task.FromResult<string>(null);
            return Task.FromResult(json);
        }

        public Task SaveJsonAsync(string json)
        {
            PlayerPrefs.SetString(_key, json);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }
    }
}
