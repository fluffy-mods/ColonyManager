// Karel Kroeze
// ManagerTab_Forestry.cs
// 2016-12-09

using System;
using System.Collections.Generic;
using System.Linq;
using Reloader;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;
using static FluffyManager.Widgets_Labels;

namespace FluffyManager
{
    internal class ManagerTab_Forestry : ManagerTab
    {
        #region Fields

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

        [ReloadMethod]
        public void DoContent( Rect rect )
        {
            // layout: settings | trees
            // draw background
            Widgets.DrawMenuSection( rect );


            // rects
            var optionsColumnRect = new Rect(
                rect.xMin,
                rect.yMin,
                rect.width * 3 / 5f,
                rect.height - Margin - ButtonSize.y);
            var treesColumnRect = new Rect(
                optionsColumnRect.xMax,
                rect.yMin,
                rect.width * 2 / 5f,
                rect.height - Margin - ButtonSize.y);
            var buttonRect = new Rect(
                rect.xMax - ButtonSize.x,
                rect.yMax - ButtonSize.y,
                ButtonSize.x - Margin,
                ButtonSize.y - Margin);

            Vector2 position;
            float width;
            Widgets_Section.BeginSectionColumn( optionsColumnRect, "Forestry.Options", out position, out width );
            Widgets_Section.Section( ref position, width, DrawJobType, "FMF.JobType".Translate() );

            if ( _selected.type == ManagerJob_Forestry.ForestryJobType.ClearArea )
            {
                Widgets_Section.Section( ref position, width, DrawClearArea, "FMF.JobType.ClearArea".Translate() );
            }

            if ( _selected.type == ManagerJob_Forestry.ForestryJobType.Logging )
            {
                Widgets_Section.Section( ref position, width, DrawThreshold, "FM.Threshold".Translate() );
                Widgets_Section.Section( ref position, width, DrawAreaRestriction, "FMF.LoggingArea".Translate() );
                Widgets_Section.Section( ref position, width, DrawAllowSaplings );
            }
            Widgets_Section.EndSectionColumn( "Forestry.Options", position );


            Widgets_Section.BeginSectionColumn( treesColumnRect, "Forestry.Trees", out position, out width );

            switch ( _selected.type )
            {
                case ManagerJob_Forestry.ForestryJobType.ClearArea:
                    Widgets_Section.Section(ref position, width, DrawEmpty_ClearArea, "FMF.Trees".Translate());
                    break;
                case ManagerJob_Forestry.ForestryJobType.ClearWind:
                    Widgets_Section.Section(ref position, width, DrawEmpty_ClearWind, "FMF.Trees".Translate());
                    break;
                case ManagerJob_Forestry.ForestryJobType.Logging:
                    Widgets_Section.Section( ref position, width, DrawTreeList, "FMF.Trees".Translate() );
                    break;
            }

            Widgets_Section.EndSectionColumn( "Forestry.Trees", position );

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
        }

        public float DrawJobType( Vector2 pos, float width )
        {
            // type of job;
            // clear wind cells | clear area | logging
            var types = Enum.GetValues( typeof( ManagerJob_Forestry.ForestryJobType ) ) as ManagerJob_Forestry.ForestryJobType[];
            var cellWidth = width / types.Length;

            Rect cellRect = new Rect(
                pos.x,
                pos.y,
                cellWidth,
                ListEntryHeight );

            foreach ( var type in types )
            {
                Utilities.DrawToggle( 
                    cellRect, 
                    $"FMF.JobType.{type}".Translate(),
                    _selected.type == type,
                    () => _selected.type = type,
                    () => {}, 
                    wrap: false );
                cellRect.x += cellWidth;
            }

            return ListEntryHeight;
        }

        public float DrawClearArea( Vector2 pos, float width )
        {
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight );
            AreaAllowedGUI.DoAllowedAreaSelectorsMC( rowRect, ref _selected.ClearAreas );
            return ListEntryHeight;
        }

        public float DrawThreshold( Vector2 pos, float width )
        {
            var start = pos;
            int currentCount = _selected.Trigger.CurCount;
            int designatedCount = _selected.GetWoodInDesignations();
            int targetCount = _selected.Trigger.Count;

            _selected.Trigger.DrawTriggerConfig(ref pos, width, ListEntryHeight, false,
                "FMF.TargetCount".Translate(currentCount, designatedCount, targetCount),
                "FMF.TargetCountTooltip".Translate(currentCount, designatedCount, targetCount));

            return pos.y - start.y;
        }

        public float DrawAreaRestriction( Vector2 pos, float width )
        {
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight );
            AreaAllowedGUI.DoAllowedAreaSelectors( rowRect, ref _selected.LoggingArea, manager );
            return ListEntryHeight;
        }

        public float DrawAllowSaplings( Vector2 pos, float width )
        {
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight);

            // NOTE: AllowSaplings logic is the reverse from the label that is shown to the user.
            Utilities.DrawToggle( rowRect, "FMF.AllowSaplings".Translate(), 
                !_selected.AllowSaplings,
                () => _selected.AllowSaplings = false, 
                () => _selected.AllowSaplings = true );
            return ListEntryHeight;
        }

        public float DrawTreeList( Vector2 pos, float width )
        {
            var start = pos;
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight);
            var allowedTrees = _selected.AllowedTrees;
            var trees = new List<ThingDef>( allowedTrees.Keys );

            // toggle all
            var toggleAllRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            Utilities.DrawToggle( toggleAllRect, "<i>" + "FM.All".Translate() + "</i>",
                _selected.AllowedTrees.Values.All( v => v ),
                () => trees.ForEach( t => allowedTrees[t] = true ),
                () => trees.ForEach( t => allowedTrees[t] = false ) );

            // toggle for each tree
            foreach (ThingDef def in trees)
            {
                rowRect.y += ListEntryHeight;
                Utilities.DrawToggle( rowRect, def.LabelCap, _selected.AllowedTrees[def],
                    () => _selected.AllowedTrees[def] = !_selected.AllowedTrees[def] );
            }

            return rowRect.yMax - start.y;
        }

        public float DrawEmpty_ClearArea(Vector2 pos, float width)
        {
            return DrawEmpty("FMF.ClearArea.TreesExplanation".Translate(), pos, width);
        }

        public float DrawEmpty_ClearWind(Vector2 pos, float width)
        {
            return DrawEmpty("FMF.ClearWind.TreesExplanation".Translate(), pos, width);
        }

        public float DrawEmpty( string label, Vector2 pos, float width )
        {
            var height = Mathf.Max( Text.CalcHeight( label, width ), ListEntryHeight );
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                height );
            Label( rowRect, label, TextAnchor.MiddleLeft, color: Color.gray );
            return height;
        }

        public void DoLeftRow( Rect rect )
        {
            Widgets.DrawMenuSection( rect );

            // content
            float height = _leftRowHeight;
            var scrollView = new Rect( 0f, 0f, rect.width, height );
            if ( height > rect.height )
                scrollView.width -= ScrollbarWidth;

            Widgets.BeginScrollView( rect, ref _scrollPosition, scrollView );
            Rect scrollContent = scrollView;

            GUI.BeginGroup( scrollContent );
            Vector2 cur = Vector2.zero;
            var i = 0;

            foreach ( ManagerJob_Forestry job in _jobs )
            {
                var row = new Rect( 0f, cur.y, scrollContent.width, LargeListEntryHeight );
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

                cur.y += LargeListEntryHeight;
            }

            // row for new job.
            var newRect = new Rect( 0f, cur.y, scrollContent.width, LargeListEntryHeight );
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

            cur.y += LargeListEntryHeight;

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
