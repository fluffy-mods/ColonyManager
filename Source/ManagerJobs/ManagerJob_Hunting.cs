// Karel Kroeze
// ManagerJob_Hunting.cs
// 2016-12-09

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class ManagerJob_Hunting : ManagerJob
    {
        #region Fields

        public Dictionary<PawnKindDef, bool> AllowedAnimals = new Dictionary<PawnKindDef, bool>();
        public List<Designation> Designations = new List<Designation>();
        public History History;
        public Area HuntingGrounds;
        public new Trigger_Threshold Trigger;
        public bool UnforbidCorpses = true;
        private readonly float _margin = Utilities.Margin;
        private Utilities.CachedValue<int> _corpseCachedValue = new Utilities.CachedValue<int>();
        private Utilities.CachedValue<int> _designatedCachedValue = new Utilities.CachedValue<int>();
        private List<ThingDef> _humanLikeMeatDefs;

        private bool _allowHumanLikeMeat;
        public bool AllowHumanLikeMeat
        {
            get { return _allowHumanLikeMeat; }
            set
            {
                // no change
                if ( value == _allowHumanLikeMeat )
                    return;

                // update value and filter
                _allowHumanLikeMeat = value;
                foreach ( ThingDef def in HumanLikeMeatDefs )
                    Trigger.ThresholdFilter.SetAllow( def, value );
            }
        }

        private bool _allowInsectMeat;
        public bool AllowInsectMeat
        {
            get { return _allowInsectMeat; }
            set
            {
                // no change
                if (value == _allowInsectMeat)
                    return;

                // update value and filter
                _allowInsectMeat = value;
                Trigger.ThresholdFilter.SetAllow( Utilities_Hunting.InsectMeat, value );
            }
        }

        #endregion Fields

        #region Constructors

        public ManagerJob_Hunting( Manager manager ) : base( manager )
        {
            // populate the trigger field, set the root category to meats and allow all but human & insect meat.
            Trigger = new Trigger_Threshold( this );
            Trigger.ThresholdFilter.SetDisallowAll();
            Trigger.ThresholdFilter.SetAllow( Utilities_Hunting.MeatRaw, true );

            // disallow humanlike
            foreach ( ThingDef def in HumanLikeMeatDefs )
                Trigger.ThresholdFilter.SetAllow( def, false );

            // disallow insect
            Trigger.ThresholdFilter.SetAllow( Utilities_Hunting.InsectMeat, false );
            
            // start the history tracker;
            History = new History( new[] { "stock", "corpses", "designated" },
                                   new[] { Color.white, new Color( .7f, .7f, .7f ), new Color( .4f, .4f, .4f ) } );
            
            // init stuff if we're not loading
            if (Scribe.mode == LoadSaveMode.Inactive)
                UpdateAllowedAnimals();
        }

        #endregion Constructors

        #region Properties

        public override bool Completed
        {
            get { return !Trigger.State; }
        }

        public List<Corpse> Corpses
        {
            get
            {
                List<Corpse> corpses =
                    manager.map.listerThings.ThingsInGroup( ThingRequestGroup.Corpse )
                           .ConvertAll( thing => thing as Corpse );
                return
                    corpses.Where(
                                  thing => thing?.InnerPawn != null &&
                                           AllowedAnimals.ContainsKey( thing.InnerPawn.kindDef ) &&
                                           AllowedAnimals[thing.InnerPawn.kindDef] ).ToList();
            }
        }

        public List<ThingDef> HumanLikeMeatDefs
        {
            get
            {
                if ( _humanLikeMeatDefs == null )
                {
                    _humanLikeMeatDefs =
                        DefDatabase<ThingDef>.AllDefsListForReading
                                             .Where( def => def.category == ThingCategory.Pawn &&
                                                            def.race.Humanlike &&
                                                            def.race.IsFlesh )
                                             .Select( pk => pk.race.meatDef )
                                             .Distinct()
                                             .ToList();
                }

                return _humanLikeMeatDefs;
            }
        }

        public override string Label
        {
            get { return "FMH.Hunting".Translate(); }
        }

        public override ManagerTab Tab
        {
            get { return Manager.For( manager ).ManagerTabs.Find( tab => tab is ManagerTab_Hunting ); }
        }

        public override string[] Targets
        {
            get
            {
                return AllowedAnimals.Keys.Where( key => AllowedAnimals[key] ).Select( pk => pk.LabelCap ).ToArray();
            }
        }

        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Hunting;

        #endregion Properties



        #region Methods
        /// <summary>
        /// Remove obsolete designations from the list.
        /// </summary>
        public void CleanDesignations()
        {
            // get the intersection of bills in the game and bills in our list.
            List<Designation> GameDesignations =
                manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.Hunt ).ToList();
            Designations = Designations.Intersect( GameDesignations ).ToList();
        }

        public override void CleanUp()
        {
            // clear the list of obsolete designations
            CleanDesignations();

            // cancel outstanding designation
            foreach ( Designation des in Designations )
                des.Delete();

            // clear the list completely
            Designations.Clear();
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
            string text = Label + "\n";
            string subtext = string.Join( ", ", Targets );
            if ( subtext.Fits( labelRect ) )
                text += subtext.Italic();
            else
                text += "multiple".Translate().Italic();

            // do the drawing
            GUI.BeginGroup( rect );

            // draw label
            Utilities.Label( labelRect, text, subtext, TextAnchor.MiddleLeft, _margin );

            // if the bill has a manager job, give some more info.
            if ( active )
            {
                this.DrawStatusForListEntry( statusRect, Trigger );
            }
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

            // references first, because of the stupid bug in CrossRefResolver.
            Scribe_References.Look( ref HuntingGrounds, "HuntingGrounds" );

            // must be after references, because reasons.
            Scribe_Deep.Look( ref Trigger, "trigger", manager );

            // settings
            Scribe_Collections.Look( ref AllowedAnimals, "AllowedAnimals", LookMode.Def, LookMode.Value );
            Scribe_Values.Look( ref UnforbidCorpses, "UnforbidCorpses", true );
            Scribe_Values.Look( ref _allowHumanLikeMeat, "AllowHumanLikeMeat", false );
            Scribe_Values.Look( ref _allowInsectMeat, "AllowInsectMeat", false );

            // don't store history in import/export mode.
            if ( Manager.LoadSaveMode == Manager.Modes.Normal )
            {
                Scribe_Deep.Look( ref History, "History" );
            }
        }

        public int GetMeatInCorpses()
        {
            // get current count + corpses in storage that is not a grave + designated count
            // current count in storage
            var count = 0;

            // try get cached value
            if ( _corpseCachedValue.TryGetValue( out count ) )
            {
                return count;
            }

            // corpses not buried / forbidden
            foreach ( Thing current in Corpses )
            {
                // make sure it's a real corpse. (I dunno, poke it?)
                // and that it's not forbidden (anymore) and can be reached.
                var corpse = current as Corpse;
                if ( corpse != null &&
                     !corpse.IsForbidden( Faction.OfPlayer ) &&
                     manager.map.reachability.CanReachColony( corpse.Position ) )
                {
                    // check to see if it's buried.
                    var buried = false;
                    SlotGroup slotGroup = manager.map.slotGroupManager.SlotGroupAt( corpse.Position );
                    var building_Storage = slotGroup?.parent as Building_Storage;

                    // Sarcophagus inherits grave
                    if ( building_Storage != null &&
                         building_Storage.def == ThingDefOf.Grave )
                    {
                        buried = true;
                    }

                    // get the rottable comp and check how far gone it is.
                    var rottable = corpse.TryGetComp<CompRottable>();

                    if ( !buried && rottable?.Stage == RotStage.Fresh )
                    {
                        count += corpse.EstimatedMeatCount();
                    }
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
            if ( _designatedCachedValue.TryGetValue( out count ) )
            {
                return count;
            }

            // designated animals
            foreach ( Designation des in manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.Hunt ) )
            {
                // make sure target is a pawn, is an animal, is not forbidden and somebody can reach it.
                // note: could be rolled into a fancy LINQ chain, but this is probably clearer.
                var target = des.target.Thing as Pawn;
                if ( target != null &&
                     target.RaceProps.Animal &&
                     !target.IsForbidden( Faction.OfPlayer ) &&
                     manager.map.reachability.CanReachColony( target.Position ) )
                {
                    count += target.EstimatedMeatCount();
                }
            }

            // update cache
            _designatedCachedValue.Update( count );

            return count;
        }

        public override void Tick()
        {
            History.Update( Trigger.CurCount, GetMeatInCorpses(), GetMeatInDesignations() );
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
            int totalCount = Trigger.CurCount + GetMeatInCorpses() + GetMeatInDesignations();

            // get a list of huntable animals sorted by distance (ignoring obstacles) and expected meat count.
            // note; attempt to balance cost and benefit, current formula: value = meat / ( distance ^ 2)
            List<Pawn> huntableAnimals = GetHuntableAnimalsSorted();

            // while totalCount < count AND we have animals that can be designated, designate animal.
            for ( var i = 0; i < huntableAnimals.Count && totalCount < Trigger.Count; i++ )
            {
                AddDesignation( huntableAnimals[i] );
                totalCount += huntableAnimals[i].EstimatedMeatCount();
                workDone = true;
            }

            // unforbid if required
            if ( UnforbidCorpses )
            {
                DoUnforbidCorpses( ref workDone );
            }

            return workDone;
        }

        private void AddDesignation( Designation des )
        {
            // add to game
            manager.map.designationManager.AddDesignation( des );

            // add to internal list
            Designations.Add( des );
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
                Designation des in
                    manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.Hunt )
                           .Except( Designations )
                           .Where( des => IsValidHuntingTarget( des.target ) ) )
            {
                AddDesignation( des );
            }
        }

        private void CleanAreaDesignations()
        {
            // huntinggrounds of null denotes unrestricted
            if ( HuntingGrounds != null )
            {
                foreach ( Designation des in Designations )
                {
                    if ( des.target.HasThing &&
                         !HuntingGrounds.ActiveCells.Contains( des.target.Thing.Position ) )
                    {
                        des.Delete();
                    }
                }
            }
        }

        // copypasta from autohuntbeacon by Carry
        // https://ludeon.com/forums/index.php?topic=8930.0
        private void DoUnforbidCorpses( ref bool workDone )
        {
            foreach ( Corpse corpse in Corpses )
            {
                // don't unforbid corpses in storage - we're going to assume they were manually set.
                if ( corpse != null &&
                     !corpse.IsInAnyStorage() &&
                     corpse.IsForbidden( Faction.OfPlayer ) )
                {
                    // only fresh corpses
                    var comp = corpse.GetComp<CompRottable>();
                    if ( comp != null &&
                         comp.Stage == RotStage.Fresh )
                    {
                        // unforbid
                        workDone = true;
                        corpse.SetForbidden( false, false );
                    }
                }
            }
        }

        private List<Pawn> GetHuntableAnimalsSorted()
        {
            // get the 'home' position
            IntVec3 position = manager.map.GetBaseCenter();

            // get a list of alive animals that are not designated in the hunting grounds and are reachable, sorted by meat / distance * 2
            List<Pawn> list = manager.map.mapPawns.AllPawns.Where( p => IsValidHuntingTarget( p ) )

                                     // OrderBy defaults to ascending, switch sign on estimated meat count to get descending
                                     .OrderBy(
                                              p =>
                                              -p.EstimatedMeatCount() /
                                              ( Math.Sqrt( position.DistanceToSquared( p.Position ) ) * 2 ) ).ToList();

            return list;
        }

        private bool IsValidHuntingTarget( LocalTargetInfo t )
        {
            return t.HasThing
                   && t.Thing is Pawn
                   && IsValidHuntingTarget( (Pawn)t.Thing );
        }

        private bool IsValidHuntingTarget( Pawn p )
        {
            return p.RaceProps.Animal
                   && !p.health.Dead
                   && p.Spawned

                   // wild animals only
                   && p.Faction == null

                   // non-biome animals won't be on the list
                   && AllowedAnimals.ContainsKey( p.kindDef )
                   && AllowedAnimals[p.kindDef]
                   && manager.map.designationManager.DesignationOn( p ) == null
                   && ( HuntingGrounds == null
                        || HuntingGrounds.ActiveCells.Contains( p.Position ) )
                   && manager.map.reachability.CanReachColony( p.Position );
        }

        #endregion Methods

        public void UpdateAllowedAnimals()
        {
            // add animals that were not already in the list, disallow by default.
            foreach ( PawnKindDef pawnKind in manager.map.Biome.AllWildAnimals
                                                     .Concat( manager.map.mapPawns.AllPawns
                                                                     .Where( p => ( p.RaceProps?.Animal ?? false )
                                                                              && !( manager.map.fogGrid?.IsFogged( p.Position ) ?? true ) )
                                                                     .Select( p => p.kindDef ) )
                                                     .Distinct()
                                                     .OrderBy( pk => pk.label ) )
            {
                if ( !AllowedAnimals.ContainsKey( pawnKind ) )
                {
                    AllowedAnimals.Add( pawnKind, false );
                }
            }

            AllowedAnimals = AllowedAnimals
                                .OrderBy( x => x.Key.label )
                                .ToDictionary( k => k.Key, v => v.Value );
        }
    }
}
