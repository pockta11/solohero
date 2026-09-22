using System.Threading.Tasks;

namespace SoloHero.Core.Boot
{
    public interface IAuthGateway
    {
        Task<string> SignInAsync();
    }
}
