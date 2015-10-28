using Verse;

namespace FM
{
    public abstract class Trigger : IExposable
    {
        public abstract bool State { get; }

        public abstract void ExposeData();

        public abstract void DrawThresholdConfig( ref Listing_Standard listing );
    }
}