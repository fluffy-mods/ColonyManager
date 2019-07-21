// Widgets_Section.cs
// Copyright Karel Kroeze, 2017-2017

using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public static class Widgets_Section
    {
        private static readonly Dictionary<string, float>   _columnHeights         = new Dictionary<string, float>();
        private static readonly Dictionary<string, Vector2> _columnScrollPositions = new Dictionary<string, Vector2>();

        private static readonly Dictionary<int, float> _heights = new Dictionary<int, float>();

        public static void BeginSectionColumn( Rect canvas, string identifier, out Vector2 position, out float width )
        {
            var height         = GetHeight( identifier );
            var scrollPosition = GetScrollPosition( identifier );
            var outRect        = canvas.ContractedBy( Margin ).RoundToInt();
            var viewRect       = new Rect( outRect.xMin, outRect.yMin, outRect.width, height );
            if ( viewRect.height > outRect.height )
                viewRect.width -= ScrollbarWidth + Margin / 2f;
            viewRect = viewRect.RoundToInt();

            Widgets.BeginScrollView( outRect, ref scrollPosition, viewRect );
            GUI.BeginGroup( viewRect );
            viewRect = viewRect.AtZero();

            position = Vector2.zero;
            width    = viewRect.width;

            _columnScrollPositions[identifier] = scrollPosition;
        }

        public static void EndSectionColumn( string identifier, Vector2 position )
        {
            GUI.EndGroup();
            Widgets.EndScrollView();

            _columnHeights[identifier] = position.y;
        }

        private static float GetHeight( string identifier )
        {
            float height;
            if ( _columnHeights.TryGetValue( identifier, out height ) )
                return height;

            height                     = 0f;
            _columnHeights[identifier] = height;
            return height;
        }

        private static Vector2 GetScrollPosition( string identifier )
        {
            Vector2 scrollposition;
            if ( _columnScrollPositions.TryGetValue( identifier, out scrollposition ) )
                return scrollposition;

            scrollposition                     = Vector2.zero;
            _columnScrollPositions[identifier] = scrollposition;
            return scrollposition;
        }

        public static void Section( ref Vector2 position, float width, Func<Vector2, float, float> drawerFunc,
                                    string header = null, int id = 0 )
        {
            var hasHeader = !header.NullOrEmpty();
            id = id != 0 ? id : drawerFunc.GetHashCode();

            // header
            if ( hasHeader )
            {
                var headerRect = new Rect( position.x, position.y, width, SectionHeaderHeight ).RoundToInt();
                Widgets_Labels.Label( headerRect, header, TextAnchor.LowerLeft, GameFont.Tiny, margin: 3 * Margin );
                position.y += SectionHeaderHeight;
            }

            // draw content
            var contentRect = new Rect(
                position.x,
                position.y,
                width,
                GetHeight( id ) + 2 * Margin ).RoundToInt();

            // NOTE: we're updating height _after_ drawing, so the background is technically always one frame behind.
            GUI.DrawTexture( contentRect, Resources.SlightlyDarkBackground );
            var height = drawerFunc( position + new Vector2( Margin, Margin ), width - 2 * Margin );
            position.y   += height + 3 * Margin;
            _heights[id] =  height;
        }

        private static float GetHeight( int id )
        {
            float height;
            _heights.TryGetValue( id, out height );
            return height;
        }
    }
}