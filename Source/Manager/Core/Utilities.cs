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
            if (Find.TickManager.TicksGame - _lastCache < 250 && _cachedFilter == filter)
            {
                count = _cachedCount;
                return true;
            }
#if DEBUG_COUNTS
            Log.Message("not cached");
#endif
            count = 0;
            return false;
        }

        public static int CountProducts(ThingFilter filter)
        {
            int count = 0;
            if (filter != null && TryGetCached(filter, out count)) return count;

#if DEBUG_COUNTS
            Log.Message("Obtaining new count");
#endif

            if (filter != null)
            {
                foreach (ThingDef td in filter.AllowedThingDefs)
                {
                    // if it counts as a resource, use the ingame counter (e.g. only steel in stockpiles.)
                    if (td.CountAsResource)
                    {
#if DEBUG_COUNTS
                        Log.Message(td.LabelCap + ", " + Find.ResourceCounter.GetCount(td));
#endif
                        count += Find.ResourceCounter.GetCount(td);
                    }
                    else
                    {
                        foreach (Thing t in Find.ListerThings.ThingsOfDef(td))
                        {
                            // otherwise, go look for stuff that matches our filters.
                            // TODO: does this catch minified things?
                            QualityCategory quality;
                            if (t.TryGetQuality(out quality))
                            {
                                if (!filter.AllowedQualityLevels.Includes(quality)) continue;
                            }
                            if (filter.AllowedHitPointsPercents.IncludesEpsilon(t.HitPoints)) continue;

#if DEBUG_COUNTS
                            Log.Message(t.LabelCap + ": " + CountProducts(t));
#endif

                            count += CountProducts(t);
                        }
                    }
                }
                _cachedFilter = filter;
            }
            _lastCache = Find.TickManager.TicksGame;
            _cachedFilter = filter;
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
