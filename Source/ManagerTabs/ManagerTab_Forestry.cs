// Karel Kroeze
// ManagerTab_Forestry.cs
// 2016-12-09

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;
using static FluffyManager.Widgets_Labels;

namespace FluffyManager
{
    internal class ManagerTab_Forestry : ManagerTab
    {
        private List<ManagerJob_Forestry> _jobs;
        private float                     _leftRowHeight  = 9999f;
        private Vector2                   _scrollPosition = Vector2.zero;
        private ManagerJob_Forestry       _selected;

        public ManagerTab_Forestry( Manager manager ) : base( manager )
        {
            _selected = new ManagerJob_Forestry( manager );
        }

        public override Texture2D Icon { get; } = Resources.IconForestry;

        public override IconAreas IconArea => IconAreas.Middle;

        public override string Label => "FMF.Forestry".Translate();

        public override ManagerJob Selected
        {
            get => _selected;
            set => _selected = (ManagerJob_Forestry) value;
        }

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
                rect.height - Margin - ButtonSize.y );
            var treesColumnRect = new Rect(
                optionsColumnRect.xMax,
                rect.yMin,
                rect.width * 2 / 5f,
                rect.height - Margin - ButtonSize.y );
            var buttonRect = new Rect(
                rect.xMax    - ButtonSize.x,
                rect.yMax    - ButtonSize.y,
                ButtonSize.x - Margin,
                ButtonSize.y - Margin );

            Vector2 position;
            float   width;
            Widgets_Section.BeginSectionColumn( optionsColumnRect, "Forestry.Options", out position, out width );
            Widgets_Section.Section( ref position, width, DrawJobType, "FMF.JobType".Translate() );

            if ( _selected.Type == ManagerJob_Forestry.ForestryJobType.ClearArea )
                Widgets_Section.Section( ref position, width, DrawClearArea, "FMF.JobType.ClearArea".Translate() );

            if ( _selected.Type == ManagerJob_Forestry.ForestryJobType.Logging )
            {
                Widgets_Section.Section( ref position, width, DrawThreshold, "FM.Threshold".Translate() );
                Widgets_Section.Section( ref position, width, DrawAreaRestriction, "FMF.LoggingArea".Translate() );
                Widgets_Section.Section( ref position, width, DrawAllowSaplings );
            }

            Widgets_Section.EndSectionColumn( "Forestry.Options", position );

            Widgets_Section.BeginSectionColumn( treesColumnRect, "Forestry.Trees", out position, out width );
            Widgets_Section.Section( ref position, width, DrawTreeShortcuts, "FMF.Trees".Translate() );
            Widgets_Section.Section( ref position, width, DrawTreeList );
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
            // clear clear area | logging
            var types =
                Enum.GetValues( typeof( ManagerJob_Forestry.ForestryJobType ) ) as ManagerJob_Forestry.ForestryJobType
                    [];

            // backwards compatibility for wind areas
            // TODO: REMOVE ON NEXT BREAKING VERSION!
            types = types.Where( type => type != ManagerJob_Forestry.ForestryJobType.ClearWind ).ToArray();

            var cellWidth = width / types.Length;

            var cellRect = new Rect(
                pos.x,
                pos.y,
                cellWidth,
                ListEntryHeight );

            foreach ( var type in types )
            {
                Utilities.DrawToggle(
                    cellRect,
                    $"FMF.JobType.{type}".Translate(),
                    $"FMF.JobType.{type}.Tip".Translate(),
                    _selected.Type == type,
                    () => _selected.Type = type,
                    () => { },
                    wrap: false );
                cellRect.x += cellWidth;
            }

