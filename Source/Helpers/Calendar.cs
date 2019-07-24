// Calendar.cs
// Copyright Karel Kroeze, -2019

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class CalendarMarker
    {
        public CalendarMarker( float days, Color color, bool fill, bool debug = false )
        {
            Days  = days;
            Color = color;
            Fill  = fill;
            Debug = debug;
        }

        public float Days  { get; }
        public Color Color { get; }
        public bool  Fill  { get; }

        public bool Debug { get; }
    }

    public static class Calendar
    {
        private static readonly Dictionary<string, int> _sizeCache = new Dictionary<string, int>();

        public static void Draw( Rect canvas, params CalendarMarker[] markers )
        {
            Draw( canvas, Resources.SlightlyDarkBackgroundColour, markers );
        }

        public static void Draw( Rect canvas, Color color, CalendarMarker[] markers )
        {
            var days = markers.NullOrEmpty()
                ? GenDate.DaysPerTwelfth
                : Mathf.CeilToInt( markers.Max( m => m.Days ) / GenDate.DaysPerTwelfth ) * GenDate.DaysPerTwelfth;

            var size = SquareSize( canvas.width, canvas.height, days );
            var cols = Mathf.FloorToInt( canvas.width / size );

            for ( var d = 0; d < days; d++ )
            {
                // draw day background 
                DrawDay( d % cols, d / cols, size, canvas.min, 1f, color );

                foreach ( var marker in markers )
                {
                    if ( marker.Days < d )
                        continue;

                    if ( marker.Fill )
                        DrawDay( d % cols, d / cols, size, canvas.min, marker.Days - d, marker.Color );

                    if ( !marker.Fill && marker.Days > d && marker.Days <= d + 1 )
                        DrawMarker( d % cols, d / cols, size, canvas.min, marker.Days - d, marker.Color );
                }
            }
        }

        private static void DrawDay( int col, int row, int size, Vector2 pos, float progress, Color color )
        {
            var canvas = new Rect( (int) ( col * size + pos.x ),
                                   (int) ( row * size + pos.y ),
                                   Mathf.Clamp01( progress ) * ( size - 1 ),
                                   size - 1 );
            Widgets.DrawBoxSolid( canvas, color );
        }

        private static void DrawMarker( int col, int row, int size, Vector2 pos, float progress, Color color )
        {
            var start = new Vector2( ( col + Mathf.Clamp01( progress ) ) * size, row         * size - 2 ) + pos;
            var end   = new Vector2( ( col + Mathf.Clamp01( progress ) ) * size, ( row + 1 ) * size + 1 ) + pos;
            Widgets.DrawLine( start, end, color, 1 );
        }

        //https://math.stackexchange.com/a/466248/176741
        private static int SquareSize( float x, float y, int n )
        {
            var key = $"x:{x:F3}, y:{y:F3}, n:{n}";
            if ( _sizeCache.TryGetValue( key, out var size ) )
                return size;

            float sx, sy;

            var px = Mathf.CeilToInt( Mathf.Sqrt( n * x / y ) );
            if ( Mathf.Floor( px                 * y / x ) * px < n )
                sx = y / Mathf.CeilToInt( px * y / x );
            else
                sx = x / px;

            var py = Mathf.Ceil( Mathf.Sqrt( n * y / x ) );
            if ( Mathf.Floor( py                 * x / y ) * py < n )
                sy = x / Mathf.CeilToInt( x * py / y );
            else
                sy = y / py;

            size = (int) Mathf.Max( sx, sy );
            _sizeCache.Add( key, size );
            return size;
        }
    }
}