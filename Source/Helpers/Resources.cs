// Karel Kroeze
// Resources.cs
// 2016-12-09

using UnityEngine;
using Verse;

namespace FluffyManager
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Color Orange                       = new Color( 1f, 144 / 255f, 0f ),
                                     SlightlyDarkBackgroundColour = new Color( 0f, 0f, 0f, .2f );

        public static Texture2D
            // sorting arrows
            ArrowTop    = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowTop" ),
            ArrowUp     = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowUp" ),
            ArrowDown   = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowDown" ),
            ArrowBottom = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowBottom" ),

            // stamps
            StampCompleted = ContentFinder<Texture2D>.Get( "UI/Stamps/Completed" ),
            StampSuspended = ContentFinder<Texture2D>.Get( "UI/Stamps/Suspended" ),
            StampStart     = ContentFinder<Texture2D>.Get( "UI/Stamps/Start" ),

            // tab icons
            IconOverview     = ContentFinder<Texture2D>.Get( "UI/Icons/Overview" ),
            IconHammer       = ContentFinder<Texture2D>.Get( "UI/Icons/Hammer" ),
            IconHunting      = ContentFinder<Texture2D>.Get( "UI/Icons/Hunting" ),
            IconImportExport = ContentFinder<Texture2D>.Get( "UI/Icons/ImportExport" ),
            IconForestry     = ContentFinder<Texture2D>.Get( "UI/Icons/Tree" ),
            IconLivestock    = ContentFinder<Texture2D>.Get( "UI/Icons/Livestock" ),
            IconForaging     = ContentFinder<Texture2D>.Get( "UI/Icons/Foraging" ),
            IconPower        = ContentFinder<Texture2D>.Get( "UI/Icons/Power" ),
            IconTrading      = ContentFinder<Texture2D>.Get( "UI/Icons/Power" ),
            IconMining       = ContentFinder<Texture2D>.Get( "UI/Icons/mining" ),

            // misc
            SlightlyDarkBackground       = SolidColorMaterials.NewSolidColorTexture( SlightlyDarkBackgroundColour ),
            DeleteX                      = ContentFinder<Texture2D>.Get( "UI/Buttons/Delete" ),
            Cog                          = ContentFinder<Texture2D>.Get( "UI/Buttons/Cog" ),
            BarBackgroundActiveTexture   = SolidColorMaterials.NewSolidColorTexture( new Color( 0.2f, 0.8f, 0.85f ) ),
            BarBackgroundInactiveTexture = SolidColorMaterials.NewSolidColorTexture( new Color( 0.7f, 0.7f, 0.7f ) ),
            Search                       = ContentFinder<Texture2D>.Get( "UI/Buttons/Search" ),
            BarShader                    = ContentFinder<Texture2D>.Get( "UI/Misc/BarShader" ),
            Refresh                      = ContentFinder<Texture2D>.Get( "UI/Icons/refresh" ),
            Stopwatch                    = ContentFinder<Texture2D>.Get( "UI/Icons/stopwatch" ),

            // livestock header icons
            WoolIcon   = ContentFinder<Texture2D>.Get( "UI/Icons/wool" ),
            MilkIcon   = ContentFinder<Texture2D>.Get( "UI/Icons/milk" ),
            StageC     = ContentFinder<Texture2D>.Get( "UI/Icons/stage-3" ),
            StageB     = ContentFinder<Texture2D>.Get( "UI/Icons/stage-2" ),
            StageA     = ContentFinder<Texture2D>.Get( "UI/Icons/stage-1" ),
            FemaleIcon = ContentFinder<Texture2D>.Get( "UI/Icons/female" ),
            MaleIcon   = ContentFinder<Texture2D>.Get( "UI/Icons/male" ),
            MeatIcon   = ContentFinder<Texture2D>.Get( "UI/Icons/meat" ),
            UnkownIcon = ContentFinder<Texture2D>.Get( "UI/Icons/unknown" );

        //public static Texture2D[] LifeStages = {StageA, StageB, StageC};

        public static Texture2D LifeStages( int lifeStageIndex )
        {
            switch ( lifeStageIndex )
            {
                case 0:
                    return StageA;
                case 1:
                    return StageB;
                case 2:
                default:
                    return StageC; // animals with > 3 lifestages just get the adult icon.
            }
        }
    }
}