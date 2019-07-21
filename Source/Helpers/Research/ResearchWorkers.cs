// Karel Kroeze
// ResearchWorkers.cs
// 2016-12-09

using Verse;

namespace FluffyManager
{
    public class UnlockPowerTab : ResearchMod
    {
        public override void Apply()
        {
            ManagerTab_Power.unlocked = true;
        }
    }
}