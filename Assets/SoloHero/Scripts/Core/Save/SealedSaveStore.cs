using System;
using System.Threading.Tasks;
using SoloHero.Core.Security;

namespace SoloHero.Core.Save
{
    /// <summary>A store that can tell whether the save it last loaded was the game's own (D-184).</summary>
    public interface ILocalSaveTrust
    {
        LocalSaveTrust LastTrust { get; }
    }

    /// <summary>
    /// D-184: wraps the device's backup store so every write is sealed (<see cref="SaveSeal"/>) and every read reports
    /// whether the seal still matches. SaveService decides what an unsealed or broken copy may do (play on, but not
    /// upload or beat the server's copy).
    /// </summary>
    public sealed class SealedSaveStore : ISaveStore, ILocalSaveTrust
    {
        private readonly ISaveStore _inner;

        public SealedSaveStore(ISaveStore inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public LocalSaveTrust LastTrust { get; private set; } = LocalSaveTrust.Missing;

        public async Task<string> LoadJsonAsync()
        {
            string stored = await _inner.LoadJsonAsync();
            LastTrust = SaveSeal.Open(stored, out string json);
            if (LastTrust == LocalSaveTrust.Broken) TamperGuard.Report("save");
            return json;
        }

        public Task SaveJsonAsync(string json) => _inner.SaveJsonAsync(SaveSeal.Seal(json));
    }
}
