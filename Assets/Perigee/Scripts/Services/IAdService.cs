using System;

namespace Perigee
{
    public interface IAdService
    {
        // done(true) = реклама досмотрена, награду можно выдать
        void ShowRewarded(Action<bool> done);

        void ShowInterstitial();
    }
}
