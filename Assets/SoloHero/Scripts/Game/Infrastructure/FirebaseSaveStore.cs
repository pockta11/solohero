using System.Threading.Tasks;
using Firebase.Database;
using SoloHero.Core.Save;

namespace SoloHero.Game.Infrastructure
{
    public sealed class FirebaseSaveStore : ISaveStore
    {
        private readonly DatabaseReference _node;

        public FirebaseSaveStore(DatabaseReference node)
        {
            _node = node;
        }

        public static DatabaseReference V2Node(string userId)
        {
            return GameDatabase.Instance.RootReference.Child("users").Child(userId).Child("v2");
        }

        public static DatabaseReference V1Node(string userId)
        {
            return GameDatabase.Instance.RootReference.Child("users").Child(userId);
        }

        public async Task<string> LoadJsonAsync()
        {
            SoloHero.Core.Common.Log.Info(SoloHero.Core.Common.LogTag.Save, "remote load start " + _node.Key);
            DataSnapshot snapshot = await _node.GetValueAsync();
            SoloHero.Core.Common.Log.Info(SoloHero.Core.Common.LogTag.Save, "remote load done exists=" + snapshot.Exists);
            if (!snapshot.Exists) return null;
            return snapshot.GetRawJsonValue();
        }

        public async Task SaveJsonAsync(string json)
        {
            SoloHero.Core.Common.Log.Info(SoloHero.Core.Common.LogTag.Save, "remote save start");
            await _node.SetRawJsonValueAsync(json);
            SoloHero.Core.Common.Log.Info(SoloHero.Core.Common.LogTag.Save, "remote save done");
        }
    }
}
