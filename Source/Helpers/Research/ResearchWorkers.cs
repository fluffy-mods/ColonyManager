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
            Manager._powerUnlocked = true;
        }

        #endregion Methods
    }
}