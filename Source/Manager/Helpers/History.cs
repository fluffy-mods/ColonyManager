using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace FM
{
    public class History
    {
        // main cache
        public int          size { get; set; }
        private List<int>   _hist           = new List<int> { 0 }; // start with a single entry to avoid issues with .Max()

        // period stuff
        private period      _period         = period.day;
        private int         _ticksPerPeriod = 0;

        // types
        public enum period
        {
            day,
            month,
            year
        }
        
        // plotting stuff
        public Color        _lineCol        = Color.white;
        private Texture2D   _plotBG         = SolidColorMaterials.NewSolidColorTexture( 0f, 0f, 0f, .2f );
        private float       _margin         = Manager.Margin;
        private float       _yAxisMargin    = 25f;
        private int         _breaks         = 5;
        private int         _dashLength     = 3;

        public void Add(int x )
        {
            _hist.Add( x );

            while (_hist.Count > size )
            {
                _hist.RemoveAt( 0 );
            }
        }

        public int[] Get
        {
            get
            {
                return _hist.ToArray();
            }
        }

        public int Interval
        {
            get
            {
                if (_ticksPerPeriod == 0 )
                {
                    int ticks;
                    switch( _period )
                    {
                        case period.day:
                            ticks = GenDate.TicksPerDay;
                            break;
                        case period.month:
                            ticks = GenDate.TicksPerMonth;
                            break;
                        case period.year:
                        default:
                            ticks = GenDate.TicksPerYear;
                            break;
                    }
                    _ticksPerPeriod = ticks / size;
                }
                return _ticksPerPeriod;
            }
        }
        
        public void DrawPlot(Rect rect, int target = 0, string label = "" )
        {
            // stuff we need
            Rect plot   = rect.ContractedBy(Manager.Margin);
            plot.xMin   += _yAxisMargin;
            int max     = Math.Max(_hist.Max(), (int)(target * 1.2));
            float w     = plot.width;
            float h     = plot.height;
            float wu    = w / size;             // width per section
            float hu    = h / max;              // height per count
            int bi      = max / (_breaks + 1);  // count per break
            float bu    = hu * bi;              // height per break
            
            // plot the line
            GUI.DrawTexture( plot, _plotBG );
            GUI.BeginGroup( plot );
            if (_hist.Count() > 1 )
            {
                for( int i = 0; i < _hist.Count() - 1; i++ ) // line segments, so up till n-1
                {
                    Vector2 start = new Vector2(wu * i, h - hu * _hist[i]);
                    Vector2 end = new Vector2(wu * (i + 1), h - hu * _hist[i+1]);
                    Widgets.DrawLine( start, end, _lineCol, 1f );
                }
            }

            // draw target line
            GUI.color = Color.gray;
            for( int i = 0; i < plot.width / _dashLength; i += 2 )
            {
                Widgets.DrawLineHorizontal( i * _dashLength, plot.height - target * hu, _dashLength );
            }
            GUI.EndGroup();

            // plot axis
            GUI.BeginGroup( rect );
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            
            // draw ticks + labels
            for( int i = 1; i < _breaks + 1; i++ )
            {
                Widgets.DrawLineHorizontal( _yAxisMargin + _margin / 2, plot.height - i * bu, _margin );
                Rect labRect = new Rect(0f, plot.height - i * bu - 4f, _yAxisMargin, 20f);
                Widgets.Label( labRect, (i * bi).ToString() );
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            GUI.EndGroup();
        }

        public History( int size, period period = period.day )
        {
            this.size = size;
            _period = period;
        }
    }
}
