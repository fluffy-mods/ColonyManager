// Utilities_Mining.cs
// Copyright Karel Kroeze, 2020-2020

using System.Linq;
using RimWorld;
using Verse;

namespace FluffyManager
{
    public static class Utilities_Mining
    {
        public static bool IsChunk( this ThingDef def )
        {
            return def?.thingCategories?.Any( c => ThingCategoryDefOf.Chunks.ThisAndChildCategoryDefs.Contains( c ) ) ??
                   false;
        }
    }
}