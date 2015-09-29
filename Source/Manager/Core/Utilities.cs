using System.Linq;
using RimWorld;
using Verse;

namespace FM
{
    public static class Utilities
    {
        /// <summary>
        /// Returns current count of ThingDef thing, as used by core bill screens. (Bill_Production).
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>int</returns>
        public static int CountProducts(Thing thing)
        {
            if (thing == null) return 0;
            return thing.stackCount;
        }

        private static int _cachedCount;

        private static ThingFilter _cachedFilter;

        private static int _lastCache;

        private static bool TryGetCached(ThingFilter filter, out int count)
        {
            if (_lastCache < Find.TickManager.TicksGame + 250 && _cachedFilter == filter)
            {
                count = 0;
                return false;
            }
            count = _cachedCount;
            return true;
        }

        public static int CountProducts(ThingFilter filter)
        {
            int count;

            if (TryGetCached(filter, out count)) return count;

            foreach (Thing thing in filter.AllowedThingDefs.SelectMany(td => Find.ListerThings.ThingsOfDef(td))) // TODO: does this catch minified things?
            {
                QualityCategory quality;
                if (thing.TryGetQuality(out quality))
                {
                    if (!filter.AllowedQualityLevels.Includes(quality)) continue;
                }
                if (filter.AllowedHitPointsPercents.IncludesEpsilon(thing.HitPoints)) continue;
                
                count += CountProducts(thing);
            }
            _cachedFilter = filter;
            _lastCache = Find.TickManager.TicksGame;
            _cachedCount = count;
            return count;
        }

        public static bool IsInt(this string text)
        {
            int num;
            return int.TryParse(text, out num);
        }
    }
}
