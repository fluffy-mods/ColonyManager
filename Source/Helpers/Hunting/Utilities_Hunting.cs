// Utilities_Hunting.cs
// Copyright Karel Kroeze, 2018-2020

using RimWorld;
using Verse;

namespace FluffyManager
{
    public static class Utilities_Hunting
    {
        public static ThingCategoryDef FoodRaw    = DefDatabase<ThingCategoryDef>.GetNamed( "FoodRaw" );
        public static ThingDef         HumanMeat  = ThingDef.Named( "Human" ).race.meatDef;
        public static ThingDef         InsectMeat = ThingDef.Named( "Megaspider" ).race.meatDef;
        public static ThingCategoryDef MeatRaw    = DefDatabase<ThingCategoryDef>.GetNamed( "MeatRaw" );

        public static int EstimatedMeatCount( this Pawn p )
        {
            return (int) p.GetStatValue( StatDefOf.MeatAmount );
        }

        public static int EstimatedMeatCount( this Corpse c )
        {
            return EstimatedMeatCount( c.InnerPawn );
        }
    }
}