            return ListEntryHeight;
        }

        public float DrawClearArea( Vector2 pos, float width )
        {
            var start = pos;
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight );
            AreaAllowedGUI.DoAllowedAreaSelectorsMC( rowRect, ref _selected.ClearAreas );
            pos.y += ListEntryHeight;
            Utilities.DrawToggle(
                ref pos,
                width,
                "FMF.ClearWindCells".Translate(),
                "FMF.ClearWindCells.Tip".Translate(),
                ref _selected.ClearWindCells );

            return pos.y - start.y;
        }

        public float DrawThreshold( Vector2 pos, float width )
        {
            var start           = pos;
            var currentCount    = _selected.Trigger.CurrentCount;
            var designatedCount = _selected.GetWoodInDesignations();
            var targetCount     = _selected.Trigger.TargetCount;

            _selected.Trigger.DrawTriggerConfig( ref pos, width, ListEntryHeight,
                                                 "FMF.TargetCount".Translate(
                                                     currentCount, designatedCount, targetCount ),
                                                 "FMF.TargetCountTooltip".Translate(
                                                     currentCount, designatedCount, targetCount ),
                                                 _selected.Designations, null, _selected.DesignationLabel );

            Utilities.DrawReachabilityToggle( ref pos, width, ref _selected.CheckReachable );
            Utilities.DrawToggle(
                ref pos,
                width,
                "FM.PathBasedDistance".Translate(),
                "FM.PathBasedDistance.Tip".Translate(),
                ref _selected.PathBasedDistance,
                true );

            return pos.y - start.y;
        }

        public float DrawAreaRestriction( Vector2 pos, float width )
        {
            var start = pos;
            AreaAllowedGUI.DoAllowedAreaSelectors( ref pos, width, ref _selected.LoggingArea, manager );
            return pos.y - start.y;
        }

        public float DrawAllowSaplings( Vector2 pos, float width )
        {
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight );

            // NOTE: AllowSaplings logic is the reverse from the label that is shown to the user.
            Utilities.DrawToggle(
                rowRect,
                "FMF.AllowSaplings".Translate(),
                "FMF.AllowSaplings.Tip".Translate(),
                !_selected.AllowSaplings,
                () => _selected.AllowSaplings = false,
                () => _selected.AllowSaplings = true );
            return ListEntryHeight;
        }

        public float DrawTreeShortcuts( Vector2 pos, float width )
        {
            var start = pos;
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight );
            var allowed = _selected.AllowedTrees;
            var plants  = new List<ThingDef>( allowed.Keys );

            // toggle all
            Utilities.DrawToggle( rowRect,
                                  "FM.All".Translate().Italic(),
                                  string.Empty,
                                  _selected.AllowedTrees.Values.All( v => v ),
                                  _selected.AllowedTrees.Values.All( v => !v ),
                                  () => plants.ForEach( t => allowed[t] = true ),
                                  () => plants.ForEach( t => allowed[t] = false ) );

            if ( _selected.Type == ManagerJob_Forestry.ForestryJobType.ClearArea )
            {
                rowRect.y += ListEntryHeight;
                // trees (anything that drops wood, or has the correct harvest tag).
                var trees = plants.Where( tree => tree.plant.harvestTag        == "Wood" ||
                                                  tree.plant.harvestedThingDef == ThingDefOf.WoodLog ).ToList();
                Utilities.DrawToggle( rowRect,
                                      "FMF.Trees".Translate().Italic(),
                                      "FMF.Trees.Tip".Translate(),
                                      trees.All( t => allowed[t] ),
                                      trees.All( t => !allowed[t] ),
                                      () => trees.ForEach( t => allowed[t] = true ),
                                      () => trees.ForEach( t => allowed[t] = false ) );
                rowRect.y += ListEntryHeight;

                // flammable (probably all - might be modded stuff).
                var flammable = plants.Where( tree => tree.BaseFlammability > 0 ).ToList();
                if ( flammable.Count != plants.Count )
                {
                    Utilities.DrawToggle(
                        rowRect,
                        "FMF.Flammable".Translate().Italic(),
                        "FMF.Flammable.Tip".Translate(),
                        flammable.All( t => allowed[t] ),
                        flammable.All( t => !allowed[t] ),
                        () => flammable.ForEach( t => allowed[t] = true ),
                        () => flammable.ForEach( t => allowed[t] = false ) );
                    rowRect.y += ListEntryHeight;
                }

                // ugly (possibly none - modded stuff).
                var ugly = plants.Where( tree => tree.statBases.GetStatValueFromList( StatDefOf.Beauty, 0 ) < 0 )
                                 .ToList();
                if ( !ugly.NullOrEmpty() )
                {
                    Utilities.DrawToggle( rowRect,
                                          "FMF.Ugly".Translate().Italic(),
                                          "FMF.Ugly.Tip".Translate(),
                                          ugly.All( t => allowed[t] ),
                                          ugly.All( t => !allowed[t] ),
                                          () => ugly.ForEach( t => allowed[t] = true ),
                                          () => ugly.ForEach( t => allowed[t] = false ) );
                    rowRect.y += ListEntryHeight;
                }

                // provides cover
                var cover = plants.Where( tree => tree.Fillage == FillCategory.Full ||
                                                  tree.Fillage == FillCategory.Partial && tree.fillPercent > 0 )
                                  .ToList();
                Utilities.DrawToggle( rowRect,
                                      "FMF.ProvidesCover".Translate().Italic(),
                                      "FMF.ProvidesCover.Tip".Translate(),
                                      cover.All( t => allowed[t] ),
                                      cover.All( t => !allowed[t] ),
                                      () => cover.ForEach( t => allowed[t] = true ),
                                      () => cover.ForEach( t => allowed[t] = false ) );
                rowRect.y += ListEntryHeight;

                // blocks wind
                var wind = plants.Where( tree => tree.blockWind ).ToList();
                Utilities.DrawToggle( rowRect,
                                      "FMF.BlocksWind".Translate().Italic(),
                                      "FMF.BlocksWind.Tip".Translate(),
                                      wind.All( t => allowed[t] ),
                                      wind.All( t => !allowed[t] ),
                                      () => wind.ForEach( t => allowed[t] = true ),
                                      () => wind.ForEach( t => allowed[t] = false ) );
            }

            return rowRect.yMax - start.y;
        }

        public float DrawTreeList( Vector2 pos, float width )
        {
            var start = pos;
            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight );
            var allowedTrees = _selected.AllowedTrees;
            var trees        = new List<ThingDef>( allowedTrees.Keys );

            // toggle for each tree
            foreach ( var def in trees )
            {
                Utilities.DrawToggle( rowRect, def.LabelCap, def.description, _selected.AllowedTrees[def],
                                      () => _selected.AllowedTrees[def] = !_selected.AllowedTrees[def] );
                rowRect.y += ListEntryHeight;
            }

            return rowRect.yMin - start.y;
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
            var height     = _leftRowHeight;
            var scrollView = new Rect( 0f, 0f, rect.width, height );
            if ( height > rect.height )
                scrollView.width -= ScrollbarWidth;

            Widgets.BeginScrollView( rect, ref _scrollPosition, scrollView );
            var scrollContent = scrollView;

            GUI.BeginGroup( scrollContent );
            var cur = Vector2.zero;
            var i   = 0;

            foreach ( var job in _jobs )
            {
                var row = new Rect( 0f, cur.y, scrollContent.width, LargeListEntryHeight );
                Widgets.DrawHighlightIfMouseover( row );
                if ( _selected == job ) Widgets.DrawHighlightSelected( row );

                if ( i++ % 2 == 1 ) Widgets.DrawAltRect( row );

                var jobRect = row;

                if ( ManagerTab_Overview.DrawOrderButtons( new Rect( row.xMax - 50f, row.yMin, 50f, 50f ), manager,
                                                           job ) ) Refresh();
                jobRect.width -= 50f;

                job.DrawListEntry( jobRect, false );
                if ( Widgets.ButtonInvisible( jobRect ) ) _selected = job;

                cur.y += LargeListEntryHeight;
            }

            // row for new job.
            var newRect = new Rect( 0f, cur.y, scrollContent.width, LargeListEntryHeight );
            Widgets.DrawHighlightIfMouseover( newRect );

            if ( i % 2 == 1 ) Widgets.DrawAltRect( newRect );

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( newRect, "<" + "FMF.NewForestryJob".Translate() + ">" );
            Text.Anchor = TextAnchor.UpperLeft;

            if ( Widgets.ButtonInvisible( newRect ) ) Selected = new ManagerJob_Forestry( manager );

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
            if ( Selected != null ) DoContent( contentCanvas );
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
            foreach ( var job in _jobs )
                job.UpdateClearAreas();


            // update plant options
            foreach ( var job in _jobs )
                job.RefreshAllowedTrees();

            // also for selected job
            _selected?.RefreshAllowedTrees();
        }
    }
}