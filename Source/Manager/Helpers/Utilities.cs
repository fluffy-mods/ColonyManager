// Manager/Utilities.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:28

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System.Reflection;

namespace FM
{
    public static class Utilities
    {
        public const float LargeListEntryHeight = 50f;
        public const float Margin = 6f;
        public const float SliderHeight = 20f;
        public static float BottomButtonHeight = 50f;
        public static Vector2 ButtonSize = new Vector2( 200f, 40f );

        public static Dictionary<StockpileFilter, FilterCountCache> CountCache =
            new Dictionary<StockpileFilter, FilterCountCache>();

        public static float LargeIconSize = 32f;
        public static float ListEntryHeight = 30f;
        public static float MediumIconSize = 24f;
        public static float SmallIconSize = 16f;
        public static float TitleHeight = 50f;
        public static float TopAreaHeight = 30f;
        public static WorkTypeDef WorkTypeDefOf_Managing = DefDatabase<WorkTypeDef>.GetNamed( "Managing" );

        public static void Label( ref Vector2 cur, float width, float height, string label, string tooltip = null,
                                  TextAnchor anchor = TextAnchor.MiddleLeft, float lrMargin = Margin,
                                  float tbMargin = 0f,
                                  GameFont font = GameFont.Small )
        {
            Rect rect = new Rect( cur.x, cur.y, width, height );
            Label( rect, label, tooltip, anchor, lrMargin, tbMargin, font );
            cur.y += height;
        }

        public static void Label( Rect rect, string label, string tooltip = null,
                                  TextAnchor anchor = TextAnchor.MiddleLeft, float lrMargin = Margin,
                                  float tbMargin = 0f,
                                  GameFont font = GameFont.Small )
        {
            // apply margins
            Rect labelRect = new Rect( rect.xMin + lrMargin, rect.yMin + tbMargin, rect.width - 2 * lrMargin,
                                       rect.height - 2 * tbMargin );

            // draw label with anchor - reset anchor
            Text.Anchor = anchor;
            Text.Font = font;
            Widgets.Label( labelRect, label );
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // if set, draw tooltip
            if ( tooltip != null )
            {
                TooltipHandler.TipRegion( rect, tooltip );
            }
        }

        private static bool TryGetCached( StockpileFilter stockpileFilter, out int count )
        {
            if ( CountCache.ContainsKey( stockpileFilter ) )
            {
                FilterCountCache filterCountCache = CountCache[stockpileFilter];
                if ( Find.TickManager.TicksGame - filterCountCache.TimeSet < 250 )
                {
                    count = filterCountCache.Cache;
                    return true;
                }
            }
#if DEBUG_COUNTS
            Log.Message("not cached");
#endif
            count = 0;
            return false;
        }

        public static string TimeString( this int ticks )
        {
            int days = ticks / GenDate.TicksPerDay,
                hours = ticks % GenDate.TicksPerDay / GenDate.TicksPerHour;

            string s = string.Empty;

            if ( days > 0 )
            {
                s += days + "LetterDay".Translate() + " ";
            }
            s += hours + "LetterHour".Translate();

            return s;
        }

