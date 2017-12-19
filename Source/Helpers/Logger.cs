// Karel Kroeze
// Logger.cs
// 2017-06-10

using System.Diagnostics;

namespace FluffyManager
{
    public static class Logger
    {
        public const string identifier = "Colony Manager";

        [Conditional( "DEBUG" )]
        public static void Debug( string message )
        {
            Verse.Log.Message( identifier + " :: " + message );
        }

        [Conditional( "DEBUG_FOLLOW" )]
        public static void Follow( string message )
        {
            Verse.Log.Message( identifier + " :: " + message );
        }
    }
}