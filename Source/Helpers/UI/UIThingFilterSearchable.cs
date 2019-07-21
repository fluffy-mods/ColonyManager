// Karel Kroeze
// UIThingFilterSearchable.cs
// 2016-12-09

using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class ThingFilterUI
    {
        private float viewHeight;

        public void DoThingFilterConfigWindow( Rect canvas, ref Vector2 scrollPosition, ThingFilter filter,
                                               ThingFilter parentFilter = null, int openMask = 1,
                                               bool buttonsAtBottom = false )
        {
            // respect your bounds!
            GUI.BeginGroup( canvas );
            canvas = canvas.AtZero();

            // set up buttons
            Text.Font = GameFont.Tiny;
            var width           = canvas.width - 2f;
            var clearButtonRect = new Rect( canvas.x             + 1f, canvas.y + 1f, width     / 2f, 24f );
            var allButtonRect   = new Rect( clearButtonRect.xMax + 1f, clearButtonRect.y, width / 2f, 24f );

            // offset canvas position for buttons.
            if ( buttonsAtBottom )
            {
                clearButtonRect.y =  canvas.height - clearButtonRect.height;
                allButtonRect.y   =  canvas.height - clearButtonRect.height;
                canvas.yMax       -= clearButtonRect.height;
            }
            else
            {
                canvas.yMin = clearButtonRect.height;
            }

            // draw buttons + logic
            if ( Widgets.ButtonTextSubtle( clearButtonRect, "ClearAll".Translate() ) ) filter.SetDisallowAll();
            if ( Widgets.ButtonTextSubtle( allButtonRect, "AllowAll".Translate() ) ) filter.SetAllowAll( parentFilter );
            Text.Font = GameFont.Small;

            // do list
            var curY     = 2f;
            var viewRect = new Rect( 0f, 0f, canvas.width - ScrollbarWidth, viewHeight );

            // scrollview
            Widgets.BeginScrollView( canvas, ref scrollPosition, viewRect );

            // slider(s)
            DrawHitPointsFilterConfig( ref curY, viewRect.width, filter );
            DrawQualityFilterConfig( ref curY, viewRect.width, filter );

            // main listing
            var listingRect            = new Rect( 0f, curY, viewRect.width, 9999f );
            var listingTreeThingFilter = new Listing_TreeThingFilter( filter, parentFilter, null, null, null );
            listingTreeThingFilter.Begin( listingRect );
            var node = ThingCategoryNodeDatabase.RootNode;
            if ( parentFilter != null )
            {
                if ( parentFilter.DisplayRootCategory == null ) parentFilter.RecalculateDisplayRootCategory();
                node = parentFilter.DisplayRootCategory;
            }

            // draw the actual thing
            listingTreeThingFilter.DoCategoryChildren( node, 0, openMask, Find.CurrentMap, true );
            listingTreeThingFilter.End();

            // update height.
            viewHeight = curY + listingTreeThingFilter.CurHeight;
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private static void DrawHitPointsFilterConfig( ref float y, float width, ThingFilter filter )
        {
            if ( !filter.allowedHitPointsConfigurable ) return;

            var rect                     = new Rect( 20f, y, width - 20f, 26f );
            var allowedHitPointsPercents = filter.AllowedHitPointsPercents;
            Widgets.FloatRange( rect, 1, ref allowedHitPointsPercents, 0f, 1f, "HitPoints", ToStringStyle.PercentZero );
            filter.AllowedHitPointsPercents =  allowedHitPointsPercents;
            y                               += 26f;
            y                               += 5f;
            Text.Font                       =  GameFont.Small;
        }

        private static void DrawQualityFilterConfig( ref float y, float width, ThingFilter filter )
        {
            if ( !filter.allowedQualitiesConfigurable ) return;

            var rect                 = new Rect( 20f, y, width - 20f, 26f );
            var allowedQualityLevels = filter.AllowedQualityLevels;
            Widgets.QualityRange( rect, 2, ref allowedQualityLevels );
            filter.AllowedQualityLevels =  allowedQualityLevels;
            y                           += 26f;
            y                           += 5f;
            Text.Font                   =  GameFont.Small;
        }
    }
}