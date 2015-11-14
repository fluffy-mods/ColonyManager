// Manager/History.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:32

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FM
{
    public class History
    {
        // types
        public enum Period
        {
            Day,
            Month,
            Year
        }

        private const int Breaks = 5;
        private const int DashLength = 3;
        private const float Margin = Utilities.Margin;
        private List< int > _hist = new List< int > { 0 };

        // Period stuff
        private readonly Period _period;
        private readonly Texture2D _plotBG = SolidColorMaterials.NewSolidColorTexture( 0f, 0f, 0f, .2f );

        // start with a single entry to avoid issues with .Max()
        private int _ticksPerPeriod;
        private readonly float _yAxisMargin = 25f;

        // plotting stuff
        public Color LineCol = Color.white;

        public int[] Get
        {
            get { return _hist.ToArray(); }
        }

        public int Interval
        {
            get
            {
                if ( _ticksPerPeriod == 0 )
                {
                    int ticks;
                    switch ( _period )
                    {
                        case Period.Day:
                            ticks = GenDate.TicksPerDay;
                            break;

                        case Period.Month:
                            ticks = GenDate.TicksPerMonth;
                            break;

                        case Period.Year:
                        default:
                            ticks = GenDate.TicksPerYear;
                            break;
                    }
                    _ticksPerPeriod = ticks / Size;
                }
                return _ticksPerPeriod;
            }
        }

        // main cache
        public int Size { get; set; }

        public History( int size, Period period = Period.Day )
        {
            Size = size;
            _period = period;
        }

        public void Add( int x )
        {
            _hist.Add( x );

            while ( _hist.Count > Size )
            {
                _hist.RemoveAt( 0 );
            }
        }

        public void DrawPlot( Rect rect, int target = 0, string label = "" )
        {
            // stuff we need
            Rect plot = rect.ContractedBy( Utilities.Margin );
            plot.xMin += _yAxisMargin;
            int max = Math.Max( _hist.Max(), (int)( target * 1.2 ) );
            float w = plot.width;
            float h = plot.height;
            float wu = w / Size; // width per section
            float hu = h / max; // height per count
            int bi = max / ( Breaks + 1 ); // count per break
            float bu = hu * bi; // height per break

            // plot the line
            GUI.DrawTexture( plot, _plotBG );
            GUI.BeginGroup( plot );
            if ( _hist.Count > 1 )
            {
                for ( var i = 0; i < _hist.Count - 1; i++ ) // line segments, so up till n-1
                {
                    var start = new Vector2( wu * i, h - hu * _hist[i] );
                    var end = new Vector2( wu * ( i + 1 ), h - hu * _hist[i + 1] );
                    Widgets.DrawLine( start, end, LineCol, 1f );
                }
            }

            // draw target line
            GUI.color = Color.gray;
            for ( var i = 0; i < plot.width / DashLength; i += 2 )
            {
                Widgets.DrawLineHorizontal( i * DashLength, plot.height - target * hu, DashLength );
            }
            GUI.EndGroup();

            // plot axis
            GUI.BeginGroup( rect );
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;

            // draw ticks + labels
            for ( var i = 1; i < Breaks + 1; i++ )
            {
                Widgets.DrawLineHorizontal( _yAxisMargin + Margin / 2, plot.height - i * bu, Margin );
                var labRect = new Rect( 0f, plot.height - i * bu - 4f, _yAxisMargin, 20f );
                Widgets.Label( labRect, ( i * bi ).ToString() );
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            GUI.EndGroup();
        }
    }
}