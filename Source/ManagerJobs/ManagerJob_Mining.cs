// ManagerJob_Mining.cs
// Copyright Karel Kroeze, 2017-2017

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class ManagerJob_Mining : ManagerJob
    {
        public enum SyncDirection
        {
            FilterToAllowed,
            AllowedToFilter
        }

        private const    int                        RoofSupportGridSpacing = 5;
        private readonly Utilities.CachedValue<int> _chunksCachedValue     = new Utilities.CachedValue<int>();
        private readonly Utilities.CachedValue<int> _designatedCachedValue = new Utilities.CachedValue<int>();
        private          List<Designation>          _designations          = new List<Designation>();
        public           Dictionary<ThingDef, bool> AllowedBuildings       = new Dictionary<ThingDef, bool>();

        public Dictionary<ThingDef, bool> AllowedMinerals  = new Dictionary<ThingDef, bool>();
        public bool                       CheckRoofSupport = true;
        public bool                       CheckRoofSupportAdvanced;
        public bool                       CheckRoomDivision = true;
        public bool                       DeconstructBuildings;
        public History                    History;
        public Area                       MiningArea;
        public SyncDirection              Sync = SyncDirection.AllowedToFilter;

        public     bool              SyncFilterAndAllowed = true;
        public new Trigger_Threshold Trigger;


        public override bool IsValid => base.IsValid && History != null && Trigger != null;

        public ManagerJob_Mining( Manager manager ) : base( manager )
        {
            // populate the trigger field, set the root category to meats and allow all but human & insect meat.
            Trigger = new Trigger_Threshold( this );

            // start the history tracker;
            History = new History( new[] {"stock", "chunks", "designated"},
                                   new[] {Color.white, new Color( .7f, .7f, .7f ), new Color( .4f, .4f, .4f )} );

            // init stuff if we're not loading
            if ( Scribe.mode == LoadSaveMode.Inactive )
                RefreshAllowedMinerals();
        }

        public List<Designation> Designations => new List<Designation>( _designations );

        public override bool       Completed => !Trigger.State;
        public override string     Label     => "FM.Mining".Translate();
        public override ManagerTab Tab       => manager.Tabs.Find( tab => tab is ManagerTab_Mining );

        public override string[] Targets => AllowedMinerals.Keys
                                                           .Where( key => AllowedMinerals[key] )
                                                           .Select( pk => pk.LabelCap ).ToArray();

        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Mining;

        public override void CleanUp()
        {
            RemoveObsoleteDesignations();
            foreach ( var designation in _designations )
                designation.Delete();

            _designations.Clear();
        }

        private void RemoveObsoleteDesignations()
        {
            // get the intersection of bills in the game and bills in our list.
            var designations = manager.map.designationManager.allDesignations.Where( d =>
                                                                                         ( d.def == DesignationDefOf
                                                                                              .Mine || d.def ==
                                                                                           DesignationDefOf.Deconstruct
                                                                                         ) &&
                                                                                         ( !d.target.HasThing ||
                                                                                           d.target.Thing.Map ==
                                                                                           manager
                                                                                              .map
                                                                                         ) ); // equates to SpawnedDesignationsOfDef, with two defs.
            _designations = _designations.Intersect( designations ).ToList();
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
            base.ExposeData();

            Scribe_References.Look( ref MiningArea, "MiningArea" );
            Scribe_Deep.Look( ref Trigger, "Trigger", manager );
            Scribe_Collections.Look( ref AllowedMinerals, "AllowedMinerals", LookMode.Def, LookMode.Value );
            Scribe_Collections.Look( ref AllowedBuildings, "AllowedBuildings", LookMode.Def, LookMode.Value );
            Scribe_Values.Look( ref SyncFilterAndAllowed, "SyncFilterAndAllowed", true );
            Scribe_Values.Look( ref DeconstructBuildings, "DeconstructBuildings" );
            Scribe_Values.Look( ref CheckRoofSupport, "CheckRoofSupport", true );
            Scribe_Values.Look( ref CheckRoofSupportAdvanced, "CheckRoofSupportAdvanced" );
            Scribe_Values.Look( ref CheckRoomDivision, "CheckRoomDivision", true );

            // don't store history in import/export mode.
            if ( Manager.LoadSaveMode == Manager.Modes.Normal ) Scribe_Deep.Look( ref History, "History" );
        }

        public override void Tick()
        {
            History.Update( Trigger.CurrentCount, GetCountInChunks(), GetCountInDesignations() );
        }

        public override bool TryDoJob()
        {
            var workDone = false;

            RemoveObsoleteDesignations();
            AddRelevantGameDesignations();

            var count = Trigger.CurrentCount + GetCountInChunks() + GetCountInDesignations();

            if ( DeconstructBuildings )
            {
                var buildings = GetDeconstructibleBuildingsSorted();
                for ( var i = 0; i < buildings.Count && count < Trigger.TargetCount; i++ )
                {
                    AddDesignation( buildings[i], DesignationDefOf.Deconstruct );
                    count += GetCountInBuilding( buildings[i] );
                }
            }

            var minerals = GetMinableMineralsSorted();
            for ( var i = 0; i < minerals.Count && count < Trigger.TargetCount; i++ )
                if ( !IsARoofSupport_Advanced( minerals[i] ) )
                {
                    AddDesignation( minerals[i], DesignationDefOf.Mine );
                    count += GetCountInMineral( minerals[i] );
                }

            return workDone;
        }

        public int GetCountInChunks()
        {
            int count;
            if ( _chunksCachedValue.TryGetValue( out count ) )
                return count;

            count = manager.map.listerThings.AllThings
                           .Where( t => t.Faction == Faction.OfPlayer
                                     && !t.IsForbidden( Faction.OfPlayer )
                                     && IsChunk( t.def ) )
                           .Sum( GetCountInChunk );

            _chunksCachedValue.Update( count );
            return count;
        }

        public int GetCountInDesignations()
        {
            var count = 0;
            if ( _designatedCachedValue.TryGetValue( out count ) )
                return count;

            // deconstruction jobs
            count += _designations.Where( d => d.def == DesignationDefOf.Deconstruct )
                                  .Sum( d => GetCountInBuilding( d.target.Thing as Building ) );

            // mining jobs
            var mineralCounts = _designations.Where( d => d.def == DesignationDefOf.Mine )
                                             .Select( d => manager
                                                          .map.thingGrid.ThingsListAtFast( d.target.Cell )
                                                          .FirstOrDefault()?.def )
                                             .Where( d => d != null )
                                             .GroupBy( d => d, d => d, ( d, g ) => new {def = d, count = g.Count()} )
                                             .Where( g => Allowed( g.def ) );

            foreach ( var mineralCount in mineralCounts )
                count += GetCountInMineral( mineralCount.def ) * mineralCount.count;

            _designatedCachedValue.Update( count );
            return count;
        }

        public int GetCountInBuilding( Building building )
        {
            var def = building?.def;
            if ( def == null )
                return 0;

            var count = def.CostListAdjusted( building.Stuff )
                           .Where( Counted )
                           .Sum( tc => tc.count * def.resourcesFractionWhenDeconstructed );
            return Mathf.RoundToInt( count );
        }

        public int GetCountInMineral( Mineable rock )
        {
            return GetCountInMineral( rock.def );
        }

        public int GetCountInMineral( ThingDef rock )
        {
            var resource = rock.building?.mineableThing;
            if ( resource == null )
                return 0;

            // stone chunks
            if ( IsChunk( resource ) )
                return (int) ( GetCountInChunk( resource ) * rock.building.mineableDropChance );

            // metals
            if ( Counted( resource ) )
                return (int) ( rock.building.mineableYield * Find.Storyteller.difficulty.mineYieldFactor *
                               rock.building.mineableDropChance );

            return 0;
        }

        public bool IsChunk( ThingDef def )
        {
            return def?.thingCategories?.Any( c => ThingCategoryDefOf.Chunks.ThisAndChildCategoryDefs.Contains( c ) ) ??
                   false;
        }

        public bool Allowed( ThingDef thingDef )
        {
            if ( thingDef == null )
                return false;
            return AllowedMineral( thingDef ) || AllowedBuilding( thingDef );
        }

        public void SetAllowMineral( ThingDef mineral, bool allow, bool sync = true )
        {
            if ( mineral == null )
                throw new ArgumentNullException( nameof( mineral ) );
            AllowedMinerals[mineral] = allow;

            if ( SyncFilterAndAllowed && sync )
            {
                Sync = SyncDirection.AllowedToFilter;
                foreach ( var material in GetMaterialsInMineral( mineral ) )
                    if ( Trigger.ParentFilter.Allows( material ) )
                        Trigger.ThresholdFilter.SetAllow( material, allow );
            }
        }

        public List<ThingDef> GetMaterialsInMineral( ThingDef mineral )
        {
            var resource = mineral.building?.mineableThing;
            if ( resource == null )
                return new List<ThingDef>();

            // stone chunks
            if ( IsChunk( resource ) )
                return GetMaterialsInChunk( resource );

            // metals
            var list = new List<ThingDef>();
            list.Add( resource );
            return list;
        }

        public List<ThingDef> GetMaterialsInChunk( ThingDef chunk )
        {
            var materials = new List<ThingDef>();
            materials.Add( chunk );

            if ( !chunk.butcherProducts.NullOrEmpty() )
                materials.AddRange( chunk.butcherProducts.Select( tc => tc.thingDef ) );

            return materials;
        }

        public void SetAllowBuilding( ThingDef building, bool allow, bool sync = true )
        {
            if ( building == null )
                throw new ArgumentNullException( nameof( building ) );
            AllowedBuildings[building] = allow;

            if ( SyncFilterAndAllowed && sync )
            {
                Sync = SyncDirection.AllowedToFilter;
                foreach ( var material in GetMaterialsInBuilding( building ) )
                    if ( Trigger.ParentFilter.Allows( material ) )
                        Trigger.ThresholdFilter.SetAllow( material, allow );
            }
        }

        public List<ThingDef> GetMaterialsInBuilding( ThingDef building )
        {
            if ( building == null )
                return new List<ThingDef>();

            var baseCosts = building.costList.NullOrEmpty()
                ? new List<ThingDef>()
                : building.costList.Select( tc => tc.thingDef );
            var possibleStuffs = DefDatabase<ThingDef>.AllDefsListForReading
                                                      .Where( td => td.IsStuff
                                                                 && !td.stuffProps.categories.NullOrEmpty()
                                                                 && !building.stuffCategories.NullOrEmpty()
                                                                 && td.stuffProps.categories
                                                                      .Intersect( building.stuffCategories ).Any() );

            return baseCosts.Concat( possibleStuffs ).ToList();
        }

        public bool AllowedMineral( ThingDef thingDef )
        {
            if ( thingDef == null )
                return false;
            return AllowedMinerals.ContainsKey( thingDef ) && AllowedMinerals[thingDef];
        }

        public bool AllowedBuilding( ThingDef thingDef )
        {
            if ( thingDef == null )
                return false;
            return AllowedBuildings.ContainsKey( thingDef ) && AllowedBuildings[thingDef];
        }

        public bool Counted( ThingDefCountClass thingDefCount )
        {
            return Counted( thingDefCount.thingDef );
        }

        public bool Counted( ThingDef thingDef )
        {
            return Trigger.ThresholdFilter.Allows( thingDef );
        }

        public int GetCountInChunk( Thing chunk )
        {
            return GetCountInChunk( chunk.def );
        }

        public int GetCountInChunk( ThingDef chunk )
        {
            if ( chunk.butcherProducts.NullOrEmpty() )
                return 0;

            return chunk.butcherProducts
                        .Where( Counted )
                        .Sum( tc => tc.count );
        }

        public List<Building> GetDeconstructibleBuildingsSorted()
        {
            var position = manager.map.GetBaseCenter();

            return manager.map.listerThings.ThingsInGroup( ThingRequestGroup.BuildingArtificial ).OfType<Building>()
                          .Where( IsValidDeconstructionTarget )
                          .OrderBy( b => -GetCountInBuilding( b ) / Distance( b, position ) )
                          .ToList();
        }

        public List<Mineable> GetMinableMineralsSorted()
        {
            var position = manager.map.GetBaseCenter();

            return manager.map.listerThings.AllThings.OfType<Mineable>()
                          .Where( IsValidMiningTarget )
                          .OrderBy( r => -GetCountInMineral( r ) / Distance( r, position ) )
                          .ToList();
        }

        public void AddRelevantGameDesignations()
        {
            foreach ( var des in manager.map.designationManager
                                        .SpawnedDesignationsOfDef( DesignationDefOf.Mine )
                                        .Except( _designations )
                                        .Where( des => IsValidMiningTarget( des.target ) ) )
                AddDesignation( des );
            foreach ( var des in manager.map.designationManager
                                        .SpawnedDesignationsOfDef( DesignationDefOf.Deconstruct )
                                        .Except( _designations )
                                        .Where( des => IsValidDeconstructionTarget( des.target ) ) )
                AddDesignation( des );
        }

        public bool IsValidMiningTarget( LocalTargetInfo target )
        {
            return target.HasThing
                && target.IsValid
                && IsValidMiningTarget( target.Thing as Mineable );
        }

        public bool IsValidMiningTarget( Mineable target )
        {
            // mineable
            return target != null
                && target.def.mineable

                   // allowed
                && AllowedMineral( target.def )

                   // discovered 
                   // NOTE: also in IsReachable, but we expect a lot of fogged tiles, so move this check up a bit.
                && !target.Position.Fogged( manager.map )

                   // not yet designated
                && manager.map.designationManager.DesignationOn( target ) == null

                   // matches settings
                && IsInAllowedArea( target )
                && IsRelevantMiningTarget( target )
                && !IsARoomDivider( target )
                 &&
                   !IsARoofSupport_Basic(
                       target ) // note, returns true if advanced checking is enabled - checks will then be done before designating

                   // can be reached
                && IsReachable( target );
        }

        public bool IsRelevantDeconstructionTarget( Building target )
        {
            return target.def.building.IsDeconstructible
                && target.def.resourcesFractionWhenDeconstructed > 0
                && target.def.CostListAdjusted( target.Stuff )
                         .Any( tc => Trigger.ThresholdFilter.Allows( tc.thingDef ) );
        }

        public bool IsRelevantMiningTarget( Mineable target )
        {
            return GetCountInMineral( target ) > 0;
        }

        public bool IsValidDeconstructionTarget( Building target )
        {
            return target != null
                && target.Spawned

                   // not ours
                && target.Faction != Faction.OfPlayer

                   // not already designated
                && manager.map.designationManager.DesignationOn( target ) == null

                   // allowed
                && !target.IsForbidden( Faction.OfPlayer )
                && AllowedBuilding( target.def )

                   // drops things we want
                && IsRelevantDeconstructionTarget( target )

                   // in allowed area & reachable
                && IsInAllowedArea( target )
                && IsReachable( target )

                   // doesn't create safety hazards
                && !IsARoofSupport_Basic( target )
                && !IsARoomDivider( target );
        }


        public bool IsInAllowedArea( Thing target )
        {
            return MiningArea == null || MiningArea.ActiveCells.Contains( target.Position );
        }

        public bool IsARoofSupport_Basic( Building building )
        {
            if ( !CheckRoofSupport || CheckRoofSupportAdvanced )
                return false;

            // simply check location, leaving a grid of pillars
            return IsARoofSupport_Basic( building.Position );
        }

        public bool IsARoofSupport_Basic( IntVec3 cell )
        {
            return cell.x % RoofSupportGridSpacing == 0 && cell.z % RoofSupportGridSpacing == 0;
        }

        public bool IsARoofSupport_Advanced( Building building )
        {
            if ( !CheckRoofSupport || !CheckRoofSupportAdvanced )
                return false;

            // check if any cell in roofing range would collapse if this cell were to be removed
            for ( var i = RoofCollapseUtility.RoofSupportRadialCellsCount - 1; i >= 0; i-- )
                if ( WouldCollapseIfSupportDestroyed( GenRadial.RadialPattern[i] + building.Position, building.Position,
                                                      manager.map ) )
                    return true;
            return false;
        }

        // largely copypasta from RoofCollapseUtility.WithinRangeOfRoofHolder
        // TODO: PERFORMANCE; maintain a cellgrid of 'safe' supported areas.
        private static bool WouldCollapseIfSupportDestroyed( IntVec3 position, IntVec3 support, Map map )
        {
            if ( !position.InBounds( map ) || !position.Roofed( map ) )
                return false;

            // cell indexes and buildings on map indexed by cellIndex
            var cellIndices = map.cellIndices;
            var innerArray  = map.edificeGrid.InnerArray;

            for ( var i = 0; i < RoofCollapseUtility.RoofSupportRadialCellsCount; i++ )
            {
                Logger.Debug( i.ToString() );
                var candidate = position + GenRadial.RadialPattern[i];
                if ( candidate != support && candidate.InBounds( map ) )
                {
                    var building = innerArray[cellIndices.CellToIndex( candidate )];
#if DEBUG
                    map.debugDrawer.FlashCell(
                        candidate, DebugSolidColorMats.MaterialOf( new Color( 0f, 0f, 1f, .1f ) ), ".", 500 );
#endif
                    if ( building != null && building.def.holdsRoof && !IsDesignatedForRemoval( building, map ) )
                    {
#if DEBUG
                        map.debugDrawer.FlashCell(
                            candidate, DebugSolidColorMats.MaterialOf( new Color( 0f, 1f, 0f, .1f ) ), "!", 500 );
                        map.debugDrawer.FlashCell(
                            position, DebugSolidColorMats.MaterialOf( new Color( 0f, 1f, 0f, .1f ) ), "V", 500 );
#endif
                        return false;
                    }
                }
            }
#if DEBUG
            map.debugDrawer.FlashCell( position, DebugSolidColorMats.MaterialOf( Color.red ), "X" );
#endif
            return true;
        }

        public static bool IsDesignatedForRemoval( Building building, Map map )
        {
            var designation = map.designationManager.DesignationOn( building );

            return designation != null && ( designation.def == DesignationDefOf.Mine ||
                                            designation.def == DesignationDefOf.Deconstruct );
        }

        public bool IsARoomDivider( Thing target )
        {
            if ( !CheckRoomDivision )
                return false;

            var adjacent = GenAdjFast.AdjacentCells8Way( target.Position )
                                     .Where( c => c.InBounds( manager.map )
                                               && !c.Fogged( manager.map )
                                               && !c.Impassable( manager.map ) )
                                     .ToArray();

            // check if there are more than two rooms in the surrounding cells.
            var rooms = adjacent.Select( c => c.GetRoom( manager.map, RegionType.Normal ) )
                                .Where( r => r != null )
                                .Distinct()
                                .ToList();

            if ( rooms.Count() >= 2 ) return true;

            // check if any adjacent region is more than x regions from any other region
            for ( var i = 0; i < adjacent.Count(); i++ )
            {
                for ( var j = i + 1; j < adjacent.Count(); j++ )
                {
                    var path = manager.map.pathFinder.FindPath( adjacent[i], adjacent[j],
                                                                TraverseParms.For(
                                                                    TraverseMode.NoPassClosedDoors, Danger.Some ) );
                    var cost = path.TotalCost;
                    path.ReleaseToPool();

                    Logger.Debug( $"from {adjacent[i]} to {adjacent[j]}: {cost}" );
                    if ( cost > MaxPathCost )
                        return true;
                }
            }

            return false;
        }

        private bool RegionsAreClose( Region start, Region end, int depth = 0 )
        {
            if ( depth > MaxRegionDistance )
                return false;

            var neighbours = start.Neighbors;
            if ( neighbours.Contains( end ) )
                return true;

            return neighbours.Any( n => RegionsAreClose( n, end, depth + 1 ) );
        }

        public bool IsValidDeconstructionTarget( LocalTargetInfo target )
        {
            return target.HasThing
                && target.IsValid
                && target.Thing is Building building
                && IsValidDeconstructionTarget( building );
        }

        public string DesignationLabel( Designation designation )
        {
            if ( designation.def == DesignationDefOf.Deconstruct )
            {
                var building = designation.target.Thing;
                return "Fluffy.Manager.DesignationLabel".Translate(
                    building.LabelCap,
                    Distance( building, manager.map.GetBaseCenter() ).ToString( "F0" ),
                    "?", "?" );
            }

            if ( designation.def == DesignationDefOf.Mine )
            {
                var mineable = designation.target.Cell.GetFirstMineable( manager.map );
                return "Fluffy.Manager.DesignationLabel".Translate(
                    mineable.LabelCap,
                    Distance( mineable, manager.map.GetBaseCenter() ).ToString( "F0" ),
                    GetCountInMineral( mineable ),
                    GetMaterialsInMineral( mineable.def )?.First().LabelCap ?? "?" );
            }

            return string.Empty;
        }

        public void AddDesignation( Designation designation )
        {
            manager.map.designationManager.AddDesignation( designation );
            _designations.Add( designation );
        }


        private void AddDesignation( Thing target, DesignationDef designationDef )
        {
            if ( designationDef == DesignationDefOf.Deconstruct )
            {
                var building = target as Building;
                if ( building?.ClaimableBy( Faction.OfPlayer ) ?? false ) building.SetFaction( Faction.OfPlayer );
            }

            AddDesignation( new Designation( target, designationDef ) );
        }

        public void RefreshAllowedMinerals()
        {
            var deconstructibleDefs = manager.map.listerThings.AllThings.OfType<Building>()
                                             .Where( b => b.Faction != Faction.OfPlayer
                                                       && !b.Position.Fogged( manager.map )
                                                       && b.def.building.IsDeconstructible
                                                       && !b.CostListAdjusted().NullOrEmpty()
                                                       && b.def.resourcesFractionWhenDeconstructed > 0 )
                                             .Select( b => b.def )
                                             .Distinct()
                                             .OrderBy( b => b.LabelCap )
                                             .ToDictionary( d => d, AllowedBuilding );

            AllowedBuildings = deconstructibleDefs;

            var mineralDefs = DefDatabase<ThingDef>.AllDefsListForReading
                                                   .Where( d => d.building != null
                                                             && d.building.isNaturalRock )
                                                   .OrderBy( d => d.LabelCap )
                                                   .ToDictionary( d => d, AllowedMineral );

            AllowedMinerals = mineralDefs;
        }

        public void Notify_ThresholdFilterChanged()
        {
            Logger.Debug( "Threshold changed." );
            if ( !SyncFilterAndAllowed || Sync == SyncDirection.AllowedToFilter )
                return;

            foreach ( var building in new List<ThingDef>( AllowedBuildings.Keys ) )
                AllowedBuildings[building] = GetMaterialsInBuilding( building )
                   .Any( m => Trigger.ThresholdFilter.Allows( m ) );
            foreach ( var mineral in new List<ThingDef>( AllowedMinerals.Keys ) )
                AllowedMinerals[mineral] = GetMaterialsInMineral( mineral )
                   .Any( m => Trigger.ThresholdFilter.Allows( m ) );
        }
    }
}