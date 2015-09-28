using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Verse;
using RimWorld;

namespace FM
{
    public static class Utilities
    {
        /// <summary>
        /// Cast an arbitrary object into another, preserving values of common members.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="myobj"></param>
        /// <returns></returns>
        public static T Cast<T>(this Object myobj)
        {
            Type objectType = myobj.GetType();
            Type target = typeof(T);
            var x = Activator.CreateInstance(target, false);
            var z = from source in objectType.GetMembers().ToList()
                    where source.MemberType == MemberTypes.Property
                    select source;
            var d = from source in target.GetMembers().ToList()
                    where source.MemberType == MemberTypes.Property
                    select source;
            List<MemberInfo> members = d.Where(memberInfo => d.Select(c => c.Name)
               .ToList().Contains(memberInfo.Name)).ToList();
            PropertyInfo propertyInfo;
            object value;
            foreach (var memberInfo in members)
            {
                propertyInfo = typeof(T).GetProperty(memberInfo.Name);
                value = myobj.GetType().GetProperty(memberInfo.Name).GetValue(myobj, null);

                propertyInfo.SetValue(x, value, null);
            }
            return (T)x;
        }

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

        private static int cachedCount;

        private static ThingFilter cachedFilter;

        private static int lastCache;

        private static bool TryGetCached(ThingFilter category, out int count)
        {
            if (lastCache < Find.TickManager.TicksGame + 250)
            {
                count = 0;
                return false;
            }
            else
            {
                count = cachedCount;
                return true;
            }
        }

        public static int CountProducts(ThingFilter filter)
        {
            int count = 0;

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
            cachedFilter = filter;
            lastCache = Find.TickManager.TicksGame;
            cachedCount = count;
            return count;
        }

        public static bool IsInt(this string text)
        {
            int num;
            return int.TryParse(text, out num);
        }
    }
}
