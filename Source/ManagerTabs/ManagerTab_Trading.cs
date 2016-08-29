// // Karel Kroeze
// // ManagerTab_Trading.cs
// // 2016-07-02

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class ManagerTab_Trading : ManagerTab
    {
        private static float              _entryHeight           = 30f;
        private static ManagerJob_Trading _selected              = new ManagerJob_Trading();
        private Vector2                   _button                = new Vector2( 200f, 40f );
        private float                     _leftRowHeight         = 9999f;
        private float                     _margin                = Utilities.Margin;
        private Vector2                   _scrollPosition        = Vector2.zero;
        private float                     _topAreaHeight         = 30f;
        public List<ManagerJob_Trading>   Jobs;

        public override Texture2D Icon => Resources.IconTrading;
        public override IconAreas IconArea => IconAreas.Middle;
        public override string Label => "FMT.Trading".Translate();

        public override ManagerJob Selected
        {
            get { return _selected; }
            set { _selected = value as ManagerJob_Trading; }
        }

        public override void DoWindowContents( Rect canvas )
        {
            // set up rects
            Rect leftRow = new Rect( 0f, 0f, DefaultLeftRowSize, canvas.height );
            Rect contentCanvas = new Rect( leftRow.xMax + _margin, 0f, canvas.width - leftRow.width - _margin,
                                           canvas.height );

            // draw overview row
            // TODO: DoLeftRow( leftRow );

            // draw job interface if something is selected.
            if ( Selected != null )
            {
               // TODO: DoContent( contentCanvas );
            }
        }
    }
}