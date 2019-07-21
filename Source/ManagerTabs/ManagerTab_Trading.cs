// // Karel Kroeze
// // ManagerTab_Trading.cs
// // 2016-12-09

// using RimWorld;
// using System.Collections.Generic;
// using UnityEngine;
// using Verse;

// namespace FluffyManager
// {
//     public class ManagerTab_Trading : ManagerTab
//     {
//         #region Fields

//         public List<ManagerJob_Trading> Jobs;
//         private static float _entryHeight = 30f;
//         private Vector2 _button = new Vector2( 200f, 40f );
//         private float _leftRowHeight = 9999f;
//         private Vector2 _scrollPosition = Vector2.zero;
//         private ManagerJob_Trading _selected;
//         private float _topAreaHeight = 30f;

//         #endregion Fields

//         #region Constructors

//         public ManagerTab_Trading( Manager manager ) : base( manager )
//         {
//             _selected = new ManagerJob_Trading( manager );
//         }

//         #endregion Constructors


//         #region Properties

//         public override Texture2D Icon => Resources.IconTrading;

//         public override IconAreas IconArea => IconAreas.Middle;

//         public override string Label => "FMT.Trading".Translate();

//         public override ManagerJob Selected
//         {
//             get { return _selected; }
//             set { _selected = value as ManagerJob_Trading; }
//         }

//         #endregion Properties


//         #region Methods

//         public override void DoWindowContents( Rect canvas )
//         {
//             // set up rects
//             var leftRow = new Rect( 0f, 0f, DefaultLeftRowSize, canvas.height );
//             var contentCanvas = new Rect( leftRow.xMax + Constants.Margin, 0f, canvas.width - leftRow.width - Constants.Margin,
//                                           canvas.height );

//             // draw overview row
//             // TODO: DoLeftRow( leftRow );

//             // draw job interface if something is selected.
//             if ( Selected != null )
//             {
//                 // TODO: DoContent( contentCanvas );
//             }
//         }

//         #endregion Methods
//     }
// }