        public static int CountProducts( ThingFilter filter, Zone_Stockpile stockpile = null )
        {
            int count = 0;

            // copout if filter is null
            if ( filter == null )
            {
                return count;
            }
            StockpileFilter cacheKey = new StockpileFilter( filter, stockpile );
            if ( TryGetCached( cacheKey, out count ) )
            {
                return count;
            }

#if DEBUG_COUNTS
            Log.Message("Obtaining new count");
#endif

            foreach ( ThingDef td in filter.AllowedThingDefs )
            {
                // if it counts as a resource and we're not limited to a single stockpile, use the ingame counter (e.g. only steel in stockpiles.)
                if ( td.CountAsResource &&
                     stockpile == null )
                {
#if DEBUG_COUNTS
                        Log.Message(td.LabelCap + ", " + Find.ResourceCounter.GetCount(td));
#endif
                    // we don't need to bother with quality / hitpoints as these are non-existant/irrelevant for resources.
                    count += Find.ResourceCounter.GetCount( td );
                }
                else
                {
                    // otherwise, go look for stuff that matches our filters.
                    List<Thing> thingList = Find.ListerThings.ThingsOfDef( td );

                    // if filtered by stockpile, filter the thinglist accordingly.
                    if ( stockpile != null )
                    {
                        SlotGroup areaSlotGroup = stockpile.slotGroup;
                        thingList = thingList.Where( t => t.Position.GetSlotGroup() == areaSlotGroup ).ToList();
                    }
                    foreach ( Thing t in thingList )
                    {
                        QualityCategory quality;
                        if ( t.TryGetQuality( out quality ) )
                        {
                            if ( !filter.AllowedQualityLevels.Includes( quality ) )
                            {
                                continue;
                            }
                        }
                        if ( filter.AllowedHitPointsPercents.IncludesEpsilon( t.HitPoints ) )
                        {
                            continue;
                        }

#if DEBUG_COUNTS
                            Log.Message(t.LabelCap + ": " + CountProducts(t));
#endif

                        count += t.stackCount;
                    }
                }

                // update cache if exists.
                if ( CountCache.ContainsKey( cacheKey ) )
                {
                    CountCache[cacheKey].Cache = count;
                    CountCache[cacheKey].TimeSet = Find.TickManager.TicksGame;
                }
                else
                {
                    CountCache.Add( cacheKey, new FilterCountCache( count ) );
                }
            }
            return count;
        }

        public static bool IsInt( this string text )
        {
            int num;
            return int.TryParse( text, out num );
        }

        public static void DrawStatusForListEntry<T>( this T job, Rect rect, Trigger trigger ) where T : ManagerJob
        {
            if ( job.Completed ||
                 job.Suspended )
            {
                // put a stamp on it
                Rect stampRect = new Rect( 0f, 0f, MediumIconSize, MediumIconSize );

                // center stamp in available space
                stampRect = stampRect.CenteredOnXIn( rect ).CenteredOnYIn( rect );

                // draw it.
                if ( job.Completed )
                {
                    GUI.DrawTexture( stampRect, Resources.StampCompleted );
                    TooltipHandler.TipRegion( stampRect, "FM.JobCompletedToolip".Translate() );
                    return;
                }
                if ( job.Suspended )
                {
                    // allow activating the job from here.
                    if ( !Mouse.IsOver( stampRect ) )
                    {
                        GUI.DrawTexture( stampRect, Resources.StampSuspended );
                    }
                    else
                    {
                        if ( Widgets.ImageButton( stampRect, Resources.StampStart ) )
                        {
                            job.Suspended = false;
                        }
                        TooltipHandler.TipRegion( stampRect, "FM.JobSuspendedToolTip".Translate() );
                    }
                    return;
                }
            }
            if ( trigger == null )
            {
                Log.Message( "Trigger NULL" );
                return;
            }

            // set up rects
            Rect progressRect = new Rect( Margin, 0f, ManagerJob.ProgressRectWidth, rect.height ),
                 lastUpdateRect = new Rect( progressRect.xMax + Margin, 0f, ManagerJob.LastUpdateRectWidth, rect.height );

            // set drawing canvas
            GUI.BeginGroup( rect );

            // draw progress bar
            trigger.DrawProgressBar( progressRect, true );

            // draw time since last action
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( lastUpdateRect, ( Find.TickManager.TicksGame - job.LastAction ).TimeString() );

            // set tooltips
            TooltipHandler.TipRegion( progressRect, trigger.StatusTooltip );
            TooltipHandler.TipRegion( lastUpdateRect,
                                      "FM.LastUpdateTooltip".Translate(
                                          ( Find.TickManager.TicksGame - job.LastAction ).TimeString() ) );

            GUI.EndGroup();
        }

