using System;

namespace CatapultCats.Core
{
    public sealed class ShotCounter
    {
        public ShotCounter(int maximumShots)
        {
            if (maximumShots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumShots));
            }

            MaximumShots = maximumShots;
            RemainingShots = maximumShots;
        }

        public int MaximumShots { get; }
        public int RemainingShots { get; private set; }
        public bool HasShotsRemaining => RemainingShots > 0;
        public event Action<int> Changed;

        public bool Consume()
        {
            if (!HasShotsRemaining)
            {
                return false;
            }

            RemainingShots--;
            Changed?.Invoke(RemainingShots);
            return true;
        }

        public void Reset()
        {
            RemainingShots = MaximumShots;
            Changed?.Invoke(RemainingShots);
        }
    }
}
