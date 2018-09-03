using RimWorld;
using Verse;
using System;
using Harmony;

namespace FluffyManager {
    /**
    Patch for https://ludeon.com/mantis/view.php?id=3565.   
     */
     
    [HarmonyPatch(typeof(PathFinder), "GetBuildingCost")]
    public static class PathFinder_GetBuildingCost {
        public static bool Prefix( TraverseParms parms, Pawn pawn, int __result){
            if (parms.mode == TraverseMode.PassDoors && pawn == null ){
                __result = 150;
                return false;
            }
            return true;
        }
    }
}