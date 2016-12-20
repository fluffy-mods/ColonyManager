// Karel Kroeze
// Utilities_Hunting.cs
// 2016-12-09

using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public static class Utilities_Hunting
    {
        public static ThingDef HumanMeat = ThingDef.Named( "Human" ).race.meatDef;
        public static ThingCategoryDef RawMeat = DefDatabase<ThingCategoryDef>.GetNamed( "MeatRaw" );

        public static int EstimatedMeatCount( this Pawn p )
        {
            // StatDef MeatAmount
            return (int)( StatDefOf.MeatAmount.defaultBaseValue * p.BodySize );
        }

        public static int GetMeatCount( this Corpse c )
        {
            return EstimatedMeatCount( c.InnerPawn );
        }
    }
}
