using System;

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
    }
}