        public static void DrawToggle( Rect rect, string label, ref bool checkOn, float size = 24f,
                                       float margin = Margin, GameFont font = GameFont.Small )
        {
            // set up rects
            Rect labelRect = rect;
            Rect checkRect = new Rect( rect.xMax - size - margin * 2, 0f, size, size );

            // finetune rects
            checkRect = checkRect.CenteredOnYIn( labelRect );

            // draw label
            Label( rect, label, null, TextAnchor.MiddleLeft, margin, font: font );

            // draw check
            if ( checkOn )
            {
                GUI.DrawTexture( checkRect, Widgets.CheckboxOnTex );
            }
            else
            {
                GUI.DrawTexture( checkRect, Widgets.CheckboxOffTex );
            }

            // interactivity
            Widgets.DrawHighlightIfMouseover( rect );
            if ( Widgets.InvisibleButton( rect ) )
            {
                checkOn = !checkOn;
            }
        }

        public static void DrawToggle( Rect rect, string label, bool checkOn, Action on, Action off, float size = 24f,
                                       float margin = Margin )
        {
            // set up rects
            Rect labelRect = rect;
            Rect checkRect = new Rect( rect.xMax - size - margin * 2, 0f, size, size );

            // finetune rects
            checkRect = checkRect.CenteredOnYIn( labelRect );

            // draw label
            Label( rect, label, null, TextAnchor.MiddleLeft, margin );

            // draw check
            if ( checkOn )
            {
                GUI.DrawTexture( checkRect, Widgets.CheckboxOnTex );
            }
            else
            {
                GUI.DrawTexture( checkRect, Widgets.CheckboxOffTex );
            }

            // interactivity
            Widgets.DrawHighlightIfMouseover( rect );
            if ( Widgets.InvisibleButton( rect ) )
            {
                if ( checkOn )
                {
                    off();
                }
                else
                {
                    @on();
                }
            }
        }

        public static void DrawToggle( Rect rect, string label, bool checkOn, Action toggle, float size = 24f,
                                       float margin = Margin )
        {
            DrawToggle( rect, label, checkOn, toggle, toggle, size );
        }

        public static bool TryGetPrivateField( Type type, object instance, string fieldName, out object value,
                                               BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance )
        {
            FieldInfo field = type.GetField( fieldName, flags );
            value = field?.GetValue( instance );
            return value != null;
        }

        public static bool TrySetPrivateField( Type type, object instance, string fieldName, object value,
                                               BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance )
        {
            // get field info
            FieldInfo field = type.GetField( fieldName, flags );

            // failed?
            if ( field == null )
            {
                return false;
            }

            // try setting it.
            field.SetValue( instance, value );

            // test by fetching the field again. (this is highly, stupidly inefficient, but ok).
            object test;
            if ( !TryGetPrivateField( type, instance, fieldName, out test, flags ) )
            {
                return false;
            }
            return test == value;
        }

        public struct StockpileFilter
        {
            private ThingFilter filter;
            private Zone_Stockpile stockpile;

            public StockpileFilter( ThingFilter filter, Zone_Stockpile stockpile )
            {
                this.filter = filter;
                this.stockpile = stockpile;
            }
        }

        public class CachedValue
        {
            private int _cached;
            public int timeSet;
            public int updateInterval;

            public CachedValue( int value = 0, int updateInterval = 250 )
            {
                this.updateInterval = updateInterval;
                _cached = value;
                timeSet = Find.TickManager.TicksGame;
            }

            public bool TryGetValue( out int value )
            {
                if ( Find.TickManager.TicksGame - timeSet <= updateInterval )
                {
                    value = _cached;
                    return true;
                }
                value = 0;
                return false;
            }

            public void Update( int value )
            {
                _cached = value;
                timeSet = Find.TickManager.TicksGame;
            }
        }

        // count cache for multiple products
        public class FilterCountCache
        {
            public int Cache;
            public int TimeSet;

            public FilterCountCache( int count )
            {
                Cache = count;
                TimeSet = Find.TickManager.TicksGame;
            }
        }
    }
}