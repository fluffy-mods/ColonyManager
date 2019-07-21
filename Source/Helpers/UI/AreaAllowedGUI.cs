// Karel Kroeze
// AreaAllowedGUI.cs
// 2016-12-09

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FluffyManager
{
    internal class AreaAllowedGUI
    {
        public static Area DoAllowedAreaSelectors( Rect rect,
                                                   Area areaIn,
                                                   Map map,
                                                   float lrMargin = 0 )
        {
            var areaIO = areaIn;
            DoAllowedAreaSelectors( rect, ref areaIO, map, lrMargin );
            return areaIO;
        }

        // RimWorld.AreaAllowedGUI
        public static void DoAllowedAreaSelectors( ref Vector2 pos, float width, ref Area area, Map map,
                                                   float margin = 0 )
        {
            var rect = new Rect(
                pos.x,
                pos.y,
                width,
                Constants.ListEntryHeight );
            pos.y += Constants.ListEntryHeight;
            DoAllowedAreaSelectors( rect, ref area, map, margin );
        }

        public static void DoAllowedAreaSelectors( Rect rect,
                                                   ref Area area,
                                                   Map map,
                                                   float lrMargin = 0 )
        {
            if ( lrMargin > 0 )
            {
                rect.xMin  += lrMargin;
                rect.width -= lrMargin * 2;
            }

            var allAreas  = map.areaManager.AllAreas;
            var areaCount = 1;
            for ( var i = 0; i < allAreas.Count; i++ )
                if ( allAreas[i].AssignableAsAllowed() )
                    areaCount++;

            var widthPerArea = rect.width / areaCount;
            Text.WordWrap = false;
            Text.Font     = GameFont.Tiny;
            var nullAreaRect = new Rect( rect.x, rect.y, widthPerArea, rect.height );
            DoAreaSelector( nullAreaRect, ref area, null );
            var areaIndex = 1;
            for ( var j = 0; j < allAreas.Count; j++ )
                if ( allAreas[j].AssignableAsAllowed() )
                {
                    var xOffset  = areaIndex * widthPerArea;
                    var areaRect = new Rect( rect.x + xOffset, rect.y, widthPerArea, rect.height );
                    DoAreaSelector( areaRect, ref area, allAreas[j] );
                    areaIndex++;
                }

            Text.WordWrap = true;
            Text.Font     = GameFont.Small;
        }

        public static void DoAllowedAreaSelectorsMC( Rect rect, ref Dictionary<Area, bool> areas, float lrMargin = 0 )
        {
            if ( lrMargin > 0 )
            {
                rect.xMin  += lrMargin;
                rect.width -= lrMargin * 2;
            }

            var widthPerArea = rect.width / areas.Count;
            Text.WordWrap = false;
            Text.Font     = GameFont.Tiny;
            var nullAreaRect = new Rect( rect.x, rect.y, widthPerArea, rect.height );
            var areaIndex    = 0;

            // need to use a 'clean' list of keys to iterate over when changing the dictionary values
            var _areas = new List<Area>( areas.Keys );

            foreach ( var area in _areas )
            {
                var xOffset  = areaIndex++ * widthPerArea;
                var areaRect = new Rect( rect.x + xOffset, rect.y, widthPerArea, rect.height );
                areas[area] = DoAreaSelector( areaRect, area, areas[area] );
            }

            Text.WordWrap = true;
            Text.Font     = GameFont.Small;
        }

        private static bool DoAreaSelector( Rect rect, Area area, bool status )
        {
            rect = rect.ContractedBy( 1f );
            GUI.DrawTexture( rect, area == null ? BaseContent.GreyTex : area.ColorTexture );
            Text.Anchor = TextAnchor.MiddleLeft;
            var text  = AreaUtility.AreaAllowedLabel_Area( area );
            var rect2 = rect;
            rect2.xMin += 3f;
            rect2.yMin += 2f;
            Widgets.Label( rect2, text );
            if ( status )
                Widgets.DrawBox( rect, 2 );
            if ( Mouse.IsOver( rect ) )
            {
                if ( area != null )
                    area.MarkForDraw();
                if ( Widgets.ButtonInvisible( rect ) )
                {
                    SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
                    return !status;
                }
            }

            TooltipHandler.TipRegion( rect, text );
            return status;
        }

        // RimWorld.AreaAllowedGUI
        private static void DoAreaSelector( Rect rect, ref Area areaAllowed, Area area )
        {
            rect = rect.ContractedBy( 1f );
            GUI.DrawTexture( rect, area == null ? BaseContent.GreyTex : area.ColorTexture );
            Text.Anchor = TextAnchor.MiddleLeft;
            var text  = AreaUtility.AreaAllowedLabel_Area( area );
            var rect2 = rect;
            rect2.xMin += 3f;
            rect2.yMin += 2f;
            Widgets.Label( rect2, text );
            if ( areaAllowed == area ) Widgets.DrawBox( rect, 2 );
            if ( Mouse.IsOver( rect ) )
            {
                if ( area != null ) area.MarkForDraw();
                if ( Input.GetMouseButton( 0 ) &&
                     areaAllowed != area )
                {
                    areaAllowed = area;
                    SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
                }
            }

            TooltipHandler.TipRegion( rect, text );
        }
    }
}