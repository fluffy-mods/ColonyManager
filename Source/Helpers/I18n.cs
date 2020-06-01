// I18n.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class I18n
    {
        public static string HistoryStock       = Translate( "HistoryStock" );
        public static string HistoryDesignated  = Translate( "HistoryDesignated" );
        public static string HistoryCorpses     = Translate( "HistoryCorpses" );
        public static string HistoryChunks      = Translate( "HistoryChunks" );
        public static string HistoryProduction  = Translate( "HistoryProduction" );
        public static string HistoryConsumption = Translate( "HistoryConsumption" );
        public static string HistoryBatteries   = Translate( "HistoryBatteries" );


        public static string Aggressiveness( float aggression )
        {
            return Translate( "Aggressiveness",
                              aggression >= .1f
                                  ? aggression.ToStringPercent().Colorize( Color.red )
                                  : aggression.ToStringPercent() );
        }

        public static string Key( string key )
        {
            return $"Fluffy.ColonyManager.{key}";
        }

        public static string Translate( string key, params object[] args )
        {
            return Key( key ).Translate( args ).ResolveTags();
        }

        public static string YieldOne( string label )
        {
            return $"{Translate( "Yield" )} {label}";
        }

        public static string YieldMany( IEnumerable<string> labels )
        {
            return $"{Translate( "Yield" )}\n - {labels.ToLineList( " - " )}";
        }

        public static string YieldOne( float yield, ThingDef def )
        {
            return YieldOne( $"{def.LabelCap} x{yield:F0} " );
        }

        public static string Gender( Gender gender )
        {
            return Translate( $"Gender.{gender}" );
        }

        public static string ChanceToDrop( float chance )
        {
            return Translate( "ChanceToDrop", chance.ToStringPercent() );
        }
    }
}