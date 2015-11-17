// Manager/Utilities.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:28

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FM
{
    public static class Utilities
    {
        public const float ListEntryHeight = 50f;

        // globals
        public const float Margin = 6f;
        public const float SliderHeight = 20f;

        public static Dictionary<ThingFilter, FilterCountCache> CountCache =
            new Dictionary<ThingFilter, FilterCountCache>();

        public static WorkTypeDef WorkTypeDefOf_Managing = DefDatabase<WorkTypeDef>.GetNamed( "Managing" );

        public static void Label( Rect rect, string label, string tooltip = null,
                                  TextAnchor anchor = TextAnchor.UpperLeft, float lrMargin = 0f, float tbMargin = 0f,
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

        private static bool TryGetCached( ThingFilter filter, out int count )
        {
            if ( CountCache.ContainsKey( filter ) )
            {
                FilterCountCache filterCountCache = CountCache[filter];
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

        public static int CountProducts( ThingFilter filter )
        {
            int count = 0;
            if ( filter != null &&
                 TryGetCached( filter, out count ) )
            {
                return count;
            }

#if DEBUG_COUNTS
            Log.Message("Obtaining new count");
#endif

            if ( filter != null )
            {
                foreach ( ThingDef td in filter.AllowedThingDefs )
                {
                    // if it counts as a resource, use the ingame counter (e.g. only steel in stockpiles.)
                    if ( td.CountAsResource )
                    {
#if DEBUG_COUNTS
                        Log.Message(td.LabelCap + ", " + Find.ResourceCounter.GetCount(td));
#endif
                        count += Find.ResourceCounter.GetCount( td );
                    }
                    else
                    {
                        foreach ( Thing t in Find.ListerThings.ThingsOfDef( td ) )
                        {
                            // otherwise, go look for stuff that matches our filters.
                            // TODO: does this catch minified things?
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
                }

                // update cache if exists.
                if ( CountCache.ContainsKey( filter ) )
                {
                    CountCache[filter].Cache = count;
                    CountCache[filter].TimeSet = Find.TickManager.TicksGame;
                }
                else
                {
                    CountCache.Add( filter, new FilterCountCache( count ) );
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
                // get size for the stamp, respecting proportions
                float proportion = rect.width / Resources.StampCompleted.width;
                Rect stampRect = rect;

                // don't scale up
                if ( proportion < 1f )
                {
                    stampRect.width *= proportion;
                    stampRect.height *= proportion;
                }

                // center stamp in available space
                stampRect = stampRect.CenteredOnXIn( rect ).CenteredOnYIn( rect );

                // draw it.
                if ( job.Completed )
                {
                    GUI.DrawTexture( stampRect, Resources.StampCompleted );
                    return;
                }
                if ( job.Suspended )
                {
                    GUI.DrawTexture( stampRect, Resources.StampSuspended );
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

        public class CachedValue
        {
            private int _cached;
            public int timeSet;
            public int updateInterval;

            public CachedValue( int count = 0, int updateInterval = 250 )
            {
                this.updateInterval = updateInterval;
                _cached = count;
                timeSet = Find.TickManager.TicksGame;
            }

            public bool TryGetCount( out int count )
            {
                if ( Find.TickManager.TicksGame - timeSet <= updateInterval )
                {
                    count = _cached;
                    return true;
                }
                count = 0;
                return false;
            }

            public void Update( int count )
            {
                _cached = count;
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