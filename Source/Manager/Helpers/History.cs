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
    public class History : IExposable
    {
        // types
        public enum Period
        {
            Day,
            Month,
            Year
        }

        public static Color DefaultLineColor   = Color.white;
        private const int   Breaks             = 5;
        private const int   DashLength         = 3;
        private const float Margin             = Utilities.Margin;
        private const int   Size               = 100;

        // interval per period
        private static Dictionary<Period, int> _intervals   = new Dictionary<Period, int>();
        private static readonly Texture2D      _plotBG      = Resources.SlightlyDarkBackground;
        private static readonly float          _yAxisMargin = 25f;
        private static Period[]                periods      = (Period[])Enum.GetValues( typeof (Period) );

        // each chapter holds the history for all periods.
        private List<Chapter> _chapters = new List<Chapter>();
        private List<Chapter> _chaptersShown = new List<Chapter>();
        private Period        _periodShown = Period.Day;

        public History()
        {
            // for scribe?
        }

        public History( string[] labels, Color[] colors = null )
        {
            // get range of colors if not set
            if ( colors == null )
            {
                // default to white for single line
                if ( labels.Length == 1 )
                {
                    colors = new[] { Color.white };
                }

                // rainbow!
                else
                {
                    colors = HSV_Helper.Range( labels.Length );
                }
            }

            // create a chapter for each label
            for ( int i = 0; i < labels.Length; i++ )
            {
                _chapters.Add( new Chapter( labels[i], Size, colors[i % colors.Length] ) );
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.LookList( ref _chapters, "Chapter", LookMode.Deep );
        }

        public static int Interval( Period period )
        {
            if ( !_intervals.ContainsKey( period ) )
            {
                int ticks;
                switch ( period )
                {
                    case Period.Month:
                        ticks = GenDate.TicksPerMonth;
                        break;
                    case Period.Year:
                        ticks = GenDate.TicksPerYear;
                        break;
                    default:
                        ticks = GenDate.TicksPerDay;
                        break;
                }
                _intervals[period] = ticks / Size;
            }
            return _intervals[period];
        }

        public void Update( int[] counts )
        {
            if ( counts.Length != _chapters.Count )
            {
                Log.Warning( "History updated with incorrect number of chapters" );
            }

            for ( int i = 0; i < counts.Length; i++ )
            {
                _chapters[i].Add( counts[i] );
            }
        }

        public void Update( int count )
        {
            if ( _chapters.Count != 1 )
            {
                Log.Warning( "History updated with incorrect number of chapters" );
            }

            _chapters[0].Add( count );
        }

        public void DrawPlot( Rect rect, int target = 0, string label = "" )
        {
            // stuff we need
            Rect plot = rect.ContractedBy( Utilities.Margin );
            plot.xMin += _yAxisMargin;

            // maximum of all chapters.
            int max = Math.Max( _chapters.Select( c => c.Max( _periodShown ) ).Max(), (int)( target * 1.2 ) );

            // size, and pixels per node.
            float w  = plot.width;
            float h  = plot.height;
            float wu = w / Size; // width per section
            float hu = h / max; // height per count
            int   bi = max / ( Breaks + 1 ); // count per break
            float bu = hu * bi; // height per break

            // plot the line(s)
            GUI.DrawTexture( plot, _plotBG );
            GUI.BeginGroup( plot );
            foreach ( Chapter chapter in _chapters )
            {
                if ( chapter._hist[_periodShown].Count > 1 )
                {
                    List<int> hist = chapter._hist[_periodShown];
                    for ( int i = 0; i < hist.Count - 1; i++ ) // line segments, so up till n-1
                    {
                        Vector2 start = new Vector2( wu * i, h - hu * hist[i] );
                        Vector2 end = new Vector2( wu * ( i + 1 ), h - hu * hist[i + 1] );
                        Widgets.DrawLine( start, end, chapter.lineColor, 1f );
                    }
                }
            }

            // draw target line
            GUI.color = Color.gray;
            for ( int i = 0; i < plot.width / DashLength; i += 2 )
            {
                Widgets.DrawLineHorizontal( i * DashLength, plot.height - target * hu, DashLength );
            }
            GUI.EndGroup();

            // plot axis
            GUI.BeginGroup( rect );
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;

            // draw ticks + labels
            for ( int i = 1; i < Breaks + 1; i++ )
            {
                Widgets.DrawLineHorizontal( _yAxisMargin + Margin / 2, plot.height - i * bu, Margin );
                Rect labRect = new Rect( 0f, plot.height - i * bu - 4f, _yAxisMargin, 20f );
                Widgets.Label( labRect, ( i * bi ).ToString() );
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // period / variables picker
            Rect switchRect = new Rect( rect.xMax - Utilities.SmallIconSize - Utilities.Margin,
                                        rect.yMin + Utilities.Margin, Utilities.SmallIconSize, Utilities.SmallIconSize );
            Widgets.DrawHighlightIfMouseover( switchRect );
            if ( Widgets.ImageButton( switchRect, Resources.Cog ) )
            {
                List<FloatMenuOption> options =
                    periods.Select( p => new FloatMenuOption( p.ToString(), delegate { _periodShown = p; } ) ).ToList();
                Find.WindowStack.Add( new FloatMenu( options ) );
            }

            GUI.EndGroup();
        }

        public class Chapter : IExposable
        {
            public Dictionary<Period, List<int>> _hist;
            public string                        label;
            public Color                         lineColor;
            public int                           size;

            public Chapter( string label, int size, Color color )
            {
                this.label = label;
                this.size  = size;
                lineColor  = color;

                // create a dictionary of histories, one for each period.
                _hist = periods.ToDictionary( k => k, v => new List<int>() );
            }

            public void ExposeData()
            {
                Scribe_Values.LookValue( ref label, "label" );
                Scribe_Values.LookValue( ref size, "size", 100 );

                // TODO: custom class for scribing an array really necessary?
                // Scribe_Collections.LookDictionary( ref _hist, "pages", LookMode.Value, LookMode.Value);
            }

            public void Add( int count )
            {
                int curTick = Find.TickManager.TicksGame;
                foreach ( Period period in periods )
                {
                    if ( curTick % Interval( period ) == 0 )
                    {
                        _hist[period].Add( count );

                        // cull the list back down to size.
                        while ( _hist[period].Count > Size )
                        {
                            _hist[period].RemoveAt( 0 );
                        }
                    }
                }
            }

            public int Max( Period period )
            {
                return _hist[period].Max();
            }
        }
    }
}