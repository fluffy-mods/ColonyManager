// Karel Kroeze
// ManagerJob_Hunting.cs
// 2016-12-09

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class ManagerJob_Hunting : ManagerJob
    {
        private bool _allowHumanLikeMeat;

        private          bool                       _allowInsectMeat;
        private readonly Utilities.CachedValue<int> _corpseCachedValue     = new Utilities.CachedValue<int>();
        private readonly Utilities.CachedValue<int> _designatedCachedValue = new Utilities.CachedValue<int>();
        private          List<Designation>          _designations          = new List<Designation>();
        private          List<ThingDef>             _humanLikeMeatDefs;

        public     Dictionary<PawnKindDef, bool> AllowedAnimals = new Dictionary<PawnKindDef, bool>();
        public     History                       History;
        public     Area                          HuntingGrounds;
        public new Trigger_Threshold             Trigger;
        public     bool                          UnforbidCorpses = true;

        public override bool IsValid => base.IsValid && History != null && Trigger != null;

        public ManagerJob_Hunting( Manager manager ) : base( manager )
        {
            // populate the trigger field, set the root category to meats and allow all but human & insect meat.
            Trigger = new Trigger_Threshold( this );
            Trigger.ThresholdFilter.SetDisallowAll();
            Trigger.ThresholdFilter.SetAllow( Utilities_Hunting.MeatRaw, true );

            // disallow humanlike
            foreach ( var def in HumanLikeMeatDefs )
                Trigger.ThresholdFilter.SetAllow( def, false );

            // disallow insect
            Trigger.ThresholdFilter.SetAllow( Utilities_Hunting.InsectMeat, false );

            // start the history tracker;
            History = new History( new[] {"stock", "corpses", "designated"},
                                   new[] {Color.white, new Color( .7f, .7f, .7f ), new Color( .4f, .4f, .4f )} );

            // init stuff if we're not loading
            if ( Scribe.mode == LoadSaveMode.Inactive )
                RefreshAllowedAnimals();
        }

        public List<Designation> Designations => new List<Designation>( _designations );

        public override bool Completed => !Trigger.State;

        public bool AllowHumanLikeMeat
        {
            get => _allowHumanLikeMeat;
            set
            {
                // no change
                if ( value == _allowHumanLikeMeat )
                    return;

                // update value and filter
                _allowHumanLikeMeat = value;
                foreach ( var def in HumanLikeMeatDefs )
                    Trigger.ThresholdFilter.SetAllow( def, value );
            }
        }

        public bool AllowInsectMeat
        {
            get => _allowInsectMeat;
            set
            {
                // no change
                if ( value == _allowInsectMeat )
                    return;

                // update value and filter
                _allowInsectMeat = value;
                Trigger.ThresholdFilter.SetAllow( Utilities_Hunting.InsectMeat, value );
            }
        }

        public List<Corpse> Corpses
        {
            get
            {
                var corpses =
                    manager.map.listerThings.ThingsInGroup( ThingRequestGroup.Corpse )
                           .ConvertAll( thing => thing as Corpse );
                return
                    corpses.Where(
                        thing => thing?.InnerPawn != null &&
                                 ( HuntingGrounds == null ||
                                   HuntingGrounds.ActiveCells.Contains( thing.Position ) ) &&
                                 AllowedAnimals.ContainsKey( thing.InnerPawn.kindDef )     &&
                                 AllowedAnimals[thing.InnerPawn.kindDef] ).ToList();
            }
        }

        public List<ThingDef> HumanLikeMeatDefs
        {
            get
            {
                if ( _humanLikeMeatDefs == null )
                    _humanLikeMeatDefs =
                        DefDatabase<ThingDef>.AllDefsListForReading
                                             .Where( def => def.category == ThingCategory.Pawn &&
                                                            ( def.race?.Humanlike ?? false )   &&
                                                            ( def.race?.IsFlesh   ?? false ) )
                                             .Select( pk => pk.race.meatDef )
                                             .Distinct()
                                             .ToList();

                return _humanLikeMeatDefs;
            }
        }

        public override string Label => "FMH.Hunting".Translate();

        public override ManagerTab Tab
        {
            get { return Manager.For( manager ).Tabs.Find( tab => tab is ManagerTab_Hunting ); }
        }

        public override string[] Targets
        {
            get
            {
                return AllowedAnimals.Keys.Where( key => AllowedAnimals[key] ).Select( pk => pk.LabelCap ).ToArray();
            }
        }

        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Hunting;

        public string DesignationLabel( Designation designation )
        {
            // label, dist, yield.
            var thing = designation.target.Thing;
            return "Fluffy.Manager.DesignationLabel".Translate(
                thing.LabelCap,
                Distance( thing, manager.map.GetBaseCenter() ).ToString( "F0" ),
                thing.GetStatValue( StatDefOf.MeatAmount ).ToString( "F0" ),
                thing.def.race.meatDef.LabelCap );
        }

        /// <summary>
        ///     Remove obsolete designations from the list.
        /// </summary>
        public void CleanDesignations()
        {
            // get the intersection of bills in the game and bills in our list.
            var GameDesignations =
                manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.Hunt ).ToList();
            _designations = _designations.Intersect( GameDesignations ).ToList();
        }

        public override void CleanUp()
        {
            // clear the list of obsolete designations
            CleanDesignations();

            // cancel outstanding designation
            foreach ( var des in _designations )
                des.Delete();

            // clear the list completely
            _designations.Clear();
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
            // scribe base things
            base.ExposeData();

            // references first, reasons
            Scribe_References.Look( ref HuntingGrounds, "HuntingGrounds" );

            // must be after references, because reasons.
            Scribe_Deep.Look( ref Trigger, "trigger", manager );

            // settings
            Scribe_Collections.Look( ref AllowedAnimals, "AllowedAnimals", LookMode.Def, LookMode.Value );
            Scribe_Values.Look( ref UnforbidCorpses, "UnforbidCorpses", true );
            Scribe_Values.Look( ref _allowHumanLikeMeat, "AllowHumanLikeMeat" );
            Scribe_Values.Look( ref _allowInsectMeat, "AllowInsectMeat" );

            // don't store history in import/export mode.
            if ( Manager.LoadSaveMode == Manager.Modes.Normal ) Scribe_Deep.Look( ref History, "History" );
        }

        public int GetMeatInCorpses()
        {
            // get current count + corpses in storage that is not a grave + designated count
            // current count in storage
            var count = 0;

            // try get cached value
            if ( _corpseCachedValue.TryGetValue( out count ) ) return count;

            // corpses not buried / forbidden
            foreach ( Thing current in Corpses )
            {
                // make sure it's a real corpse. (I dunno, poke it?)
                // and that it's not forbidden (anymore) and can be reached.
                var corpse = current as Corpse;
                if ( corpse != null                          &&
                     !corpse.IsForbidden( Faction.OfPlayer ) &&
                     manager.map.reachability.CanReachColony( corpse.Position ) )
                {
                    // check to see if it's buried.
                    var buried           = false;
                    var slotGroup        = manager.map.haulDestinationManager.SlotGroupAt( corpse.Position );
                    var building_Storage = slotGroup?.parent as Building_Storage;

                    // Sarcophagus inherits grave
                    if ( building_Storage     != null &&
                         building_Storage.def == ThingDefOf.Grave )
                        buried = true;

                    // get the rottable comp and check how far gone it is.
                    var rottable = corpse.TryGetComp<CompRottable>();

                    if ( !buried && rottable?.Stage == RotStage.Fresh ) count += corpse.EstimatedMeatCount();
                }
            }

            // set cache
            _corpseCachedValue.Update( count );

            return count;
        }

        public int GetMeatInDesignations()
        {
            var count = 0;

            // try get cache
            if ( _designatedCachedValue.TryGetValue( out count ) ) return count;

            // designated animals
            foreach ( var des in _designations )
            {
                var target                  = des.target.Thing as Pawn;
                if ( target != null ) count += target.EstimatedMeatCount();
            }

            // update cache
            _designatedCachedValue.Update( count );

            return count;
        }

        public override void Tick()
        {
            History.Update( Trigger.CurrentCount, GetMeatInCorpses(), GetMeatInDesignations() );
        }

        public override bool TryDoJob()
        {
            // did we do any work?
            var workDone = false;

            // clean designations not in area
            CleanAreaDesignations();

            // clean dead designations
            CleanDesignations();

            // add designations that could have been handed out by us
            AddRelevantGameDesignations();

            // get the total count of meat in storage, expected meat in corpses and expected meat in designations.
            var totalCount = Trigger.CurrentCount + GetMeatInCorpses() + GetMeatInDesignations();

            // get a list of huntable animals sorted by distance (ignoring obstacles) and expected meat count.
            // note; attempt to balance cost and benefit, current formula: value = meat / ( distance ^ 2)
            var huntableAnimals = GetHuntableAnimalsSorted();

            // while totalCount < count AND we have animals that can be designated, designate animal.
            for ( var i = 0; i < huntableAnimals.Count && totalCount < Trigger.TargetCount; i++ )
            {
                AddDesignation( huntableAnimals[i] );
                totalCount += huntableAnimals[i].EstimatedMeatCount();
                workDone   =  true;
            }

            // unforbid if required
            if ( UnforbidCorpses ) DoUnforbidCorpses( ref workDone );

            return workDone;
        }

        private void AddDesignation( Designation des, bool addToGame = true )
        {
            // add to game
            if ( addToGame )
                manager.map.designationManager.AddDesignation( des );

            // add to internal list
            _designations.Add( des );
        }

        private void AddDesignation( Pawn p )
        {
            // create designation
            var des = new Designation( p, DesignationDefOf.Hunt );

            // pass to adder
            AddDesignation( des );
        }

        private void AddRelevantGameDesignations()
        {
            foreach (
                var des in
                manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.Hunt )
                       .Except( _designations )
                       .Where( des => IsValidHuntingTarget( des.target, true ) ) )
                AddDesignation( des, false );
        }

        private void CleanAreaDesignations()
        {
            // huntinggrounds of null denotes unrestricted
            if ( HuntingGrounds != null )
                foreach ( var des in _designations )
                    if ( des.target.HasThing &&
                         !HuntingGrounds.ActiveCells.Contains( des.target.Thing.Position ) )
                        des.Delete();
        }

        // copypasta from autohuntbeacon by Carry
        // https://ludeon.com/forums/index.php?topic=8930.0
        private void DoUnforbidCorpses( ref bool workDone )
        {
            foreach ( var corpse in Corpses )
                // don't unforbid corpses in storage - we're going to assume they were manually set.
                if ( corpse != null           &&
                     !corpse.IsInAnyStorage() &&
                     corpse.IsForbidden( Faction.OfPlayer ) )
                {
                    // only fresh corpses
                    var comp = corpse.GetComp<CompRottable>();
                    if ( comp       != null &&
                         comp.Stage == RotStage.Fresh )
                    {
                        // unforbid
                        workDone = true;
                        corpse.SetForbidden( false, false );
                    }
                }
        }

        // TODO: refactor into a yielding iterator for performance?
        private List<Pawn> GetHuntableAnimalsSorted()
        {
            // get the 'home' position
            var position = manager.map.GetBaseCenter();

            return manager.map.mapPawns.AllPawns
                          .Where( p => IsValidHuntingTarget( p, false ) )
                          .OrderBy( p => -p.EstimatedMeatCount() / Distance( p, position ) )
                          .ToList();
        }

        private bool IsValidHuntingTarget( LocalTargetInfo t, bool allowHunted )
        {
            return t.HasThing
                && t.Thing is Pawn
                && IsValidHuntingTarget( (Pawn) t.Thing, allowHunted );
        }

        private bool IsValidHuntingTarget( Pawn target, bool allowHunted )
        {
            return target.RaceProps.Animal
                && !target.health.Dead
                && target.Spawned

                   // wild animals only
                && target.Faction == null

                   // non-biome animals won't be on the list
                && AllowedAnimals.ContainsKey( target.kindDef )
                && AllowedAnimals[target.kindDef]
                && ( allowHunted || manager.map.designationManager.DesignationOn( target ) == null )
                && ( HuntingGrounds == null ||
                     HuntingGrounds.ActiveCells.Contains( target.Position ) )
                && IsReachable( target );
        }

        public void RefreshAllowedAnimals()
        {
            // add animals that were not already in the list, disallow by default.
            foreach ( var pawnKind in manager.map.Biome.AllWildAnimals
                                             .Concat( manager.map.mapPawns.AllPawns
                                                             .Where( p => ( p.RaceProps?.Animal ?? false )
                                                                       && !( manager.map.fogGrid?.IsFogged(
                                                                                 p.Position ) ?? true ) )
                                                             .Select( p => p.kindDef ) )
                                             .Distinct()
                                             .OrderBy( pk => pk.label ) )
                if ( !AllowedAnimals.ContainsKey( pawnKind ) )
                    AllowedAnimals.Add( pawnKind, false );

            AllowedAnimals = AllowedAnimals
                            .OrderBy( x => x.Key.label )
                            .ToDictionary( k => k.Key, v => v.Value );
        }
    }
}