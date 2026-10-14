using System;
using System.Runtime.InteropServices;

namespace Perigee
{
    // Заглушки: настоящего SDK в v1 нет, всё завершается сразу.
    public class StubAdService : IAdService
    {
        public void ShowRewarded(Action<bool> done)
        {
            done(true);
        }

        public void ShowInterstitial()
        {
        }
    }

    public class StubLeaderboardService : ILeaderboardService
    {
        public void Submit(int score)
        {
        }
    }

    public class StubPlatformService : IPlatformService
    {
        public void GameReady()
        {
        }

        public void GameplayStart()
        {
        }

        public void GameplayStop()
        {
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Plugins/WebGL/Platform.jslib
        [DllImport("__Internal")]
        static extern string PlatformLanguage();

        [DllImport("__Internal")]
        static extern void PlatformVibrate(int ms);

        public string Language()
        {
            return PlatformLanguage();
        }

        public void Vibrate(int ms)
        {
            PlatformVibrate(ms);
        }
#else
        // в редакторе язык русский, вибрации нет
        const string EditorLanguage = "ru";

        public string Language()
        {
            return EditorLanguage;
        }

        public void Vibrate(int ms)
        {
        }
#endif
    }
}
