using System.Threading.Tasks;

namespace SoloHero.Core.Save
{
    public interface ISaveStore
    {
        Task<string> LoadJsonAsync();

        Task SaveJsonAsync(string json);
    }
}
