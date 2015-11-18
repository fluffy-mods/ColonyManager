// Manager/Resources.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-17 12:59

using UnityEngine;
using Verse;

namespace FM
{
    public static class Resources
    {
        public static readonly Texture2D

            // sorting arrows
            ArrowTop                     = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowTop" ),
            ArrowUp                      = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowUp" ),
            ArrowDown                    = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowDown" ),
            ArrowBottom                  = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowBottom" ),

            // stamps
            StampCompleted               = ContentFinder<Texture2D>.Get( "UI/Stamps/Completed" ),
            StampSuspended               = ContentFinder<Texture2D>.Get( "UI/Stamps/Suspended" ),
            StampStart                   = ContentFinder<Texture2D>.Get( "UI/Stamps/Start" ),

            // tab icons
            IconOverview                 = ContentFinder<Texture2D>.Get( "UI/Icons/Overview" ),
            IconHammer                   = ContentFinder<Texture2D>.Get( "UI/Icons/Hammer" ),
            IconHunting                  = ContentFinder<Texture2D>.Get( "UI/Icons/Hunting" ),
            IconImportExport             = ContentFinder<Texture2D>.Get( "UI/Icons/ImportExport" ),
            IconForestry                 = ContentFinder<Texture2D>.Get( "UI/Icons/Tree" ),

            // misc
            SlightlyDarkBackground       = SolidColorMaterials.NewSolidColorTexture( 0f, 0f, 0f, .4f ),
            DeleteX                      = ContentFinder<Texture2D>.Get( "UI/Buttons/Delete", true ),
            Cog                          = ContentFinder<Texture2D>.Get( "UI/Buttons/Cog" ),
            BarBackgroundActiveTexture   = SolidColorMaterials.NewSolidColorTexture( new Color( 0.2f, 0.8f, 0.85f ) ),
            BarBackgroundInactiveTexture = SolidColorMaterials.NewSolidColorTexture( new Color( 0.7f, 0.7f, 0.7f ) ),
            Search                       = ContentFinder<Texture2D>.Get( "UI/Buttons/Search" );
    }
}