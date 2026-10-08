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

        /// <summary>D-136: a first launch without a network plays in local mode instead of waiting on sign-in.</summary>
        public const int SignInTimeoutMs = 10000;

        private readonly TrustedClock _clock;

        /// <param name="clock">D-133: synced to the server's clock as soon as Firebase is available.</param>
        public AuthService(TrustedClock clock = null)
        {
            _clock = clock;
        }

        /// <summary>True once Firebase dependencies resolved; analytics works even if sign-in later fails.</summary>
        public bool FirebaseReady { get; private set; }

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

                FirebaseReady = true;
                ServerTimeSync.Start(_clock);
                FirebaseAuth auth = FirebaseAuth.DefaultInstance;
                if (auth.CurrentUser != null)
                {
                    Log.Info(LogTag.Boot, "existing user " + auth.CurrentUser.UserId);
                    return auth.CurrentUser.UserId;
                }

                Task<AuthResult> signIn = auth.SignInAnonymouslyAsync();
                if (await Task.WhenAny(signIn, Task.Delay(SignInTimeoutMs)) != signIn)
                {
                    // The account still arrives later and the next start uses it; this session saves locally.
                    _ = signIn.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    Log.Warn(LogTag.Boot, "sign-in timed out, local mode");
                    return LocalUserId;
                }

                AuthResult result = await signIn;
                Log.Info(LogTag.Boot, "signed in " + result.User.UserId);
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
