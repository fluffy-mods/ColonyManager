using RimWorld;
using System;
using System.Linq;
using Verse;

namespace FluffyManager
{
    public static class ResearchWorkers
    {
        #region Methods

        public static void UnlockPowerTab()
        {
            Manager.Get._powerUnlocked = true;
        }

        #endregion Methods
    }
}