// ManagerTab_Mining.cs
// Copyright Karel Kroeze, 2017-2017

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class ManagerTab_Mining : ManagerTab
    {
        public static HashSet<ThingDef> _metals = new HashSet<ThingDef>( DefDatabase<ThingDef>.AllDefsListForReading
                                                                                              .Where( td => td.IsStuff
                                                                                                         && td
                                                                                                           .stuffProps
                                                                                                           .categories
                                                                                                           .Contains(
                                                                                                                StuffCategoryDefOf
                                                                                                                   .Metallic ) ) );

        private float             _jobListHeight;
        private Vector2           _jobListScrollPosition = Vector2.zero;
        private ManagerJob_Mining _selected;

        public List<ManagerJob_Mining> Jobs;

        public ManagerTab_Mining( Manager manager ) : base( manager )
        {
            _selected = new ManagerJob_Mining( manager );
        }

        public override Texture2D Icon     => Resources.IconMining;
        public override IconAreas IconArea => IconAreas.Middle;
        public override string    Label    => "FM.Mining".Translate();

        public override ManagerJob Selected
        {
            get => _selected;
            set => _selected = value as ManagerJob_Mining;
        }

        public override void DoWindowContents( Rect canvas )
        {
            var jobListRect = new Rect(
                0,
                0,
                DefaultLeftRowSize,
                canvas.height );
            var jobDetailsRect = new Rect(
                jobListRect.xMax + Margin,
                0,
                canvas.width - jobListRect.width - Margin,
                canvas.height );

            DoJobList( jobListRect );
            if ( Selected != null )
                DoJobDetails( jobDetailsRect );
        }

        private void DoJobDetails( Rect rect )
        {
            Widgets.DrawMenuSection( rect );

            // rects
            var optionsColumnRect = new Rect(
                rect.xMin,
                rect.yMin,
                rect.width * 3 / 5f,
                rect.height - Margin - ButtonSize.y );
            var mineralsColumnRect = new Rect(
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

            // options
            Widgets_Section.BeginSectionColumn( optionsColumnRect, "Mining.Options", out position, out width );
            Widgets_Section.Section( ref position, width, DrawThresholdSettings, "FM.Threshold".Translate() );
            Widgets_Section.Section( ref position, width, DrawDeconstructBuildings );
            Widgets_Section.Section( ref position, width, DrawMiningArea, "FM.Mining.MiningArea".Translate() );
            Widgets_Section.Section( ref position, width, DrawRoofRoomChecks, "FM.Mining.HealthAndSafety".Translate() );
            Widgets_Section.EndSectionColumn( "Mining.Options", position );

            // minerals
            Widgets_Section.BeginSectionColumn( mineralsColumnRect, "Mining.Minerals", out position, out width );
            var refreshRect = new Rect(
                position.x + width - SmallIconSize - 2 * Margin,
                position.y                         + Margin,
                SmallIconSize,
                SmallIconSize );
            if ( Widgets.ButtonImage( refreshRect, Resources.Refresh, Color.grey ) )
                _selected.RefreshAllowedMinerals();
            Widgets_Section.Section( ref position, width, DrawAllowedMineralsShortcuts,
                                     "FM.Mining.AllowedMinerals".Translate() );
            Widgets_Section.Section( ref position, width, DrawAllowedMinerals );
            Widgets_Section.Section( ref position, width, DrawAllowedBuildingsShortcuts,
                                     "FM.Mining.AllowedBuildings".Translate() );
            Widgets_Section.Section( ref position, width, DrawAllowedBuildings );
            Widgets_Section.EndSectionColumn( "Mining.Minerals", position );

            // do the button
            if ( Event.current.control && Widgets.ButtonInvisible( buttonRect ) )
                Find.WindowStack.Add( new Dialog_MiningDebugOptions( _selected ) );
            if ( !_selected.Managed )
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Manage".Translate() ) )
                {
                    // activate job, add it to the stack
                    _selected.Managed = true;
                    Manager.For( manager ).JobStack.Add( _selected );

                    // refresh source list
                    Refresh();
                }
            }
            else
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Delete".Translate() ) )
                {
                    // inactivate job, remove from the stack.
                    Manager.For( manager ).JobStack.Delete( _selected );

                    // remove content from UI
                    _selected = null;

                    // refresh source list
                    Refresh();
                }
            }
        }

        public float DrawThresholdSettings( Vector2 pos, float width )
        {
            var start = pos;

            var currentCount    = _selected.Trigger.CurrentCount;
            var chunkCount      = _selected.GetCountInChunks();
            var designatedCount = _selected.GetCountInDesignations();
            var targetCount     = _selected.Trigger.TargetCount;

            _selected.Trigger.DrawTriggerConfig( ref pos, width, ListEntryHeight,
                                                 "FM.Mining.TargetCount".Translate(
                                                     currentCount, chunkCount, designatedCount, targetCount ),
                                                 "FM.Mining.TargetCount.Tip".Translate(
                                                     currentCount, chunkCount, designatedCount, targetCount ),
                                                 _selected.Designations,
                                                 delegate
                                                 {
                                                     _selected.Sync = ManagerJob_Mining.SyncDirection.FilterToAllowed;
                                                 },
                                                 _selected.DesignationLabel );

            Utilities.DrawToggle( ref pos, width,
                                  "FM.Mining.SyncFilterAndAllowed".Translate(),
                                  "FM.Mining.SyncFilterAndAllowed.Tip".Translate(),
                                  ref _selected.SyncFilterAndAllowed );
            Utilities.DrawReachabilityToggle( ref pos, width, ref _selected.CheckReachable );
            Utilities.DrawToggle( ref pos, width,
                                  "FM.PathBasedDistance".Translate(),
                                  "FM.PathBasedDistance.Tip".Translate(),
                                  ref _selected.PathBasedDistance,
                                  true );

            return pos.y - start.y;
        }

        public float DrawMiningArea( Vector2 pos, float width )
        {
            var start = pos;
            AreaAllowedGUI.DoAllowedAreaSelectors( ref pos, width, ref _selected.MiningArea, manager );
            return pos.y - start.y;
        }

        public float DrawDeconstructBuildings( Vector2 pos, float width )
        {
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            Utilities.DrawToggle( rowRect,
                                  "FM.Mining.DeconstructBuildings".Translate(),
                                  "FM.Mining.DeconstructBuildings.Tip".Translate(),
                                  ref _selected.DeconstructBuildings );
            return ListEntryHeight;
        }

        public float DrawRoofRoomChecks( Vector2 pos, float width )
        {
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            Utilities.DrawToggle( rowRect, "FM.Mining.CheckRoofSupport".Translate(),
                                  "FM.Mining.CheckRoofSupport.Tip".Translate(), ref _selected.CheckRoofSupport );

            rowRect.y += ListEntryHeight;
            if ( _selected.CheckRoofSupport )
                Utilities.DrawToggle( rowRect, "FM.Mining.CheckRoofSupportAdvanced".Translate(),
                                      "FM.Mining.CheckRoofSupportAdvanced.Tip".Translate(),
                                      ref _selected.CheckRoofSupportAdvanced, true );
            else
                Widgets_Labels.Label( rowRect, "FM.Mining.CheckRoofSupportAdvanced".Translate(),
                                      "FM.Mining.CheckRoofSupportAdvanced.Disabled.Tip".Translate(),
                                      TextAnchor.MiddleLeft, margin: Margin,
                                      color: Color.grey );

            rowRect.y += ListEntryHeight;
            Utilities.DrawToggle( rowRect, "FM.Mining.CheckRoomDivision".Translate(),
                                  "FM.Mining.CheckRoomDivision.Tip".Translate(), ref _selected.CheckRoomDivision,
                                  true );

            return rowRect.yMax - pos.y;
        }

        public float DrawAllowedMineralsShortcuts( Vector2 pos, float width )
        {
            var start = pos;

            // list of keys in allowed animals list (all animals in biome + visible animals on map)
            var allowedMinerals = _selected.AllowedMinerals;
            var minerals        = new List<ThingDef>( allowedMinerals.Keys );

            // toggle all
            Utilities.DrawToggle( ref pos, width,
                                  "FM.All".Translate().Italic(),
                                  string.Empty,
                                  _selected.AllowedMinerals.Values.All( v => v ),
                                  _selected.AllowedMinerals.Values.All( v => !v ),
                                  () => minerals.ForEach( p => _selected.SetAllowMineral( p, true ) ),
                                  () => minerals.ForEach( p => _selected.SetAllowMineral( p, false ) ) );

            // toggle stone
            var stone = minerals.Where( m => !m.building.isResourceRock ).ToList();
            Utilities.DrawToggle( ref pos, width,
                                  "FM.Mining.Stone".Translate().Italic(),
                                  "FM.Mining.Stone.Tip".Translate(),
                                  stone.All( p => allowedMinerals[p] ),
                                  stone.All( p => !allowedMinerals[p] ),
                                  () => stone.ForEach( p => _selected.SetAllowMineral( p, true ) ),
                                  () => stone.ForEach( p => _selected.SetAllowMineral( p, false ) ) );

            // toggle metal
            var metal = minerals.Where( m => m.building.isResourceRock && IsMetal( m.building.mineableThing ) )
                                .ToList();
            Utilities.DrawToggle( ref pos, width,
                                  "FM.Mining.Metal".Translate().Italic(),
                                  "FM.Mining.Metal.Tip".Translate(),
                                  metal.All( p => allowedMinerals[p] ),
                                  metal.All( p => !allowedMinerals[p] ),
                                  () => metal.ForEach( p => _selected.SetAllowMineral( p, true ) ),
                                  () => metal.ForEach( p => _selected.SetAllowMineral( p, false ) ) );

            // toggle precious
            var precious = minerals
                          .Where( m => m.building.isResourceRock && ( m.building.mineableThing?.smallVolume ?? false ) )
                          .ToList();
            Utilities.DrawToggle( ref pos, width,
                                  "FM.Mining.Precious".Translate().Italic(),
                                  "FM.Mining.Precious.Tip".Translate(),
                                  precious.All( p => allowedMinerals[p] ),
                                  precious.All( p => !allowedMinerals[p] ),
                                  () => precious.ForEach( p => _selected.SetAllowMineral( p, true ) ),
                                  () => precious.ForEach( p => _selected.SetAllowMineral( p, false ) ) );

            return pos.y - start.y;
        }

        public bool IsMetal( ThingDef def )
        {
            return def != null && _metals.Contains( def );
        }

        public float DrawAllowedMinerals( Vector2 pos, float width )
        {
            var start = pos;
            // list of keys in allowed animals list (all animals in biome + visible animals on map)
            var allowedMinerals = _selected.AllowedMinerals;
            var minerals        = new List<ThingDef>( allowedMinerals.Keys );

            // toggle for each animal
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            foreach ( var mineral in minerals )
            {
                // draw the toggle
                Utilities.DrawToggle( rowRect, mineral.LabelCap, mineral.description,
                                      _selected.AllowedMinerals[mineral],
                                      () => _selected.SetAllowMineral( mineral, !_selected.AllowedMinerals[mineral] ) );
                rowRect.y += ListEntryHeight;
            }

            return rowRect.yMin - start.y;
        }


        public float DrawAllowedBuildingsShortcuts( Vector2 pos, float width )
        {
            var start = pos;

            // list of keys in allowed animals list (all animals in biome + visible animals on map)
            var allowedBuildings = _selected.AllowedBuildings;
            var buildings        = new List<ThingDef>( allowedBuildings.Keys );

            // toggle all
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            Utilities.DrawToggle( rowRect,
                                  "FM.All".Translate().Italic(),
                                  string.Empty,
                                  allowedBuildings.Values.All( v => v ),
                                  allowedBuildings.Values.All( v => !v ),
                                  () => buildings.ForEach( b => _selected.SetAllowBuilding( b, true ) ),
                                  () => buildings.ForEach( b => _selected.SetAllowBuilding( b, false ) ) );

            return rowRect.yMax - start.y;
        }

        public float DrawAllowedBuildings( Vector2 pos, float width )
        {
            var start = pos;

            var allowedBuildings = _selected.AllowedBuildings;
            var buildings        = new List<ThingDef>( allowedBuildings.Keys );

            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            foreach ( var building in buildings )
            {
                Utilities.DrawToggle( rowRect, building.LabelCap, building.description, allowedBuildings[building],
                                      () => _selected.SetAllowBuilding( building, !allowedBuildings[building] ) );
                rowRect.y += ListEntryHeight;
            }

            return rowRect.yMin - start.y;
        }

        private void DoJobList( Rect rect )
        {
            Widgets.DrawMenuSection( rect );

            // content
            var height     = _jobListHeight;
            var scrollView = new Rect( 0f, 0f, rect.width, height );
            if ( height > rect.height )
                scrollView.width -= ScrollbarWidth;

            Widgets.BeginScrollView( rect, ref _jobListScrollPosition, scrollView );
            var scrollContent = scrollView;

            GUI.BeginGroup( scrollContent );
            var cur = Vector2.zero;
            var i   = 0;

            foreach ( var job in Jobs )
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

            if ( i++ % 2 == 1 ) Widgets.DrawAltRect( newRect );

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( newRect, "<" + "FM.Mining.NewJob".Translate() + ">" );
            Text.Anchor = TextAnchor.UpperLeft;

            if ( Widgets.ButtonInvisible( newRect ) ) Selected = new ManagerJob_Mining( manager );

            TooltipHandler.TipRegion( newRect, "FM.Mining.NewJob.Tip".Translate() );

            cur.y += LargeListEntryHeight;

            _jobListHeight = cur.y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public override void PreOpen()
        {
            Refresh();
        }

        public void Refresh()
        {
            // upate our list of jobs
            Jobs = Manager.For( manager ).JobStack.FullStack<ManagerJob_Mining>();

            // update pawnkind options
            foreach ( var job in Jobs )
                job.RefreshAllowedMinerals();
            _selected?.RefreshAllowedMinerals();
        }
    }
}