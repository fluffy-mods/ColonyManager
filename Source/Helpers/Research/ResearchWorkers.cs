// Karel Kroeze
// ResearchWorkers.cs
// 2016-12-09

using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public static class ResearchWorkers
    {
        public static void UnlockPowerTab()
        {
            Manager._powerUnlocked = true;
        }
    }
}
