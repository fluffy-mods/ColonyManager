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
        public static void Section( ref Vector2 position, float width, Func<Vector2, float, float> drawerFunc, string header = null, int id = 0 )
        {
            bool hasHeader = !header.NullOrEmpty();
            id = id != 0 ? id : drawerFunc.GetHashCode();

            // header
            if ( hasHeader )
            {
                Rect headerRect = new Rect( position.x, position.y, width, SectionHeaderHeight );
                Widgets_Labels.Label( headerRect, header, TextAnchor.LowerLeft, GameFont.Tiny, margin: 3 * Margin );
                position.y += SectionHeaderHeight;
            }

            // draw content
            Rect contentRect = new Rect(
                position.x,
                position.y,
                width,
                GetHeight( id ) + 2 * Margin );

            // NOTE: we're updating height _after_ drawing, so the background is technically always one frame behind.
            GUI.DrawTexture( contentRect, Resources.SlightlyDarkBackground );
            var height = drawerFunc( position + new Vector2( Margin, Margin ), width - 2 * Margin );
            position.y += height + 3 * Margin;
            _heights[id] = height;
        }

        private static Dictionary<int, float> _heights = new Dictionary<int, float>();
        private static float GetHeight( int id )
        {
            float height;
            _heights.TryGetValue( id, out height );
            return height;
        }
    }
}