// Karel Kroeze
// ResearchWorkers.cs
// 2016-12-09

using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class UnlockPowerTab: ResearchMod
    {
        #region Overrides of ResearchMod

        public override void Apply()
        {
            Manager._powerUnlocked = true;
        }

        #endregion
    }
}
