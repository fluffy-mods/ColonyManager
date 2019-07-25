// Karel Kroeze
// ManagerJob_Foraging.cs
// 2016-12-09

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class ManagerJob_Foraging : ManagerJob
    {
        private readonly Utilities.CachedValue<int> _cachedCurrentDesignatedCount = new Utilities.CachedValue<int>();

        private List<Designation> _designations = new List<Designation>();

        public     Dictionary<ThingDef, bool> AllowedPlants = new Dictionary<ThingDef, bool>();
        public     Area                       ForagingArea;
        public     bool                       ForceFullyMature;
        public     History                    History;
        public new Trigger_Threshold          Trigger;

        public override bool IsValid => base.IsValid && Trigger != null && History != null;

        public ManagerJob_Foraging( Manager manager ) : base( manager )
        {
            // populate the trigger field, count all harvested thingdefs from the allowed plant list
            Trigger = new Trigger_Threshold( this );

            // create History tracker
            History = new History( new[] {"stock", "designated"}, new[] {Color.white, Color.grey} );

            // init stuff if we're not loading
            // todo: please, please refactor this into something less clumsy!
            if ( Scribe.mode == LoadSaveMode.Inactive )
                RefreshAllowedPlants();
        }

        public List<Designation> Designations => new List<Designation>( _designations );

        public override bool Completed => !Trigger.State;

        public int CurrentDesignatedCount
        {
            get
            {
                var count = 0;

                // see if we have a cached count
                if ( _cachedCurrentDesignatedCount.TryGetValue( out count ) )
                    return count;

                // fetch count
                foreach ( var des in _designations )
                {
                    if ( !des.target.HasThing )
                        continue;

                    var plant = des.target.Thing as Plant;

                    if ( plant == null )
                        continue;

                    count += plant.YieldNow();
                }

                _cachedCurrentDesignatedCount.Update( count );
                return count;
            }
        }

        public override string Label => "FMG.Foraging".Translate();

        public override ManagerTab Tab => Manager.For( manager ).Tabs.Find( tab => tab is ManagerTab_Foraging );

        public override string[] Targets
            => AllowedPlants.Keys.Where( key => AllowedPlants[key] ).Select( plant => plant.LabelCap ).ToArray();

        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Growing;

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
            foreach (
                var des in manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.HarvestPlant )
                                  .Except( _designations )
                                  .Where( des => IsValidForagingTarget( des.target ) ) )
                AddDesignation( des );
        }

        /// <summary>
        ///     Remove designations in our managed list that are not in the game's designation manager.
        /// </summary>
        public void CleanDeadDesignations()
        {
            var _gameDesignations =
                manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.HarvestPlant );
            _designations = _designations.Intersect( _gameDesignations ).ToList();
        }

        /// <summary>
        ///     Clean up all outstanding designations
        /// </summary>
        public override void CleanUp()
        {
            CleanDeadDesignations();
            foreach ( var des in _designations ) des.Delete();

            _designations.Clear();
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | (bar | last update)/(stamp) -> handled in Utilities.DrawStatusForListEntry

            // set up rects
            var labelRect = new Rect(
                Margin,
                Margin,
                rect.width  - ( active ? StatusRectWidth + 4 * Margin : 2 * Margin ),
                rect.height - 2 * Margin );
            var statusRect = new Rect( labelRect.xMax + Margin, Margin, StatusRectWidth, rect.height - 2 * Margin );

            // create label string
            var text    = Label + "\n";
            var subtext = string.Join( ", ", Targets );
            if ( subtext.Fits( labelRect ) )
                text += subtext.Italic();
            else
                text += "multiple".Translate().Italic();

            // do the drawing
            GUI.BeginGroup( rect );

            // draw label
            Widgets_Labels.Label( labelRect, text, subtext, TextAnchor.MiddleLeft, margin: Margin );

            // if the bill has a manager job, give some more info.
            if ( active ) this.DrawStatusForListEntry( statusRect, Trigger );
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
            Scribe_References.Look( ref ForagingArea, "ForagingArea" );
            Scribe_Deep.Look( ref Trigger, "trigger", manager );
            Scribe_Collections.Look( ref AllowedPlants, "AllowedPlants", LookMode.Def, LookMode.Value );
            Scribe_Values.Look( ref ForceFullyMature, "ForceFullyMature" );

            if ( Manager.LoadSaveMode == Manager.Modes.Normal )
                // scribe history
                Scribe_Deep.Look( ref History, "History" );
        }

        public override void Tick()
        {
            History.Update( Trigger.CurrentCount, CurrentDesignatedCount );
        }

        public override bool TryDoJob()
        {
            // keep track of work done
            var workDone = false;

            // clean up designations that were completed.
            CleanDeadDesignations();

            // clean up designations that are (now) in the wrong area.
            CleanAreaDesignations();

            // add designations in the game that could have been handled by this job
            AddRelevantGameDesignations();

            // designate plants until trigger is met.
            var count = Trigger.CurrentCount + CurrentDesignatedCount;
            if ( count < Trigger.TargetCount )
            {
                var targets = GetValidForagingTargetsSorted();

                for ( var i = 0; i < targets.Count && count < Trigger.TargetCount; i++ )
                {
                    var des = new Designation( targets[i], DesignationDefOf.HarvestPlant );
                    count += targets[i].YieldNow();
                    AddDesignation( des );
                    workDone = true;
                }
            }

            return workDone;
        }

        private void AddDesignation( Designation des )
        {
            // add to game
            manager.map.designationManager.AddDesignation( des );

            // add to internal list
            _designations.Add( des );
        }

        private void CleanAreaDesignations()
        {
            foreach ( var des in _designations )
                if ( !des.target.HasThing )
                    des.Delete();

                // if area is not null and does not contain designate location, remove designation.
                else if ( !ForagingArea?.ActiveCells.Contains( des.target.Thing.Position ) ?? false ) des.Delete();
        }

        private List<Plant> GetValidForagingTargetsSorted()
        {
            var position = manager.map.GetBaseCenter();

            return manager.map.listerThings.AllThings
                          .Where( IsValidForagingTarget )

                           // OrderBy defaults to ascending, switch sign on current yield to get descending
                          .Select( p => p as Plant )
                          .OrderBy( p => -p.YieldNow() / Distance( p, position ) )
                          .ToList();
        }

        private bool IsValidForagingTarget( LocalTargetInfo t )
        {
            return t.HasThing
                && IsValidForagingTarget( t.Thing );
        }

        private bool IsValidForagingTarget( Thing t )
        {
            var plant = t as Plant;
            return plant != null && IsValidForagingTarget( plant );
        }

        private bool IsValidForagingTarget( Plant target )
        {
            // should be a plant, and be on the same map as this job
            return target.def.plant != null
                && target.Map       == manager.map

                   // non-biome plants won't be on the list, also filters non-yield or wood plants
                && AllowedPlants.ContainsKey( target.def )
                && AllowedPlants[target.def]
                && target.Spawned
                && manager.map.designationManager.DesignationOn( target ) == null

                   // cut only mature plants, or non-mature that yield something right now.
                && ( !ForceFullyMature && target.YieldNow() > 1
                  || target.LifeStage == PlantLifeStage.Mature )

                   // limit to area of interest
                && ( ForagingArea == null
                  || ForagingArea.ActiveCells.Contains( target.Position ) )

                   // reachable
                && IsReachable( target );
        }

        public void RefreshAllowedPlants( bool firstTime = false )
        {
            Logger.Debug( "Refreshing allowed plants" );

            // all plants that yield something, and it isn't wood.
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
                                                 .Select( p => p.def )
                                                 .Distinct() )

                                  // that yield something that is not wood
                                 .Where( plant => plant.plant.harvestYield      > 0     &&
                                                  plant.plant.harvestedThingDef != null &&
                                                  plant.plant.harvestTag        != "Wood" )
                                 .Distinct();

            foreach ( var plant in options )
                if ( !AllowedPlants.ContainsKey( plant ) )
                    AllowedPlants.Add( plant, false );

            if ( firstTime )
            {
                Trigger.ThresholdFilter.SetDisallowAll();
                foreach ( var plant in AllowedPlants.Keys )
                    Trigger.ThresholdFilter.SetAllow( plant.plant.harvestedThingDef, true );
            }
        }
    }
}