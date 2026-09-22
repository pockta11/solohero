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
            return FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(userId).Child("v2");
        }

        public static DatabaseReference V1Node(string userId)
        {
            return FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(userId);
        }

        public async Task<string> LoadJsonAsync()
        {
            DataSnapshot snapshot = await _node.GetValueAsync();
            if (!snapshot.Exists) return null;
            return snapshot.GetRawJsonValue();
        }

        public async Task SaveJsonAsync(string json)
        {
            await _node.SetRawJsonValueAsync(json);
        }
    }
}
