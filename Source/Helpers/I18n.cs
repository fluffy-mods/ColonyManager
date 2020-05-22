// I18n.cs
// Copyright Karel Kroeze, 2020-2020

using Verse;

namespace FluffyManager
{
    public class I18n
    {
        public static string Translate( string key, params object[] args )
        {
            return Key( key ).Translate( args ).ResolveTags();
        }

        public static string Key( string key ) => $"Fluffy.ColonyManager.{key}";
        public static string HistoryStock       = Translate( "HistoryStock" );
        public static string HistoryDesignated  = Translate( "HistoryDesignated" );
        public static string HistoryCorpses     = Translate( "HistoryCorpses" );
        public static string HistoryChunks      = Translate( "HistoryChunks" );
        public static string HistoryProduction  = Translate( "HistoryProduction" );
        public static string HistoryConsumption = Translate( "HistoryConsumption" );
        public static string HistoryBatteries   = Translate( "HistoryBatteries" );
    }
}