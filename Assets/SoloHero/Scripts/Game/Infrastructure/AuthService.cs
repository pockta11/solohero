using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using SoloHero.Core.Boot;
using SoloHero.Core.Common;

namespace SoloHero.Game.Infrastructure
{
    public sealed class AuthService : IAuthGateway
    {
        public const string LocalUserId = "local";

        public async Task<string> SignInAsync()
        {
            try
            {
                DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (status != DependencyStatus.Available)
                {
                    Log.Warn(LogTag.Boot, "firebase dependencies unavailable: " + status);
                    return LocalUserId;
                }

                FirebaseAuth auth = FirebaseAuth.DefaultInstance;
                if (auth.CurrentUser != null) return auth.CurrentUser.UserId;

                AuthResult result = await auth.SignInAnonymouslyAsync();
                return result.User.UserId;
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "auth failed, local mode: " + e.Message);
                return LocalUserId;
            }
        }
    }
}
