using Harmony;
using Verse;
using Verse.AI;

namespace FluffyManager
{
    /**
     * Patch for https://ludeon.com/mantis/view.php?id=3565.   
     * Update: 16/9, issue resolved. 
     * TODO: Remove patch on next RW update.
     */

    [HarmonyPatch( typeof( PathFinder ), "GetBuildingCost" )]
    public static class PathFinder_GetBuildingCost
    {
        public static bool Prefix( TraverseParms traverseParms, Pawn pawn, ref int __result )
        {
            if ( traverseParms.mode == TraverseMode.PassDoors && pawn == null )
            {
                __result = 150;
                return false;
            }

            return true;
        }
    }
}