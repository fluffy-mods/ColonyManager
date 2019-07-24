// Clock.cs
// Copyright Karel Kroeze, 2019-2019

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class ClockHandle : HourTick
    {
        public ClockHandle( float hours, Color? color = null, float length = 1f, float thickness = 1.5f ) : base(
            color ?? Color.white, length, thickness )
        {
            Hours = hours;
        }

        public float Hours { get; }
    }

    public class HourTick
    {
        public HourTick( Color? color = null, float length = .2f, float thickness = 1 )
        {
            Length    = length;
            Thickness = thickness;
            Color     = color ?? Color.grey;
        }

        public float Length    { get; }
        public float Thickness { get; }
        public Color Color     { get; }
    }

    public static class Clock
    {
        public static void Draw( Rect canvas, params ClockHandle[] clockHandles )
        {
            Draw( canvas, clockHandles, new HourTick( length: .3f ), new HourTick() );
        }

        public static void Draw( Rect canvas, IEnumerable<ClockHandle> clockHandles, HourTick major, HourTick minor )
        {
            for ( var h = 0; h < 12; h++ )
                if ( h % 3 == 0 )
                {
                    if ( major != null ) DrawTick( canvas, major, h );
                }
                else
                {
                    if ( minor != null ) DrawTick( canvas, minor, h );
                }

            foreach ( var handle in clockHandles ) DrawHandle( canvas, handle );
        }

        public static void DrawHandle( Rect canvas, ClockHandle handle )
        {
            DrawMarker( canvas, handle.Hours, handle.Thickness, handle.Color, 0f, handle.Length );
        }

        public static void DrawTick( Rect canvas, HourTick tick, float hour )
        {
            DrawMarker( canvas, hour, tick.Thickness, tick.Color, 1f - tick.Length, 1f );
        }

        public static void DrawMarker( Rect canvas, float hour, float thickness, Color color, float start, float end )
        {
            var angle  = ( hour / 6 - .5f )                       * Mathf.PI; // should start at top...
            var radius = Mathf.Min( canvas.width, canvas.height ) / 2f;
            var vector = new Vector2( Mathf.Cos( angle ), Mathf.Sin( angle ) );
            var from   = radius * start * vector + canvas.center;
            var to     = radius * end   * vector + canvas.center;
            Logger.Debug( $"{canvas}, {from}, {to}" );
            Widgets.DrawLine( from, to, color, thickness );
        }
    }
}