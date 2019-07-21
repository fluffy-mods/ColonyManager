// Karel Kroeze
// History.cs
// 2016-12-09

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
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

        private const int Breaks     = 4;
        private const int DashLength = 3;
        private const int Size       = 100;

        public static Color DefaultLineColor = Color.white;

        // interval per period
        private static readonly Dictionary<Period, int> _intervals = new Dictionary<Period, int>();

        private static readonly float    _yAxisMargin = 40f;
        public static           Period[] periods      = (Period[]) Enum.GetValues( typeof( Period ) );

        // each chapter holds the history for all periods.
        private List<Chapter> _chapters = new List<Chapter>();

        private readonly List<Chapter> _chaptersShown = new List<Chapter>();

        // settings
        public bool AllowTogglingLegend = true;

        public bool DrawCounts = true;

        // for detailed legend
        public bool DrawIcons = true;

        public bool   DrawInfoInBar;
        public bool   DrawInlineLegend = true;
        public bool   DrawMaxMarkers;
        public bool   DrawOptions    = true;
        public bool   DrawTargetLine = true;
        public bool   MaxPerChapter;
        public Period periodShown = Period.Day;
        public string Suffix      = string.Empty;

        // for scribe.
        public History()
        {
        }

        public History( string[] labels, Color[] colors = null )
        {
#if DEBUG_HISTORY
            Log.Message( "History created" + string.Join( ", ", labels ) );
#endif
            // get range of colors if not set
            if ( colors == null )
            {
                // default to white for single line
                if ( labels.Length == 1 )
                    colors = new[] {Color.white};

                // rainbow!
                else
                    colors = HSV_Helper.Range( labels.Length );
            }

            // create a chapter for each label
            for ( var i = 0; i < labels.Length; i++ )
                _chapters.Add( new Chapter( labels[i], Size, colors[i % colors.Length] ) );

            // show all by default
            _chaptersShown.AddRange( _chapters );
        }

        public History( ThingDefCount[] thingCounts, Color[] colors = null )
        {
            // get range of colors if not set
            if ( colors == null )
            {
                // default to white for single line
                if ( thingCounts.Length == 1 )
                    colors = new[] {Color.white};

                // rainbow!
                else
                    colors = HSV_Helper.Range( thingCounts.Length );
            }

            // create a chapter for each label
            for ( var i = 0; i < thingCounts.Length; i++ )
                _chapters.Add( new Chapter( new ThingDefCountClass( thingCounts[i].ThingDef, thingCounts[i].Count ),
                                            Size,
                                            colors[i % colors.Length] ) );

            // show all by default
            _chaptersShown.AddRange( _chapters );
        }

        public bool IsRelevantTick
        {
            get { return periods.Any( p => Find.TickManager.TicksGame % Interval( p ) == 0 ); }
        }

        public void ExposeData()
        {
            // settings
            Scribe_Values.Look( ref AllowTogglingLegend, "AllowToggingLegend", true );
            Scribe_Values.Look( ref DrawInlineLegend, "ShowLegend", true );
            Scribe_Values.Look( ref DrawTargetLine, "DrawTargetLine", true );
            Scribe_Values.Look( ref DrawOptions, "DrawOptions", true );
            Scribe_Values.Look( ref Suffix, "Suffix", "" );
            Scribe_Values.Look( ref DrawIcons, "DrawIcons", true );
            Scribe_Values.Look( ref DrawCounts, "DrawCounts", true );
            Scribe_Values.Look( ref DrawInfoInBar, "DrawInfoInBar" );
            Scribe_Values.Look( ref DrawMaxMarkers, "DrawMaxMarkers", true );
            Scribe_Values.Look( ref MaxPerChapter, "MaxPerChapter" );

            // history chapters
            Scribe_Collections.Look( ref _chapters, "Chapters", LookMode.Deep );

            // some post load tweaks
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                // set chapters shown to the newly loaded chapters (instead of the default created empty chapters).
                _chaptersShown.Clear();
                _chaptersShown.AddRange( _chapters );
            }
        }

        public static int Interval( Period period )
        {
            if ( !_intervals.ContainsKey( period ) )
            {
                int ticks;
                switch ( period )
                {
                    case Period.Month:
                        ticks = GenDate.TicksPerTwelfth;
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

        /// <summary>
        ///     Round up to given precision
        /// </summary>
        /// <param name="x">input</param>
        /// <param name="precision">number of digits to preserve past the magnitude, should be equal to or greater than zero.</param>
        /// <returns></returns>
        public int CeilToPrecision( float x, int precision = 1 )
        {
            var magnitude = Mathf.FloorToInt( Mathf.Log10( x ) );
            var unit      = Mathf.FloorToInt( Mathf.Pow( 10, magnitude - precision ) );
            return Mathf.CeilToInt( x / unit ) * unit;
        }

        public string FormatCount( float x, int unit = 1000, string[] suffixes = null )
        {
            if ( suffixes == null )
                suffixes = new[] {"", "k", "M", "G"};
            var i = 0;
            while ( x > unit && i < suffixes.Length )
            {
                x /= unit;
                i++;
            }

            return x.ToString( "0.#" + suffixes[i] + Suffix );
        }

        public void Update( params int[] counts )
        {
            if ( counts.Length != _chapters.Count ) Log.Warning( "History updated with incorrect number of chapters" );

            for ( var i = 0; i < counts.Length; i++ ) _chapters[i].Add( counts[i] );
        }

        public void UpdateThingCounts( params int[] counts )
        {
            if ( counts.Length != _chapters.Count ) Log.Warning( "History updated with incorrect number of chapters" );

            for ( var i = 0; i < counts.Length; i++ ) _chapters[i].ThingDefCount.count = counts[i];
        }

        public void UpdateMax( params int[] maxes )
        {
            if ( maxes.Length != _chapters.Count ) Log.Warning( "History updated with incorrect number of chapters" );

            for ( var i = 0; i < maxes.Length; i++ ) _chapters[i].TrueMax = maxes[i];
        }

        public void UpdateThingCountAndMax( int[] counts, int[] maxes )
        {
            if ( maxes.Length != _chapters.Count || maxes.Length != _chapters.Count )
                Log.Warning( "History updated with incorrect number of chapters" );

            for ( var i = 0; i < maxes.Length; i++ )
                if ( _chapters[i].ThingDefCount.count != counts[i] )
                {
                    _chapters[i].TrueMax             = maxes[i];
                    _chapters[i].ThingDefCount.count = counts[i];
                }
        }

        public void DrawPlot( Rect rect, int target = 0, string label = "", bool positiveOnly = false,
                              bool negativeOnly = false )
        {
            // set sign
            var sign = negativeOnly ? -1 : 1;

            // subset chapters
            var chapters =
                _chaptersShown.Where( chapter => !positiveOnly || chapter.pages[periodShown].Any( i => i > 0 ) )
                              .Where( chapter => !negativeOnly || chapter.pages[periodShown].Any( i => i < 0 ) )
                              .ToList();

            // get out early if no chapters.
            if ( chapters.Count == 0 )
            {
                GUI.DrawTexture( rect.ContractedBy( Margin ), Resources.SlightlyDarkBackground );
                Widgets_Labels.Label( rect, "FM.HistoryNoChapters".Translate(), TextAnchor.MiddleCenter,
                                      color: Color.grey );
                return;
            }

            // stuff we need
            var plot = rect.ContractedBy( Margin );
            plot.xMin += _yAxisMargin;

            // maximum of all chapters.
            var max =
                CeilToPrecision( Math.Max( chapters.Select( c => c.Max( periodShown, !negativeOnly ) ).Max(), target ) *
                                 1.2f );

            // size, and pixels per node.
            var w  = plot.width;
            var h  = plot.height;
            var wu = w   / Size;           // width per section
            var hu = h   / max;            // height per count
            var bi = max / ( Breaks + 1 ); // count per break
            var bu = hu  * bi;             // height per break

            // plot the line(s)
            GUI.DrawTexture( plot, Resources.SlightlyDarkBackground );
            GUI.BeginGroup( plot );
            foreach ( var chapter in chapters ) chapter.Plot( periodShown, plot.AtZero(), wu, hu, sign );

            // handle mouseover events
            if ( Mouse.IsOver( plot.AtZero() ) )
            {
                // very conveniently this is the position within the current group.
                var pos  = Event.current.mousePosition;
                var upos = new Vector2( pos.x / wu, ( plot.height - pos.y ) / hu );

                // get distances
                var distances =
                    chapters.Select( c => Math.Abs( c.ValueAt( periodShown, (int) upos.x, sign ) - upos.y ) ).ToArray();

                // get the minimum index
                float min      = int.MaxValue;
                var   minIndex = 0;
                for ( var i = 0; i < distances.Count(); i++ )
                    if ( distances[i] < min )
                    {
                        minIndex = i;
                        min      = distances[i];
                    }

                // closest line
                var closest = chapters[minIndex];

                // do minimum stuff.
                var realpos =
                    new Vector2( pos.x, plot.height - closest.ValueAt( periodShown, (int) upos.x, sign ) * hu );
                var blipRect = new Rect( realpos.x - SmallIconSize / 2f,
                                         realpos.y - SmallIconSize / 2f, SmallIconSize,
                                         SmallIconSize );
                GUI.color = closest.lineColor;
                GUI.DrawTexture( blipRect, Resources.StageB );
                GUI.color = DefaultLineColor;

                // get orientation of tooltip
                var tippos = realpos + new Vector2( Margin, Margin );
                var tip = chapters[minIndex].label + ": " +
                          FormatCount( chapters[minIndex].ValueAt( periodShown, (int) upos.x, sign ) );
                var  tipsize = Text.CalcSize( tip );
                bool up      = false, left = false;
                if ( tippos.x + tipsize.x > plot.width )
                {
                    left     =  true;
                    tippos.x -= tipsize.x + 2 * +Margin;
                }

                if ( tippos.y + tipsize.y > plot.height )
                {
                    up       =  true;
                    tippos.y -= tipsize.y + 2 * Margin;
                }

                var anchor = TextAnchor.UpperLeft;
                if ( up && left )
                    anchor = TextAnchor.LowerRight;
                if ( up && !left )
                    anchor = TextAnchor.LowerLeft;
                if ( !up && left )
                    anchor = TextAnchor.UpperRight;
                var tooltipRect = new Rect( tippos.x, tippos.y, tipsize.x, tipsize.y );
                Widgets_Labels.Label( tooltipRect, tip, anchor, GameFont.Tiny );
            }

            // draw target line
            if ( DrawTargetLine )
            {
                GUI.color = Color.gray;
                for ( var i = 0; i < plot.width / DashLength; i += 2 )
                    Widgets.DrawLineHorizontal( i * DashLength, plot.height - target * hu, DashLength );
            }

            // draw legend
            var lineCount = _chapters.Count;
            if ( AllowTogglingLegend && lineCount > 1 && DrawInlineLegend )
            {
                var rowHeight  = 20f;
                var lineLength = 30f;
                var labelWidth = 100f;

                var cur = Vector2.zero;
                foreach ( var chapter in _chapters )
                {
                    GUI.color = chapter.lineColor;
                    Widgets.DrawLineHorizontal( cur.x, cur.y + rowHeight / 2f, lineLength );
                    cur.x += lineLength;
                    Widgets_Labels.Label( ref cur, labelWidth, rowHeight, chapter.label, font: GameFont.Tiny );
                    cur.x = 0f;
                }

                GUI.color = Color.white;
            }

            GUI.EndGroup();

            // plot axis
            GUI.BeginGroup( rect );
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font   = GameFont.Tiny;

            // draw ticks + labels
            for ( var i = 1; i < Breaks + 1; i++ )
            {
                Widgets.DrawLineHorizontal( _yAxisMargin + Margin / 2, plot.height - i * bu, Margin );
                var labRect = new Rect( 0f, plot.height - i * bu - 4f, _yAxisMargin, 20f );
                Widgets.Label( labRect, FormatCount( i * bi ) );
            }

            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color   = Color.white;

            rect = rect.AtZero(); // ugh, I'm tired, just work.

            // period / variables picker
            if ( DrawOptions )
            {
                var switchRect = new Rect( rect.xMax - SmallIconSize - Margin,
                                           rect.yMin                 + Margin, SmallIconSize,
                                           SmallIconSize );

                Widgets.DrawHighlightIfMouseover( switchRect );
                if ( Widgets.ButtonImage( switchRect, Resources.Cog ) )
                {
                    var options =
                        periods.Select(
                            p =>
                                new FloatMenuOption( "FM.HistoryPeriod".Translate() + ": " + p.ToString(),
                                                     delegate { periodShown = p; } ) ).ToList();
                    if ( AllowTogglingLegend && _chapters.Count > 1 ) // add option to show/hide legend if appropriate.
                        options.Add( new FloatMenuOption( "FM.HistoryShowHideLegend".Translate(),
                                                          delegate { DrawInlineLegend = !DrawInlineLegend; } ) );
                    Find.WindowStack.Add( new FloatMenu( options ) );
                }
            }

            GUI.EndGroup();
        }

        public void DrawDetailedLegend( Rect canvas, ref Vector2 scrollPos, int? max, bool positiveOnly = false,
                                        bool negativeOnly = false )
        {
            // set sign
            var sign = negativeOnly ? -1 : 1;

            var ChaptersOrdered = _chapters
                                 .Where( chapter => !positiveOnly || chapter.pages[periodShown].Any( i => i > 0 ) )
                                 .Where( chapter => !negativeOnly || chapter.pages[periodShown].Any( i => i < 0 ) )
                                 .OrderByDescending( chapter => chapter.Last( periodShown ) * sign ).ToList();

            // get out early if no chapters.
            if ( ChaptersOrdered.Count == 0 )
            {
                GUI.DrawTexture( canvas.ContractedBy( Margin ), Resources.SlightlyDarkBackground );
                Widgets_Labels.Label( canvas, "FM.HistoryNoChapters".Translate(), TextAnchor.MiddleCenter,
                                      color: Color.grey );
                return;
            }

            // max
            float _max = max ?? ( DrawMaxMarkers
                             ? ChaptersOrdered.Max( chapter => chapter.TrueMax )
                             : ChaptersOrdered.FirstOrDefault()?.Last( periodShown ) * sign )
                      ?? 0;

            // cell height
            var height    = 30f;
            var barHeight = 18f;

            // n rows
            var n = ChaptersOrdered.Count;

            // scrolling region
            var viewRect = canvas;
            viewRect.height = n * height;
            if ( viewRect.height > canvas.height )
            {
                viewRect.width -= 16f + Margin;
                canvas.width   -= Margin;
                canvas.height  -= 1f;
            }

            Widgets.BeginScrollView( canvas, ref scrollPos, viewRect );
            for ( var i = 0; i < n; i++ )
            {
                // set up rects
                var row = new Rect( 0f, height * i, viewRect.width,
                                     height );
                var icon = new Rect( Margin, height * i, height, height ).ContractedBy( Margin / 2f );
                // icon is square, size defined by height.
                var bar = new Rect( Margin + height, height * i, viewRect.width - height - Margin,
                                    height );

                // if icons should not be drawn make the bar full size.
                if ( !DrawIcons )
                    bar.xMin -= height + Margin;

                // bar details.
                var barBox   = bar.ContractedBy( ( height - barHeight ) / 2f );
                var barFill  = barBox.ContractedBy( 2f );
                var maxWidth = barFill.width;
                if ( MaxPerChapter )
                    barFill.width *= ChaptersOrdered[i].Last( periodShown ) * sign / (float) ChaptersOrdered[i].TrueMax;
                else
                    barFill.width *= ChaptersOrdered[i].Last( periodShown ) * sign / _max;

                GUI.BeginGroup( viewRect );

                // if DrawIcons and a thing is set, draw the icon.
                var thing = ChaptersOrdered[i].ThingDefCount.thingDef;
                if ( DrawIcons && thing != null )
                {
                    // draw the icon in correct proportions
                    var proportion = GenUI.IconDrawScale( thing );
                    Widgets.DrawTextureFitted( icon, thing.uiIcon, proportion );

                    // draw counts in upper left corner
                    if ( DrawCounts )
                        Utilities.LabelOutline( icon, ChaptersOrdered[i].ThingDefCount.count.ToString(), null,
                                                TextAnchor.UpperLeft, 0f, GameFont.Tiny, Color.white, Color.black );
                }

                // if desired, draw ghost bar
                if ( DrawMaxMarkers )
                {
                    var ghostBarFill = barFill;
                    ghostBarFill.width = MaxPerChapter ? maxWidth : maxWidth * ( ChaptersOrdered[i].TrueMax / _max );
                    GUI.color          = new Color( 1f, 1f, 1f, .2f );
                    GUI.DrawTexture( ghostBarFill, ChaptersOrdered[i].Texture ); // coloured texture
                    GUI.color = Color.white;
                }

                // draw the main bar.
                GUI.DrawTexture( barBox, Resources.SlightlyDarkBackground );
                GUI.DrawTexture( barFill, ChaptersOrdered[i].Texture ); // coloured texture
                GUI.DrawTexture( barFill, Resources.BarShader );        // slightly fancy overlay (emboss).

                // draw on bar info
                if ( DrawInfoInBar )
                {
                    var info = ChaptersOrdered[i].label + ": " +
                               FormatCount( ChaptersOrdered[i].Last( periodShown ) * sign );

                    if ( DrawMaxMarkers ) info += " / " + FormatCount( ChaptersOrdered[i].TrueMax );

                    // offset label a bit downwards and to the right
                    var rowInfoRect = row;
                    rowInfoRect.y += 3f;
                    rowInfoRect.x += Margin * 2;

                    // x offset
                    var xOffset = DrawIcons && thing != null ? height + Margin * 2 : Margin * 2;

                    Utilities.LabelOutline( rowInfoRect, info, null, TextAnchor.MiddleLeft, xOffset, GameFont.Tiny,
                                            Color.white, Color.black );
                }

                // are we currently showing this line?
                var shown = _chaptersShown.Contains( ChaptersOrdered[i] );

                // tooltip on entire row
                var tooltip = ChaptersOrdered[i].label + ": " +
                              FormatCount( Mathf.Abs( ChaptersOrdered[i].Last( periodShown ) ) );
                tooltip += "FM.HistoryClickToEnable".Translate( shown ? "hide" : "show", ChaptersOrdered[i].label );
                TooltipHandler.TipRegion( row, tooltip );

                // handle input
                if ( Widgets.ButtonInvisible( row ) )
                {
                    if ( Event.current.button == 0 )
                    {
                        if ( shown )
                            _chaptersShown.Remove( ChaptersOrdered[i] );
                        else
                            _chaptersShown.Add( ChaptersOrdered[i] );
                    }
                    else if ( Event.current.button == 1 )
                    {
                        _chaptersShown.Clear();
                        _chaptersShown.Add( ChaptersOrdered[i] );
                    }
                }

                // UI feedback for disabled row
                if ( !shown ) GUI.DrawTexture( row, Resources.SlightlyDarkBackground );

                GUI.EndGroup();
            }

            Widgets.EndScrollView();
        }

        public class Chapter : IExposable
        {
            private int                           _observedMax = -1;
            private int                           _specificMax = -1;
            public  Texture2D                     _texture;
            public  string                        label         = string.Empty;
            public  Color                         lineColor     = DefaultLineColor;
            public  Dictionary<Period, List<int>> pages         = new Dictionary<Period, List<int>>();
            public  int                           size          = Size;
            public  ThingDefCountClass            ThingDefCount = new ThingDefCountClass();

            public Chapter()
            {
                // empty for scribe.
                // create a dictionary of histories, one for each period, initialize with a zero to avoid errors.
                pages = periods.ToDictionary( k => k, v => new List<int>( new[] {0} ) );
            }

            public Chapter( string label, int size, Color color ) : this()
            {
                this.label = label;
                this.size  = size;
                lineColor  = color;
            }

            public Chapter( ThingDefCountClass thingDefCount, int size, Color color ) : this()
            {
                label         = thingDefCount.thingDef.LabelCap;
                ThingDefCount = thingDefCount;
                this.size     = size;
                lineColor     = color;
            }

            public Texture2D Texture
            {
                get
                {
                    if ( _texture == null ) _texture = SolidColorMaterials.NewSolidColorTexture( lineColor );
                    return _texture;
                }
            }

            public int TrueMax
            {
                get => Mathf.Max( _observedMax, _specificMax, 1 );
                set
                {
                    _observedMax = value != 0 ? value : Max( Period.Day );
                    _specificMax = value;
                }
            }

            public void ExposeData()
            {
                Scribe_Values.Look( ref label, "label" );
                Scribe_Values.Look( ref size, "size", 100 );
                Scribe_Values.Look( ref lineColor, "color", Color.white );
                Scribe_Values.Look( ref ThingDefCount.count, "thingCount_count" );
                Scribe_Defs.Look( ref ThingDefCount.thingDef, "thingCount_def" );

                var periods = new List<Period>( pages.Keys );
                foreach ( var period in periods )
                {
                    var values = pages[period];
                    Utilities.Scribe_IntArray( ref values, period.ToString() );

#if DEBUG_SCRIBE
                    Log.Message( Scribe.mode + " for " + label + ", daycount: " + pages[Period.Day].Count );
#endif

                    pages[period] = values;
                }
            }

            public bool Active( Period period )
            {
                return pages[period].Any( v => v > 0 );
            }

            public int Last( Period period )
            {
                return pages[period].Last();
            }

            public int ValueAt( Period period, int x, int sign = 1 )
            {
                if ( x < 0 || x >= pages[period].Count )
                    return -1;

                return pages[period][x] * sign;
            }

            public void Add( int count )
            {
                var curTick = Find.TickManager.TicksGame;
                foreach ( var period in periods )
                    if ( curTick % Interval( period ) == 0 )
                    {
                        pages[period].Add( count );
                        if ( Mathf.Abs( count ) > _observedMax )
                            _observedMax = Mathf.Abs( count );

                        // cull the list back down to size.
                        while ( pages[period].Count > Size ) pages[period].RemoveAt( 0 );
                    }
            }

            public int Max( Period period, bool positive = true )
            {
                return positive ? pages[period].Max() : Math.Abs( pages[period].Min() );
            }

            public void Plot( Period period, Rect canvas, float wu, float hu, int sign = 1 )
            {
                if ( pages[period].Count > 1 )
                {
                    var hist = pages[period];
                    for ( var i = 0; i < hist.Count - 1; i++ ) // line segments, so up till n-1
                    {
                        var start = new Vector2( wu * i, canvas.height         - hu * hist[i]     * sign );
                        var end   = new Vector2( wu * ( i + 1 ), canvas.height - hu * hist[i + 1] * sign );
                        Widgets.DrawLine( start, end, lineColor, 1f );
                    }
                }
            }
        }
    }
}