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

        public static Color DefaultLineColor = Color.white;
        private const int Breaks = 5;
        private const int DashLength = 3;
        private const float Margin = Utilities.Margin;
        private const int Size = 100;

        // settings
        private bool AllowTogglingLegend = true;
        private bool ShowLegend = true;
        private bool DrawTargetLine = true;

        // interval per period
        private static Dictionary<Period, int> _intervals = new Dictionary<Period, int>();
        private static readonly Texture2D _plotBG = Resources.SlightlyDarkBackground;
        private static readonly float _yAxisMargin = 25f;
        private static Period[] periods = (Period[])Enum.GetValues( typeof (Period) );

        // each chapter holds the history for all periods.
        private List<Chapter> _chapters = new List<Chapter>();
        private List<Chapter> _chaptersShown = new List<Chapter>();
        private Period _periodShown = Period.Day;

        // for scribe.
        public History( string[] labels ) : this( labels, null ) {}

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
            // settings
            Scribe_Values.LookValue( ref AllowTogglingLegend, "AllowToggingLegend", true );
            Scribe_Values.LookValue( ref ShowLegend, "ShowLegend", true );
            Scribe_Values.LookValue( ref DrawTargetLine, "DrawTargetLine", true );

            // history chapters
            Scribe_Collections.LookList( ref _chapters, "Chapters", LookMode.Deep );
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

        public void Update( params int[] counts )
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

        public void DrawPlot( Rect rect, int target = 0, string label = "", List<Chapter> chapters = null, int sign = 1 )
        {
            // if chapters is left default, plot all
            if ( chapters == null ) chapters = _chapters;

            // stuff we need
            Rect plot = rect.ContractedBy( Utilities.Margin );
            plot.xMin += _yAxisMargin;

            // maximum of all chapters.
            int max = Math.Max( chapters.Select( c => c.Max( _periodShown ) ).Max(), (int)( target * 1.2 ) );

            // size, and pixels per node.
            float w = plot.width;
            float h = plot.height;
            float wu = w / Size; // width per section
            float hu = h / max; // height per count
            int bi = max / ( Breaks + 1 ); // count per break
            float bu = hu * bi; // height per break

            // plot the line(s)
            GUI.DrawTexture( plot, _plotBG );
            GUI.BeginGroup( plot );
            foreach ( Chapter chapter in _chapters )
            {
                chapter.Plot( _periodShown, plot.AtZero(), wu, hu );
            }

            // handle mouseover events
            if ( Mouse.IsOver( plot.AtZero() ) )
            {
                // very conveniently this is the position within the current group.
                Vector2 pos = Event.current.mousePosition;
                Vector2 upos = new Vector2(pos.x / wu, (plot.height - pos.y) / hu);

                // get distances
                float[] distances = chapters.Select( c => Math.Abs( c.ValueAt( _periodShown, (int)upos.x, sign ) - upos.y ) ).ToArray();
                
                // get the minimum index
                float min = int.MaxValue;
                int minIndex = 0;
                for ( int i = 0; i < distances.Count(); i++ )
                {
                    if ( distances[i] < min )
                    {
                        minIndex = i;
                        min = distances[i];
                    }
                }

                // closest line
                Chapter closest = chapters[minIndex];

                // do minimum stuff.
                Vector2 realpos = new Vector2( pos.x, plot.height - closest.ValueAt( _periodShown, (int)upos.x, sign ) * hu );
                Rect blipRect = new Rect(realpos.x - Utilities.SmallIconSize / 2f, realpos.y - Utilities.SmallIconSize / 2f, Utilities.SmallIconSize, Utilities.SmallIconSize );
                GUI.color = closest.lineColor;
                GUI.DrawTexture( blipRect, Resources.StageB );
                GUI.color = DefaultLineColor;

                // get orientation of tooltip
                Vector2 tippos = realpos + new Vector2( Utilities.Margin, Utilities.Margin );
                string tip = chapters[minIndex].label + ": " + chapters[minIndex].ValueAt( _periodShown, (int)upos.x, sign );
                Vector2 tipsize = Text.CalcSize( tip );
                bool up = false, left = false;
                if ( tippos.x + tipsize.x > plot.width )
                {
                    left = true;
                    tippos.x -= tipsize.x + 2 * + Utilities.Margin;
                }
                if ( tippos.y + tipsize.y > plot.height )
                {
                    up = true;
                    tippos.y -= tipsize.y + 2 * Utilities.Margin;
                }

                TextAnchor anchor = TextAnchor.UpperLeft;
                if (up && left) anchor = TextAnchor.LowerRight;
                if ( up && !left ) anchor = TextAnchor.LowerLeft;
                if ( !up && left ) anchor = TextAnchor.UpperRight;
                Rect tooltipRect = new Rect( tippos.x, tippos.y, tipsize.x, tipsize.y );
                Utilities.Label( tooltipRect, tip, anchor: anchor, font: GameFont.Tiny );
            }

            // draw target line
            if ( DrawTargetLine )
            {
                GUI.color = Color.gray;
                for ( int i = 0; i < plot.width / DashLength; i += 2 )
                {
                    Widgets.DrawLineHorizontal( i * DashLength, plot.height - target * hu, DashLength );
                }
            }

            // draw legend
            int lineCount = _chapters.Count;
            if ( AllowTogglingLegend && lineCount > 1 && ShowLegend )
            {
                float rowHeight = 20f;
                float lineLength = 30f;
                float labelWidth = 100f;

                Vector2 cur = Vector2.zero;
                foreach ( Chapter chapter in _chapters )
                {
                    GUI.color = chapter.lineColor;
                    Widgets.DrawLineHorizontal(cur.x, cur.y + rowHeight / 2f, lineLength);
                    cur.x += lineLength;
                    Utilities.Label( ref cur, labelWidth, rowHeight, chapter.label, font: GameFont.Tiny );
                    cur.x = 0f;
                }
                GUI.color = Color.white;
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

            rect = rect.AtZero(); // ugh, I'm tired, just work.

            // period / variables picker
            Rect switchRect = new Rect( rect.xMax - Utilities.SmallIconSize - Utilities.Margin,
                                        rect.yMin + Utilities.Margin, Utilities.SmallIconSize, Utilities.SmallIconSize );

            Widgets.DrawHighlightIfMouseover( switchRect );
            if ( Widgets.ImageButton( switchRect, Resources.Cog ) )
            {
                List<FloatMenuOption> options =
                    periods.Select( p => new FloatMenuOption( "FM.HistoryPeriod".Translate() + ": " + p.ToString(), delegate { _periodShown = p; } ) ).ToList();
                if ( _chapters.Count > 1 ) // add option to show/hide legend if appropriate.
                {
                    options.Add( new FloatMenuOption( "FM.HistoryShowHideLegend".Translate(), delegate { ShowLegend = !ShowLegend; } ) );
                }
                Find.WindowStack.Add( new FloatMenu( options ) );
            }

            GUI.EndGroup();
        }

        public class ExposableList<T> : List<T>, IExposable
        {
            private List<T> _list = new List<T>();

            // get / set the items in here.
            private List<T> InnerList
            {
                get
                {
                    List<T> temp = new List<T>();
                    foreach ( T item in this )
                    {
                        temp.Add( item );
                    }
                    return temp;
                }
                set
                {
                    Clear();
                    foreach ( T item in value )
                    {
                        Add( item );
                    }
                }
            }

            // only provide an init constructor and a blank one.
            public ExposableList() {}
            public ExposableList( T init ) : base( new[] { init } ) {}

            public void ExposeData()
            {
                // get ready for saving
                if ( Scribe.mode == LoadSaveMode.Saving )
                {
                    _list = InnerList;
                }
                
                // the actual work 
                Scribe_Collections.LookList( ref _list, "List", LookMode.Value );
                
                // after loading raw data
                if ( Scribe.mode == LoadSaveMode.PostLoadInit )
                {
                    InnerList = _list;
                }
            }
        }

        public class Chapter : IExposable
        {
            public Dictionary<Period, ExposableList<int>> _hist     = new Dictionary<Period, ExposableList<int>>();
            public string                                 label     = String.Empty;
            public Color                                  lineColor = DefaultLineColor;
            public int                                    size      = Size;

            // scribe
            public Chapter() {}

            public Chapter( string label, int size, Color color )
            {
                this.label = label;
                this.size  = size;
                lineColor  = color;

                // create a dictionary of histories, one for each period, initialize with a zero to avoid errors.
                _hist = periods.ToDictionary( k => k, v => new ExposableList<int>( 0 ) );
            }

            public int ValueAt( Period period, int x , int sign = 1)
            {
                if ( x < 0 || x >= _hist[period].Count ) return -1;
                return _hist[period][x] * sign;
            }

            public void ExposeData()
            {
                Scribe_Values.LookValue( ref label, "label" );
                Scribe_Values.LookValue( ref size, "size", 100 );
                Scribe_Values.LookValue( ref lineColor, "color", Color.white );
                // TODO: NEXT MAJOR VERSION - CHANGE STORAGE TO BYTEARRAY
                Scribe_Collections.LookDictionary( ref _hist, "pages", LookMode.Value, LookMode.Deep );
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

            public void Plot( Period period, Rect canvas, float wu, float hu, int sign = 1 )
            {
                if( _hist[period].Count > 1 )
                {
                    List<int> hist = _hist[period];
                    for( int i = 0; i < hist.Count - 1; i++ ) // line segments, so up till n-1
                    {
                        Vector2 start = new Vector2( wu * i, canvas.height - hu * hist[i] * sign );
                        Vector2 end = new Vector2( wu * ( i + 1 ), canvas.height - hu * hist[i + 1] * sign );
                        Widgets.DrawLine( start, end, lineColor, 1f );
                    }
                }
            }
        }
    }
}