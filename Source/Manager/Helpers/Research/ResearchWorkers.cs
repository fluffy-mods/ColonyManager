using RimWorld;
using System;
using System.Linq;
using Verse;

namespace FluffyManager
{
    public static class ResearchWorkers
    {
        public static void UnlockPowerTab()
        {
            Manager.Get.ManagerTabs.Add( new ManagerTab_Power() );
            Manager.Get.RefreshTabs();
        }
    }
}