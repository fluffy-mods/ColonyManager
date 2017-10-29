// Karel Kroeze
// ManagerTab_Forestry.cs
// 2016-12-09

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    internal class ManagerTab_Forestry : ManagerTab
    {
        #region Fields

        private const float EntryHeight = 30f;

        private const float Margin = Utilities.Margin;

        private readonly float _topAreaHeight = 30f;

        private Vector2 _button = new Vector2( 200f, 40f );

        private Vector2 _contentScrollPosition = Vector2.zero;

        private List<ManagerJob_Forestry> _jobs;

        private float _leftRowHeight = 9999f;

        private Vector2 _scrollPosition = Vector2.zero;

        private ManagerJob_Forestry _selected;

        #endregion Fields

        #region Constructors

        public ManagerTab_Forestry( Manager manager ) : base( manager )
        {
            _selected = new ManagerJob_Forestry( manager );
        }

        #endregion Constructors



        #region Properties

        public override Texture2D Icon { get; } = Resources.IconForestry;

        public override IconAreas IconArea
        {
            get { return IconAreas.Middle; }
        }

        public override string Label
        {
            get { return "FMF.Forestry".Translate(); }
        }

        public override ManagerJob Selected
        {
            get { return _selected; }
            set { _selected = (ManagerJob_Forestry)value; }
        }

        #endregion Properties



        #region Methods

        public void DoContent( Rect rect )
        {
            // layout: settings | trees
            // draw background
            Widgets.DrawMenuSection( rect );

            // some variables
            float width = rect.width;
            float height = rect.height - _topAreaHeight - _button.y - Margin;
            var cols = 2;
            float colWidth = width / cols - Margin;
            var colRects = new List<Rect>();
            var colTitleRects = new List<Rect>();
            var buttonRect = new Rect( rect.width - _button.x, rect.height - _button.y, _button.x - Margin,
                                       _button.y - Margin );

            // set up rects
            for ( var j = 0; j < cols; j++ )
            {
                colRects.Add( new Rect( j * colWidth + j * Margin + Margin / 2, _topAreaHeight, colWidth, height ) );
                colTitleRects.Add( new Rect( j * colWidth + j * Margin + Margin * 2.5f, 0f, colWidth, _topAreaHeight ) );
            }

            // keep track of location
            Vector2 cur;

            // begin window
            GUI.BeginGroup( rect );

            // settings.
            Text.Anchor = TextAnchor.LowerLeft;
            Text.Font = GameFont.Tiny;
            Widgets.Label( colTitleRects[0], "FMF.Options".Translate() );
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.DrawTexture( colRects[0], Resources.SlightlyDarkBackground );
            GUI.BeginGroup( colRects[0] );
            cur = Vector2.zero;

            // trigger config (1)
            int currentCount = _selected.Trigger.CurCount;
            int designatedCount = _selected.GetWoodInDesignations();
            int targetCount = _selected.Trigger.Count;
            _selected.Trigger.DrawTriggerConfig( ref cur, colRects[0].width, EntryHeight, true,
                                                 "FMF.TargetCount".Translate( currentCount, designatedCount, targetCount ),
                                                 "FMF.TargetCountTooltip".Translate( currentCount, designatedCount,
                                                                                     targetCount ) );

            // Clear wind cells (2)
            var clearWindCellsRect = new Rect( cur.x, cur.y, colWidth, EntryHeight );
            Utilities.DrawToggle( clearWindCellsRect, "FMF.ClearWindCells".Translate(),
                                  ref ManagerJob_Forestry.ClearWindCells );
            cur.y += EntryHeight;

            // clear additional areas(3)
            var clearAdditionalAreasLabelRect = new Rect( cur.x, cur.y, colWidth, EntryHeight );
            Widgets.DrawAltRect( clearAdditionalAreasLabelRect );
            Utilities.Label( clearAdditionalAreasLabelRect, "FMF.ClearAreas".Translate(), anchor: TextAnchor.MiddleLeft,
                             lrMargin: Margin );
            cur.y += EntryHeight;

            var clearAdditionalAreasSelectorRect = new Rect( cur.x, cur.y, colWidth, EntryHeight );
            Widgets.DrawAltRect( clearAdditionalAreasSelectorRect );
            AreaAllowedGUI.DoAllowedAreaSelectorsMC( clearAdditionalAreasSelectorRect, ref _selected.ClearAreas, Margin );
            cur.y += EntryHeight;

            // Allow saplings (4)
            var allowSaplingsRect = new Rect( cur.x, cur.y, colWidth, EntryHeight );
            Utilities.DrawToggle( allowSaplingsRect, "FMF.AllowSaplings".Translate(), ref _selected.AllowSaplings );
            cur.y += EntryHeight;

            // Logging area (5)
            var loggingAreaTitleRect = new Rect( cur.x, cur.y, colWidth, EntryHeight );
            Widgets.DrawAltRect( loggingAreaTitleRect );
            Utilities.Label( loggingAreaTitleRect, "FMF.LoggingArea".Translate(), anchor: TextAnchor.MiddleLeft,
                             lrMargin: Margin );
            cur.y += EntryHeight;

            var loggingAreaRect = new Rect( cur.x, cur.y, colWidth, EntryHeight );
            Widgets.DrawAltRect( loggingAreaRect );
            AreaAllowedGUI.DoAllowedAreaSelectors( loggingAreaRect, ref _selected.LoggingArea, manager, lrMargin: Margin );
            cur.y += EntryHeight;

            GUI.EndGroup();

            // treedefs.
            Text.Anchor = TextAnchor.LowerLeft;
            Text.Font = GameFont.Tiny;
            Widgets.Label( colTitleRects[1], "FMF.Trees".Translate() );
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.DrawTexture( colRects[1], Resources.SlightlyDarkBackground );
            GUI.BeginGroup( colRects[1] );
            cur = Vector2.zero;

            Rect outRect = colRects[1].AtZero().ContractedBy( 1f );
            var viewRect = new Rect( 0f, 0f, outRect.width, _selected.AllowedTrees.Count * EntryHeight );
            if ( viewRect.height > outRect.height )
                viewRect.width -= Utilities.ScrollbarWidth;

            // start scrolling view
            Widgets.BeginScrollView( outRect, ref _contentScrollPosition, viewRect );

            // list of keys in allowed trees list (all plans that yield wood in biome, static)
            var treeDefs = new List<ThingDef>( _selected.AllowedTrees.Keys );

            // toggle all
            var toggleAllRect = new Rect( cur.x, cur.y, colWidth, EntryHeight );
            Widgets.DrawAltRect( toggleAllRect );
            Utilities.DrawToggle( toggleAllRect, "<i>" + "FM.All".Translate() + "</i>",
                                  _selected.AllowedTrees.Values.All( v => v ), delegate
                                                                                   {
                                                                                       foreach (
                                                                                           ThingDef def in treeDefs )
                                                                                       {
                                                                                           _selected.AllowedTrees[def] =
                                                                                               true;
                                                                                       }
                                                                                   }, delegate
                                                                                          {
                                                                                              foreach (
                                                                                                  ThingDef def in
                                                                                                      treeDefs )
                                                                                              {
                                                                                                  _selected.AllowedTrees
                                                                                                      [def] = false;
                                                                                              }
                                                                                          } );

            cur.y += EntryHeight;

            // toggle for each tree
            var i = 1;

            foreach ( ThingDef def in treeDefs )
            {
                var toggleRect = new Rect( cur.x, cur.y, colWidth, EntryHeight );

                // highlight alternate rows
                if ( i++ % 2 == 0 )
                {
                    Widgets.DrawAltRect( toggleRect );
                }

                // draw the toggle
                Utilities.DrawToggle( toggleRect, def.LabelCap, _selected.AllowedTrees[def],
                                      delegate
                                      { _selected.AllowedTrees[def] = !_selected.AllowedTrees[def]; } );

                // update current position
                cur.y += EntryHeight;
            }

            // close scrolling view
            Widgets.EndScrollView();

            // close tree list
            GUI.EndGroup();

            // do the button
            if ( !_selected.Managed )
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Manage".Translate() ) )
                {
                    // activate job, add it to the stack
                    _selected.Managed = true;
                    manager.JobStack.Add( _selected );

                    // refresh source list
                    Refresh();
                }
            }
            else
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Delete".Translate() ) )
                {
                    // inactivate job, remove from the stack.
                    manager.JobStack.Delete( _selected );

                    // remove content from UI
                    _selected = null;

                    // refresh source list
                    Refresh();
                }
            }

            // close window
            GUI.EndGroup();
        }

        public void DoLeftRow( Rect rect )
        {
            Widgets.DrawMenuSection( rect );

            // content
            float height = _leftRowHeight;
            var scrollView = new Rect( 0f, 0f, rect.width, height );
            if ( height > rect.height )
                scrollView.width -= Utilities.ScrollbarWidth;

            Widgets.BeginScrollView( rect, ref _scrollPosition, scrollView );
            Rect scrollContent = scrollView;

            GUI.BeginGroup( scrollContent );
            Vector2 cur = Vector2.zero;
            var i = 0;

            foreach ( ManagerJob_Forestry job in _jobs )
            {
                var row = new Rect( 0f, cur.y, scrollContent.width, Utilities.LargeListEntryHeight );
                Widgets.DrawHighlightIfMouseover( row );
                if ( _selected == job )
                {
                    Widgets.DrawHighlightSelected( row );
                }

                if ( i++ % 2 == 1 )
                {
                    Widgets.DrawAltRect( row );
                }

                Rect jobRect = row;

                if ( ManagerTab_Overview.DrawOrderButtons( new Rect( row.xMax - 50f, row.yMin, 50f, 50f ), manager, job ) )
                {
                    Refresh();
                }
                jobRect.width -= 50f;

                job.DrawListEntry( jobRect, false );
                if ( Widgets.ButtonInvisible( jobRect ) )
                {
                    _selected = job;
                }

                cur.y += Utilities.LargeListEntryHeight;
            }

            // row for new job.
            var newRect = new Rect( 0f, cur.y, scrollContent.width, Utilities.LargeListEntryHeight );
            Widgets.DrawHighlightIfMouseover( newRect );

            if ( i % 2 == 1 )
            {
                Widgets.DrawAltRect( newRect );
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( newRect, "<" + "FMF.NewForestryJob".Translate() + ">" );
            Text.Anchor = TextAnchor.UpperLeft;

            if ( Widgets.ButtonInvisible( newRect ) )
            {
                Selected = new ManagerJob_Forestry( manager );
            }

            TooltipHandler.TipRegion( newRect, "FMF.NewForestryJobTooltip".Translate() );

            cur.y += Utilities.LargeListEntryHeight;

            _leftRowHeight = cur.y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public override void DoWindowContents( Rect canvas )
        {
            // set up rects
            var leftRow = new Rect( 0f, 0f, DefaultLeftRowSize, canvas.height );
            var contentCanvas = new Rect( leftRow.xMax + Margin, 0f, canvas.width - leftRow.width - Margin,
                                          canvas.height );

            // draw overview row
            DoLeftRow( leftRow );

            // draw job interface if something is selected.
            if ( Selected != null )
            {
                DoContent( contentCanvas );
            }
        }

        public override void PostClose()
        {
            Refresh();
        }

        public override void PreOpen()
        {
            Refresh();
        }

        public void Refresh()
        {
            _jobs = manager.JobStack.FullStack<ManagerJob_Forestry>();

            // makes sure the list of possible areas is up-to-date with the area in the game.
            foreach ( ManagerJob_Forestry job in _jobs )
                job.UpdateClearAreas();


            // update plant options
            foreach (var job in _jobs)
                job.RefreshAllowedTrees();
            
            // also for selected job
            _selected?.RefreshAllowedTrees();
        }

        #endregion Methods
    }
}
