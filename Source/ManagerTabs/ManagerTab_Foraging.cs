// Karel Kroeze
// ManagerTab_Foraging.cs
// 2016-12-09

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    internal class ManagerTab_Foraging : ManagerTab
    {
        private List<ManagerJob_Foraging> _jobs;
        private float                     _leftRowHeight;
        private Vector2                   _scrollPosition = Vector2.zero;
        private ManagerJob_Foraging       _selected;

        public ManagerTab_Foraging( Manager manager ) : base( manager )
        {
            _selected = new ManagerJob_Foraging( manager );
        }

        public override Texture2D Icon     => Resources.IconForaging;
        public override IconAreas IconArea => IconAreas.Middle;
        public override string    Label    => "FMG.Foraging".Translate();

        public override ManagerJob Selected
        {
            get => _selected;
            set => _selected = (ManagerJob_Foraging) value;
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
            var plantsColumnRect = new Rect(
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
            Widgets_Section.BeginSectionColumn( optionsColumnRect, "Foraging.Options", out position, out width );
            Widgets_Section.Section( ref position, width, DrawThreshold, "FM.Threshold".Translate() );
            Widgets_Section.Section( ref position, width, DrawAreaRestriction, "FMG.ForagingArea".Translate() );
            Widgets_Section.Section( ref position, width, DrawMaturePlants );
            Widgets_Section.EndSectionColumn( "Foraging.Options", position );

            Widgets_Section.BeginSectionColumn( plantsColumnRect, "Foraging.Plants", out position, out width );
            var refreshRect = new Rect(
                position.x + width - SmallIconSize - 2 * Margin,
                position.y                         + Margin,
                SmallIconSize,
                SmallIconSize );
            if ( Widgets.ButtonImage( refreshRect, Resources.Refresh, Color.grey ) )
                _selected.RefreshAllowedPlants();
            Widgets_Section.Section( ref position, width, DrawPlantShortcuts, "FMG.Plants".Translate() );
            Widgets_Section.Section( ref position, width, DrawPlantList );
            Widgets_Section.EndSectionColumn( "Foraging.Plants", position );


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

        public float DrawThreshold( Vector2 pos, float width )
        {
            var currentCount    = _selected.Trigger.CurrentCount;
            var designatedCount = _selected.CurrentDesignatedCount;
            var targetCount     = _selected.Trigger.TargetCount;
            var start           = pos;

            _selected.Trigger.DrawTriggerConfig( ref pos, width, ListEntryHeight,
                                                 "FMG.TargetCount".Translate(
                                                     currentCount, designatedCount, targetCount ),
                                                 "FMG.TargetCountTooltip".Translate(
                                                     currentCount, designatedCount, targetCount ),
                                                 _selected.Designations, null, _selected.DesignationLabel );

            Utilities.DrawReachabilityToggle( ref pos, width, ref _selected.CheckReachable );
            Utilities.DrawToggle( ref pos, width, "FM.PathBasedDistance".Translate(),
                                  "FM.PathBasedDistance.Tip".Translate(), ref _selected.PathBasedDistance,
                                  true );

            return pos.y - start.y;
        }

        public float DrawMaturePlants( Vector2 pos, float width )
        {
            // Force mature plants only (2)
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            Utilities.DrawToggle( rowRect, "FMG.ForceMature".Translate(), "FMG.ForceMature.Tip".Translate(),
                                  ref _selected.ForceFullyMature );

            return ListEntryHeight;
        }

        public float DrawAreaRestriction( Vector2 pos, float width )
        {
            var start = pos;
            AreaAllowedGUI.DoAllowedAreaSelectors( ref pos, width, ref _selected.ForagingArea, manager );
            return pos.y - start.y;
        }

        public float DrawPlantShortcuts( Vector2 pos, float width )
        {
            var start = pos;

            // list of keys in allowed trees list (all plans that yield wood in biome, static)
            var allowedPlants = _selected.AllowedPlants;
            var plants        = allowedPlants.Keys.ToList();

            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight );

            // toggle all
            Utilities.DrawToggle(
                rowRect,
                "<i>" + "FM.All".Translate() + "</i>",
                string.Empty,
                allowedPlants.Values.All( p => p ),
                allowedPlants.Values.All( p => !p ),
                () => plants.ForEach( p => allowedPlants[p] = true ),
                () => plants.ForEach( p => allowedPlants[p] = false ) );

            // toggle edible
            rowRect.y += ListEntryHeight;
            var edible = plants.Where( p => p.plant.harvestedThingDef.IsNutritionGivingIngestible ).ToList();
            Utilities.DrawToggle(
                rowRect,
                "<i>" + "FM.Foraging.Edible".Translate() + "</i>",
                "FM.Foraging.Edible.Tip".Translate(),
                edible.All( p => allowedPlants[p] ),
                edible.All( p => !allowedPlants[p] ),
                () => edible.ForEach( p => allowedPlants[p] = true ),
                () => edible.ForEach( p => allowedPlants[p] = false ) );

            // toggle shrooms
            rowRect.y += ListEntryHeight;
            var shrooms = plants.Where( p => p.plant.cavePlant ).ToList();
            Utilities.DrawToggle(
                rowRect,
                "<i>" + "FM.Foraging.Mushrooms".Translate() + "</i>",
                "FM.Foraging.Mushrooms.Tip".Translate(),
                shrooms.All( p => allowedPlants[p] ),
                shrooms.All( p => !allowedPlants[p] ),
                () => shrooms.ForEach( p => allowedPlants[p] = true ),
                () => shrooms.ForEach( p => allowedPlants[p] = false ) );

            return rowRect.yMax - start.y;
        }

        public float DrawPlantList( Vector2 pos, float width )
        {
            var start = pos;

            // list of keys in allowed trees list (all plans that yield wood in biome, static)
            var allowedPlants = _selected.AllowedPlants;
            var plants        = allowedPlants.Keys.ToList();

            var rowRect = new Rect(
                pos.x,
                pos.y,
                width,
                ListEntryHeight );

            // toggle for each plant
            foreach ( var plant in plants.OrderBy( p => p.LabelCap ) )
            {
                Utilities.DrawToggle( rowRect, plant.LabelCap, plant.description, _selected.AllowedPlants[plant],
                                      () => _selected.AllowedPlants[plant] = !_selected.AllowedPlants[plant] );
                rowRect.y += ListEntryHeight;
            }

            return rowRect.yMin - start.y;
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
            Widgets.Label( newRect, "<" + "FMG.NewForagingJob".Translate() + ">" );
            Text.Anchor = TextAnchor.UpperLeft;

            if ( Widgets.ButtonInvisible( newRect ) ) Selected = new ManagerJob_Foraging( manager );

            TooltipHandler.TipRegion( newRect, "FMG.NewForagingJobTooltip".Translate() );

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

        public override void PreOpen()
        {
            Refresh();
        }

        public void Refresh()
        {
            _jobs = Manager.For( manager ).JobStack.FullStack<ManagerJob_Foraging>();

            // update plant options
            foreach ( var job in _jobs )
                job.RefreshAllowedPlants();

            // update selected ( also update thingfilter _only_ if the job is not managed yet )
            _selected?.RefreshAllowedPlants( !_selected.Managed );
        }
    }
}