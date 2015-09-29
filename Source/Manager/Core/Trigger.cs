using Verse;

namespace FM
{
    public abstract class Trigger
    {
        public abstract bool State
        {
            get;
        }

        public abstract void DrawThresholdConfig(ref Listing_Standard listing);
    }
}
