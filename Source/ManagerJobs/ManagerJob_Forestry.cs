// Karel Kroeze
// ManagerJob_Forestry.cs
// 2016-12-09

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

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

        #region Fields

        public ForestryJobType type = ForestryJobType.Logging;
        public Dictionary<ThingDef, bool> AllowedTrees = new Dictionary<ThingDef, bool>();
        public bool AllowSaplings;
        public Dictionary<Area, bool> ClearAreas = new Dictionary<Area, bool>();
        public List<Designation> Designations = new List<Designation>();
        public History History;
        public Area LoggingArea;
        public new Trigger_Threshold Trigger;
        private static WorkTypeDef PlantCutting = DefDatabase<WorkTypeDef>.GetNamed( "PlantCutting" );
        private readonly float _margin = Utilities.Margin;
        private List<bool> _clearAreas_allowed;
        private List<Area> _clearAreas_areas;
        private Utilities.CachedValue<int> _designatedWoodCachedValue = new Utilities.CachedValue<int>();

        #endregion Fields

        #region Constructors

        public ManagerJob_Forestry( Manager manager ) : base( manager )
        {
            // populate the trigger field, set the root category to wood.
            Trigger = new Trigger_Threshold( this );
            Trigger.ThresholdFilter.SetDisallowAll();
            Trigger.ThresholdFilter.SetAllow( Utilities_Forestry.Wood, true );

            // initialize clearAreas list with current areas
            UpdateClearAreas();

            History = new History( new[] { "stock", "designated" }, new[] { Color.white, Color.grey } );


            // init stuff if we're not loading
            // todo: please, please refactor this into something less clumsy!
            if (Scribe.mode == LoadSaveMode.Inactive)
                RefreshAllowedTrees();
        }

        #endregion Constructors



        #region Properties

        public override bool Completed
        {
            get
            {
                switch ( type )
                {
                    case ForestryJobType.Logging:
                        return !Trigger.State;
                    default:
                        return false;
                }
            }
        }

        public override string Label
        {
            get { return "FMF.Forestry".Translate(); }
        }

        public string SubLabel( Rect rect )
        {
            switch ( type )
                {
                    case ForestryJobType.Logging:
                        var sublabel = string.Join(", ", Targets);
                        if (sublabel.Fits(rect))
                            return sublabel.Italic();
                        else
                            return "multiple".Translate().Italic();
                    default:
                        return ( "FMF.JobType." + type ).Translate().Italic();
            }
        }

        public override ManagerTab Tab
        {
            get { return Manager.For( manager ).ManagerTabs.Find( tab => tab is ManagerTab_Forestry ); }
        }

        public override string[] Targets
        {
            get
            {
                return AllowedTrees.Keys.Where( key => AllowedTrees[key] ).Select( tree => tree.LabelCap ).ToArray();
            }
        }

        public override WorkTypeDef WorkTypeDef => PlantCutting;

        #endregion Properties



        #region Methods

        public void AddRelevantGameDesignations()
        {
            // get list of game designations not managed by this job that could have been assigned by this job.
            foreach ( Designation des in manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.CutPlant )
                                                .Except( Designations )
                                                .Where( des => IsValidForestryTarget( des.target ) ) )
            {
                AddDesignation( des );
            }
        }

        /// <summary>
        /// Remove obsolete designations from the list.
        /// </summary>
        public void CleanDesignations()
        {
            // get the intersection of bills in the game and bills in our list.
            List<Designation> gameDesignations = manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.HarvestPlant ).ToList();
            Designations = Designations.Intersect( gameDesignations ).ToList();
        }

        public override void CleanUp()
        {
            // clear the list of obsolete designations
            CleanDesignations();

            // cancel outstanding designation
            foreach ( Designation des in Designations )
            {
                des.Delete();
            }

            // clear the list completely
            Designations.Clear();
        }

        public void DoClearAreaDesignations( IEnumerable<IntVec3> cells, bool allPlants, ref bool workDone )
        {
            var map = manager.map;
            var designationManager = map.designationManager;

            foreach ( IntVec3 cell in cells )
            {
                // confirm there is a plant here that it is a tree and that it has no current designation
                Plant plant = cell.GetPlant( map );

                // if there is no plant, or there is already a designation here, bail out
                if (plant == null || !designationManager.AllDesignationsOn(plant).ToList().NullOrEmpty())
                    continue;
                
                // if the plant is not a tree, and allPlants is not set, bail.
                if ( !( allPlants || plant.def.plant.IsTree ) )
                    continue;
                
                // we don't cut stuff in growing zones
                var zone = map.zoneManager.ZoneAt( cell ) as IPlantToGrowSettable;
                if ( zone != null )
                    continue;

                // nor in plant pots (or hydroponics)
                var pot = map.thingGrid.ThingsListAt( cell ).FirstOrDefault( t => t is Building_PlantGrower );
                if ( pot != null )
                    continue;

                // there's no reason not to cut it down, so cut it down.
                designationManager.AddDesignation( new Designation( plant, DesignationDefOf.CutPlant ) );
                workDone = true;
            }
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | (bar | last update)/(stamp) -> handled in Utilities.DrawStatusForListEntry
            int shownTargets = overview ? 4 : 3; // there's more space on the overview

            // set up rects
            Rect labelRect = new Rect( _margin, _margin, rect.width -
                                                         ( active ? StatusRectWidth + 4 * _margin : 2 * _margin ),
                                       rect.height - 2 * _margin ),
                 statusRect = new Rect( labelRect.xMax + _margin, _margin, StatusRectWidth, rect.height - 2 * _margin );
            
            // create label string
            string subtext = SubLabel( labelRect );
            string text = Label + "\n" + subtext;

            // do the drawing
            GUI.BeginGroup( rect );

            // draw label
            Utilities.Label( labelRect, text, subtext, TextAnchor.MiddleLeft, _margin );

            // if the bill has a manager job, give some more info.
            if ( active )
                this.DrawStatusForListEntry( statusRect, Trigger );
            
            GUI.EndGroup();
        }

        public override void DrawOverviewDetails( Rect rect )
        {
            History.DrawPlot( rect, Trigger.Count );
        }

        public override void ExposeData()
        {
            // scribe base things
            base.ExposeData();

            // settings, references first!
            Scribe_References.Look( ref LoggingArea, "LoggingArea" );
            Scribe_Deep.Look( ref Trigger, "trigger", manager );
            Scribe_Collections.Look( ref AllowedTrees, "AllowedTrees", LookMode.Def, LookMode.Value );
            Scribe_Values.Look( ref type, "type", ForestryJobType.Logging );
            Scribe_Values.Look( ref AllowSaplings, "AllowSaplings", false );

            // clearing areas list
            if ( Scribe.mode == LoadSaveMode.Saving )
            {
                // make sure areas list doesn't contain deleted areas
                UpdateClearAreas();

                // create scribe helper vars
                _clearAreas_areas = new List<Area>( ClearAreas.Keys );
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
                {
                    if ( _clearAreas_areas[i] != null )
                        ClearAreas.Add( _clearAreas_areas[i], _clearAreas_allowed[i] );
                }
            }

            if ( Manager.LoadSaveMode == Manager.Modes.Normal )
            {
                // scribe history
                Scribe_Deep.Look( ref History, "History" );
            }
        }

        public int GetWoodInDesignations()
        {
            var count = 0;

            // try get cache
            if ( _designatedWoodCachedValue.TryGetValue( out count ) )
            {
                return count;
            }

            foreach ( Designation des in Designations )
            {
                if ( des.target.HasThing &&
                     des.target.Thing is Plant )
                {
                    var plant = des.target.Thing as Plant;
                    count += plant.YieldNow();
                }
            }

            // update cache
            _designatedWoodCachedValue.Update( count );

            return count;
        }

        public override void Tick()
        {
            History.Update( Trigger.CurCount, GetWoodInDesignations() );
        }

        public override bool TryDoJob()
        {
            // keep track if any actual work was done.
            var workDone = false;

            // clean dead designations
            CleanDesignations();

            switch ( type )
            {
                    case ForestryJobType.Logging:
                        DoLoggingJob( ref workDone );
                        break;
                    case ForestryJobType.ClearWind:
                        DoClearAreaDesignations( GetWindCells(), false, ref workDone );
                        break;
                    case ForestryJobType.ClearArea:
                        if ( ClearAreas.Any() )
                            DoClearAreas( ref workDone );
                        break;
            }
            
            return workDone;
        }

        private void DoLoggingJob( ref bool workDone )
        {
            // remove designations not in zone.
            if (LoggingArea != null)
                CleanAreaDesignations();

            // add external designations
            AddRelevantGameDesignations();
            
            // get current lumber count
            int count = Trigger.CurCount + GetWoodInDesignations();

            // get sorted list of loggable trees
            List<Plant> trees = GetLoggableTreesSorted();

            // designate untill we're either out of trees or we have enough designated.
            for (var i = 0; i < trees.Count && count < Trigger.Count; i++)
            {
                workDone = true;
                AddDesignation(trees[i], DesignationDefOf.HarvestPlant);
                count += trees[i].YieldNow();
            }
        }
        
        internal void UpdateClearAreas()
        {
            // init list of areas
            if ( ClearAreas == null || ClearAreas.Count == 0 )
                ClearAreas =
                    manager.map.areaManager.AllAreas.Where(
                                                           area => area.AssignableAsAllowed( AllowedAreaMode.Humanlike ) )
                           .ToDictionary( a => a, v => false );
            else
            {
                // iterate over areas, add new areas.
                foreach (
                    Area area in
                        manager.map.areaManager.AllAreas.Where( a => a.AssignableAsAllowed( AllowedAreaMode.Humanlike ) )
                    )
                {
                    if ( !ClearAreas.ContainsKey( area ) )
                        ClearAreas.Add( area, false );
                }

                // iterate over existing areas, clear deleted areas.
                var Areas = new List<Area>( ClearAreas.Keys );
                foreach ( Area area in Areas )
                {
                    if ( !manager.map.areaManager.AllAreas.Contains( area ) )
                    {
                        ClearAreas.Remove( area );
                    }
                }
            }
        }

        private void AddDesignation( Designation des )
        {
            // add to game
            manager.map.designationManager.AddDesignation( des );

            // add to internal list
            Designations.Add( des );
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
            foreach ( Designation des in Designations )
            {
                if ( !des.target.HasThing )
                    des.Delete();
                else if ( !LoggingArea.ActiveCells.Contains( des.target.Thing.Position ) )
                    des.Delete();
            }
        }

        private void DoClearAreas( ref bool workDone )
        {
            foreach ( KeyValuePair<Area, bool> area in ClearAreas )
            {
                if ( area.Value )
                    DoClearAreaDesignations( area.Key.ActiveCells, true, ref workDone );
            }
        }

        private List<Plant> GetLoggableTreesSorted()
        {
            IntVec3 position = manager.map.GetBaseCenter();

            // get a list of trees that are not designated in the logging grounds and are reachable, sorted by yield / distance * 2
            List<Plant> list = manager.map.listerThings.AllThings.Where( IsValidForestryTarget )

                                      // OrderBy defaults to ascending, switch sign on current yield to get descending
                                      .Select( p => p as Plant )
                                      .OrderBy( p => -p.YieldNow() /
                                                     ( Math.Sqrt( position.DistanceToSquared( p.Position ) ) * 2 ) )
                                      .ToList();

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
                   && IsValidForestryTarget( (Plant)t );
        }

        private bool IsValidForestryTarget( Plant p )
        {
            return p.def.plant != null

                   // non-biome trees won't be on the list
                   && AllowedTrees.ContainsKey( p.def )

                   // also filters out non-tree plants
                   && AllowedTrees[p.def]
                   && p.Spawned
                   && manager.map.designationManager.DesignationOn( p ) == null

                   // cut only mature trees, or saplings that yield something right now.
                   && ( ( AllowSaplings && p.YieldNow() > 1 ) || p.LifeStage == PlantLifeStage.Mature )
                   && ( LoggingArea == null || LoggingArea.ActiveCells.Contains( p.Position ) )
                   && manager.map.reachability.CanReachColony( p.Position );
        }

        #endregion Methods

        public void RefreshAllowedTrees()
        {
            // A tree is defined as any plant that yields wood or has a wood harvesting tag.
            var options = manager.map.Biome.AllWildPlants.Where(
                                                                pd =>
                                                                    pd.plant.harvestTag == "Wood" ||
                                                                    pd.plant.harvestedThingDef == Utilities_Forestry.Wood );

            foreach ( ThingDef tree in options )
            {
                if (!AllowedTrees.ContainsKey( tree ))
                    AllowedTrees.Add( tree, false );
            }
        }
    }
}
