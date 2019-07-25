// Karel Kroeze
// ManagerJob_Forestry.cs
// 2016-12-09

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class ManagerJob_Forestry : ManagerJob
    {
        public enum ForestryJobType
        {
            ClearArea,
            ClearWind,
            Logging
        }

        private static readonly WorkTypeDef PlantCutting =
            DefDatabase<WorkTypeDef>.GetNamed( "PlantCutting" );

        private List<bool> _clearAreas_allowed;
        private List<Area> _clearAreas_areas;

        private readonly Utilities.CachedValue<int>
            _designatedWoodCachedValue = new Utilities.CachedValue<int>();

        private List<Designation> _designations = new List<Designation>();

        // backwards compatibility for new clear jobs
        // TODO: REMOVE ON NEXT BREAKING VERSION!
        private bool _newClearJobs;

        private    ForestryJobType            _type        = ForestryJobType.Logging;
        public     Dictionary<ThingDef, bool> AllowedTrees = new Dictionary<ThingDef, bool>();
        public     bool                       AllowSaplings;
        public     Dictionary<Area, bool>     ClearAreas = new Dictionary<Area, bool>();
        public     bool                       ClearWindCells;
        public     History                    History;
        public     Area                       LoggingArea;
        public new Trigger_Threshold          Trigger;

        public ManagerJob_Forestry( Manager manager ) : base( manager )
        {
            // populate the trigger field, set the root category to wood.
            Trigger = new Trigger_Threshold( this );
            Trigger.ThresholdFilter.SetDisallowAll();
            Trigger.ThresholdFilter.SetAllow( ThingDefOf.WoodLog, true );

            // initialize clearAreas list with current areas
            UpdateClearAreas();

            History = new History( new[] {"stock", "designated"}, new[] {Color.white, Color.grey} );


            // init stuff if we're not loading
            // todo: please, please refactor this into something less clumsy!
            if ( Scribe.mode == LoadSaveMode.Inactive )
                RefreshAllowedTrees();
        }

        public List<Designation> Designations => new List<Designation>( _designations );

        public ForestryJobType Type
        {
            get => _type;
            set
            {
                _type = value;
                RefreshAllowedTrees();
            }
        }

        public override bool Completed
        {
            get
            {
                switch ( Type )
                {
                    case ForestryJobType.Logging:
                        return !Trigger.State;
                    default:
                        return false;
                }
            }
        }

        public override string Label => "FMF.Forestry".Translate();

        public override ManagerTab Tab
        {
            get { return manager.Tabs.Find( tab => tab is ManagerTab_Forestry ); }
        }

        public override bool IsValid => base.IsValid && Trigger != null && History != null;

        public override string[] Targets
        {
            get
            {
                switch ( Type )
                {
                    case ForestryJobType.Logging:
                        return AllowedTrees.Keys.Where( key => AllowedTrees[key] ).Select( tree => tree.LabelCap )
                                           .ToArray();
                    default:
                        var targets = ClearAreas
                                     .Where( ca => ca.Value )
                                     .Select( ca => ca.Key.Label );
                        if ( ClearWindCells )
                            targets = targets.Concat( "FMF.TurbineArea".Translate() );
                        if ( !targets.Any() )
                            return new[] {"FM.None".Translate()};
                        return targets.ToArray();
                }
            }
        }

        public override WorkTypeDef WorkTypeDef => PlantCutting;

        public string SubLabel( Rect rect )
        {
            string sublabel;
            switch ( Type )
            {
                case ForestryJobType.Logging:
                    sublabel = string.Join( ", ", Targets );
                    if ( sublabel.Fits( rect ) )
                        return sublabel.Italic();
                    else
                        return "multiple".Translate().Italic();
                default:
                    sublabel = "FMF.Clear".Translate( string.Join( ", ", Targets ) );
                    if ( sublabel.Fits( rect ) )
                        return sublabel.Italic();
                    else
                        return "FMF.Clear".Translate( "multiple".Translate() ).Italic();
            }
        }

        public string DesignationLabel( Designation designation )
        {
            // label, dist, yield.
            var plant = designation.target.Thing as Plant;
            return "Fluffy.Manager.DesignationLabel".Translate(
                plant.LabelCap,
                Distance( plant, manager.map.GetBaseCenter() ).ToString( "F0" ),
                plant.YieldNow(),
                plant.def.plant.harvestedThingDef.LabelCap );
        }

        public void AddRelevantGameDesignations()
        {
            // get list of game designations not managed by this job that could have been assigned by this job.
            foreach ( var des in manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.CutPlant )
                                        .Except( _designations )
                                        .Where( des => IsValidForestryTarget( des.target ) ) )
                AddDesignation( des );
        }

        /// <summary>
        ///     Remove obsolete designations from the list.
        /// </summary>
        public void CleanDesignations()
        {
            // get the intersection of bills in the game and bills in our list.
            var gameDesignations = manager.map.designationManager
                                          .SpawnedDesignationsOfDef( DesignationDefOf.HarvestPlant ).ToList();
            _designations = _designations.Intersect( gameDesignations ).ToList();
        }

        public override void CleanUp()
        {
            // clear the list of obsolete designations
            CleanDesignations();

            // cancel outstanding designation
            foreach ( var des in _designations ) des.Delete();

            // clear the list completely
            _designations.Clear();
        }

        public void DoClearAreaDesignations( IEnumerable<IntVec3> cells, ref bool workDone )
        {
            var map                = manager.map;
            var designationManager = map.designationManager;

            foreach ( var cell in cells )
            {
                // confirm there is a plant here that it is a tree and that it has no current designation
                var plant = cell.GetPlant( map );

                // if there is no plant, or there is already a designation here, bail out
                if ( plant == null || designationManager.AllDesignationsOn( plant ).Any() )
                    continue;

                // if the plant is not in the allowed filter
                if ( !AllowedTrees.ContainsKey( plant.def ) || !AllowedTrees[plant.def] )
                    continue;

                // we don't cut stuff in growing zones
                if ( map.zoneManager.ZoneAt( cell ) is IPlantToGrowSettable )
                    continue;

                // nor in plant pots (or hydroponics)
                if ( map.thingGrid.ThingsListAt( cell ).Any( t => t is Building_PlantGrower ) )
                    continue;

                // there's no reason not to cut it down, so cut it down.
                designationManager.AddDesignation( new Designation( plant, DesignationDefOf.CutPlant ) );
                workDone = true;
            }
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | (bar | last update)/(stamp) -> handled in Utilities.DrawStatusForListEntry
            var shownTargets = overview ? 4 : 3; // there's more space on the overview

            // set up rects
            Rect labelRect = new Rect( Margin, Margin, rect.width -
                                                       ( active ? StatusRectWidth + 4 * Margin : 2 * Margin ),
                                       rect.height - 2 * Margin ),
                 statusRect = new Rect( labelRect.xMax + Margin, Margin, StatusRectWidth, rect.height - 2 * Margin );

            // create label string
            var subtext = SubLabel( labelRect );
            var text    = Label + "\n" + subtext;

            // do the drawing
            GUI.BeginGroup( rect );

            // draw label
            Widgets_Labels.Label( labelRect, text, subtext, TextAnchor.MiddleLeft, margin: Margin );

            // if the bill has a manager job, give some more info.
            if ( active )
                this.DrawStatusForListEntry( statusRect, Trigger );

            GUI.EndGroup();
        }

        public override void DrawOverviewDetails( Rect rect )
        {
            History.DrawPlot( rect, Trigger.TargetCount );
        }

        public override void ExposeData()
        {
            // scribe base things
            base.ExposeData();

            // settings, references first!
            Scribe_References.Look( ref LoggingArea, "LoggingArea" );
            Scribe_Deep.Look( ref Trigger, "trigger", manager );
            Scribe_Collections.Look( ref AllowedTrees, "AllowedTrees", LookMode.Def, LookMode.Value );
            Scribe_Values.Look( ref _type, "type", ForestryJobType.Logging );
            Scribe_Values.Look( ref AllowSaplings, "AllowSaplings" );
            Scribe_Values.Look( ref ClearWindCells, "ClearWindCells" );

            // backwards compatibility for clearing jobs
            // TODO: REMOVE ON NEXT BREAKING VERSION!
            Scribe_Values.Look( ref _newClearJobs, "NEW_CLEAR_JOBS" );

            // clearing areas list
            if ( Scribe.mode == LoadSaveMode.Saving )
            {
                // make sure areas list doesn't contain deleted areas
                UpdateClearAreas();

                // create scribe helper vars
                _clearAreas_areas   = new List<Area>( ClearAreas.Keys );
                _clearAreas_allowed = new List<bool>( ClearAreas.Values );
            }

            // scribe that stuff
            Scribe_Collections.Look( ref _clearAreas_areas, "ClearAreas_areas", LookMode.Reference );
            Scribe_Collections.Look( ref _clearAreas_allowed, "ClearAreas_allowed", LookMode.Value );

            // initialize areas dict from scribe helpers
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                ClearAreas = new Dictionary<Area, bool>();
                for ( var i = 0; i < _clearAreas_areas.Count; i++ )
                    if ( _clearAreas_areas[i] != null )
                        ClearAreas.Add( _clearAreas_areas[i], _clearAreas_allowed[i] );
            }

            if ( Manager.LoadSaveMode == Manager.Modes.Normal )
                // scribe history
                Scribe_Deep.Look( ref History, "History" );
        }

        public int GetWoodInDesignations()
        {
            var count = 0;

            // try get cache
            if ( _designatedWoodCachedValue.TryGetValue( out count ) ) return count;

            foreach ( var des in _designations )
                if ( des.target.HasThing &&
                     des.target.Thing is Plant )
                {
                    var plant = des.target.Thing as Plant;
                    count += plant.YieldNow();
                }

            // update cache
            _designatedWoodCachedValue.Update( count );

            return count;
        }

        public override void Tick()
        {
            History.Update( Trigger.CurrentCount, GetWoodInDesignations() );
        }

        public override bool TryDoJob()
        {
            // keep track if any actual work was done.
            var workDone = false;

            // clean dead designations
            CleanDesignations();

            switch ( Type )
            {
                case ForestryJobType.Logging:
                    DoLoggingJob( ref workDone );
                    break;
                case ForestryJobType.ClearArea:
                    if ( ClearWindCells )
                        DoClearAreaDesignations( GetWindCells(), ref workDone );
                    if ( ClearAreas.Any() )
                        DoClearAreas( ref workDone );
                    break;
            }

            return workDone;
        }

        private void DoLoggingJob( ref bool workDone )
        {
            // remove designations not in zone.
            if ( LoggingArea != null )
                CleanAreaDesignations();

            // add external designations
            AddRelevantGameDesignations();

            // get current lumber count
            var count = Trigger.CurrentCount + GetWoodInDesignations();

            // get sorted list of loggable trees
            var trees = GetLoggableTreesSorted();

            // designate untill we're either out of trees or we have enough designated.
            for ( var i = 0; i < trees.Count && count < Trigger.TargetCount; i++ )
            {
                workDone = true;
                AddDesignation( trees[i], DesignationDefOf.HarvestPlant );
                count += trees[i].YieldNow();
            }
        }

        internal void UpdateClearAreas()
        {
            // init list of areas
            if ( ClearAreas == null || ClearAreas.Count == 0 )
            {
                ClearAreas =
                    manager.map.areaManager.AllAreas.Where( area => area.AssignableAsAllowed() )
                           .ToDictionary( a => a, v => false );
            }
            else
            {
                // iterate over areas, add new areas.
                foreach ( var area in manager.map.areaManager.AllAreas.Where( a => a.AssignableAsAllowed() ) )
                    if ( !ClearAreas.ContainsKey( area ) )
                        ClearAreas.Add( area, false );

                // iterate over existing areas, clear deleted areas.
                var Areas = new List<Area>( ClearAreas.Keys );
                foreach ( var area in Areas )
                    if ( !manager.map.areaManager.AllAreas.Contains( area ) )
                        ClearAreas.Remove( area );
            }
        }

        private void AddDesignation( Designation des )
        {
            // add to game
            manager.map.designationManager.AddDesignation( des );

            // add to internal list
            _designations.Add( des );
        }

        private void AddDesignation( Plant p, DesignationDef def = null )
        {
            // create designation
            var des = new Designation( p, def );

            // pass to adder
            AddDesignation( des );
        }

        private void CleanAreaDesignations()
        {
            foreach ( var des in _designations )
                if ( !des.target.HasThing )
                    des.Delete();
                else if ( !LoggingArea.ActiveCells.Contains( des.target.Thing.Position ) )
                    des.Delete();
        }

        private void DoClearAreas( ref bool workDone )
        {
            foreach ( var area in ClearAreas )
                if ( area.Value )
                    DoClearAreaDesignations( area.Key.ActiveCells, ref workDone );
        }

        private List<Plant> GetLoggableTreesSorted()
        {
            var position = manager.map.GetBaseCenter();

#if DEBUG_PERFORMANCE
            DeepProfiler.Start( "GetLoggableTreesSorted" );
#endif
            var list = manager.map.listerThings.AllThings.Where( IsValidForestryTarget )
                              .Select( p => p as Plant )
                              .OrderByDescending( p => p.YieldNow() / Distance( p, position ) )
                              .ToList();

#if DEBUG_PERFORMANCE
            DeepProfiler.End();
#endif

            return list;
        }

        private List<IntVec3> GetWindCells()
        {
            return manager.map.listerBuildings
                          .allBuildingsColonist
                          .Where( b => b.GetComp<CompPowerPlantWind>() != null )
                          .SelectMany( turbine => WindTurbineUtility.CalculateWindCells( turbine.Position,
                                                                                         turbine.Rotation,
                                                                                         turbine.RotatedSize ) )
                          .ToList();
        }

        private bool IsInWindTurbineArea( IntVec3 position )
        {
            return GetWindCells().Contains( position );
        }

        private bool IsValidForestryTarget( LocalTargetInfo t )
        {
            return t.HasThing
                && IsValidForestryTarget( t.Thing );
        }

        private bool IsValidForestryTarget( Thing t )
        {
            return t is Plant
                && IsValidForestryTarget( (Plant) t );
        }

        private bool IsValidForestryTarget( Plant target )
        {
            return target.def.plant != null

                   // non-biome trees won't be on the list
                && AllowedTrees.ContainsKey( target.def )

                   // also filters out non-tree plants
                && AllowedTrees[target.def]
                && target.Spawned
                && manager.map.designationManager.DesignationOn( target ) == null

                   // cut only mature trees, or saplings that yield something right now.
                && ( AllowSaplings || target.LifeStage == PlantLifeStage.Mature ) && target.YieldNow() > 1
                && ( LoggingArea                       == null || LoggingArea.ActiveCells.Contains( target.Position ) )

                   // reachable
                && IsReachable( target );
        }

        public void RefreshAllowedTrees()
        {
            Logger.Debug( "Refreshing allowed trees" );

            // all plants
            var options = manager.map.Biome.AllWildPlants

                                  // cave plants (shrooms)
                                 .Concat( DefDatabase<ThingDef>.AllDefsListForReading
                                                               .Where( td => td.plant?.cavePlant ?? false ) )

                                  // ambrosia
                                 .Concat( ThingDefOf.Plant_Ambrosia )

                                  // and anything on the map that is not in a plant zone/planter
                                 .Concat( manager.map.listerThings.AllThings.OfType<Plant>()
                                                 .Where( p => p.Spawned &&
                                                              !( manager.map.zoneManager.ZoneAt( p.Position ) is
                                                                  IPlantToGrowSettable ) &&
                                                              manager.map.thingGrid.ThingsAt( p.Position )
                                                                     .FirstOrDefault(
                                                                          t => t is Building_PlantGrower ) == null )
                                                 .Select( p => p.def ) )

                                  // add stuff in the current list
                                 .Concat( AllowedTrees.Keys.ToList() )

                                  // if type == logging, remove things that do not yield wood
                                 .Where( td => Type == ForestryJobType.ClearArea ||
                                               ( td.plant.harvestTag        == "Wood" ||
                                                 td.plant.harvestedThingDef == ThingDefOf.WoodLog ) &&
                                               td.plant.harvestYield > 0 )
                                 .Distinct();

            // remove stuff not in new list
            foreach ( var tree in AllowedTrees.Keys.ToList() )
                if ( !options.Contains( tree ) )
                    AllowedTrees.Remove( tree );

            // add stuff not in current list
            foreach ( var tree in options )
                if ( !AllowedTrees.ContainsKey( tree ) )
                    AllowedTrees.Add( tree, false );

            // sort
            AllowedTrees = AllowedTrees.OrderBy( at => at.Key.LabelCap )
                                       .ToDictionary( at => at.Key, at => at.Value );
        }
    }
}