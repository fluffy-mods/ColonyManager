﻿// Manager/ManagerJob_Hunting.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-05 22:30

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FM
{
    public class ManagerJob_Hunting : ManagerJob
    {
        private static int _histSize = 100;

        private readonly float _margin = Utilities.Margin;
        private Texture2D _cogTex = ContentFinder< Texture2D >.Get( "UI/Buttons/Cog" );
        public Dictionary< PawnKindDef, bool > AllowedAnimals = new Dictionary< PawnKindDef, bool >();

        public History day = new History( _histSize );
        public List< Designation > Designations = new List< Designation >();
        public History historyShown;
        public Area HuntingGrounds = null;
        public History month = new History( _histSize, History.Period.Month );

        public new Trigger_Threshold Trigger;
        public bool UnforbidCorpses = true;
        public History year = new History( _histSize, History.Period.Year );

        public override ManagerTab Tab
        {
            get { return Manager.Get.ManagerTabs.Find( tab => tab is ManagerTab_Hunting ); }
        }
        
        public override string Label
        {
            get { return "FMH.Hunting".Translate(); }
        }

        public override string[] Targets
        {
            get
            {
                return AllowedAnimals.Keys.Where( key => AllowedAnimals[key] ).Select( pk => pk.LabelCap ).ToArray();
            }
        }

        public static List< Thing > Corpses
        {
            get { return Find.ListerThings.ThingsInGroup( ThingRequestGroup.Corpse ) ?? new List< Thing >(); }
        }

        public ManagerJob_Hunting()
        {
            // populate the trigger field, set the root category to meats and allow all but human meat.
            Trigger = new Trigger_Threshold( this );
            Trigger.ThresholdFilter.SetDisallowAll();
            Trigger.ThresholdFilter.SetAllow( Utilities_Hunting.RawMeat, true );
            Trigger.ThresholdFilter.SetAllow( Utilities_Hunting.HumanMeat, false );

            // populate the list of animals from the animals in the biome - allow all by default.
            AllowedAnimals = Find.Map.Biome.AllWildAnimals.ToDictionary( pk => pk, v => true );
        }

        public override void ExposeData()
        {
            // scribe base things
            base.ExposeData();

            // settings
            Scribe_References.LookReference( ref HuntingGrounds, "HuntingGrounds" );
            Scribe_Collections.LookDictionary( ref AllowedAnimals, "AllowedAnimals", LookMode.DefReference, LookMode.Value );
            // human meat is saved in trigger's thingfilter.
            Scribe_Values.LookValue( ref UnforbidCorpses, "UnforbidCorpses", true );

            // trigger
            Scribe_Deep.LookDeep( ref Trigger, "Trigger", this );

            if( Manager.LoadSaveMode == Manager.Modes.Normal )
            {
                // current designations
                Scribe_Collections.LookList( ref Designations, "Designations", LookMode.MapReference );

                // scribe history
                Scribe_Deep.LookDeep( ref day, "histDay", _histSize );
                Scribe_Deep.LookDeep( ref month, "histMonth", _histSize, History.Period.Month );
                Scribe_Deep.LookDeep( ref year, "histYear", _histSize, History.Period.Year );
            }
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

        /// <summary>
        /// Remove obsolete designations from the list.
        /// </summary>
        public void CleanDesignations()
        {
            // get the intersection of bills in the game and bills in our list.
            List< Designation > GameDesignations =
                Find.DesignationManager.DesignationsOfDef( DesignationDefOf.Hunt ).ToList();
            Designations = Designations.Intersect( GameDesignations ).ToList();
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | bar | last update

            Rect labelRect = new Rect( _margin, _margin,
                                       rect.width -
                                       ( active ? _lastUpdateRectWidth + _progressRectWidth + 4 * _margin : 2 * _margin ),
                                       rect.height - 2 * _margin ),
                 progressRect = new Rect( labelRect.xMax + _margin, _margin,
                                          _progressRectWidth,
                                          rect.height - 2 * _margin ),
                 lastUpdateRect = new Rect( progressRect.xMax + _margin, _margin,
                                            _lastUpdateRectWidth,
                                            rect.height - 2 * _margin );

            string text = Label + "\n<i>" +
                          ( Targets.Count() < 4 ? string.Join( ", ", Targets ) : "<multiple>" )
                          + "</i>";

#if DEBUG
            text += Priority;
#endif

            GUI.BeginGroup( rect );
            try
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label( labelRect, text );

                // if the bill has a manager job, give some more info.
                if ( active )
                {
                    // draw progress bar
                    Trigger.DrawProgressBar( progressRect, Suspended );

                    // draw time since last action
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label( lastUpdateRect, ( Find.TickManager.TicksGame - LastAction ).TimeString() );

                    // set tooltips
                    TooltipHandler.TipRegion( progressRect,
                                              "FMP.ThresholdCount".Translate( Trigger.CurCount, Trigger.Count ) );
                    TooltipHandler.TipRegion( lastUpdateRect,
                                              "FM.LastUpdateTooltip".Translate(
                                                  ( Find.TickManager.TicksGame - LastAction ).TimeString() ) );
                    if ( !( Targets.Count() < 4 ) )
                    {
                        TooltipHandler.TipRegion( labelRect, string.Join( ", ", Targets ) );
                    }
                }
            }
            finally
            {
                // make sure everything is always properly closed / reset to defaults.
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.EndGroup();
            }
        }

        public override void DrawOverviewDetails( Rect rect )
        {
            if ( historyShown == null )
            {
                historyShown = day;
            }
            historyShown.DrawPlot( rect, Trigger.Count );

            Rect switchRect = new Rect( rect.xMax - 16f - _margin, rect.yMin + _margin, 16f, 16f );
            Widgets.DrawHighlightIfMouseover( switchRect );
            if ( Widgets.ImageButton( switchRect, _cogTex ) )
            {
                List< FloatMenuOption > options = new List< FloatMenuOption >
                {
                    new FloatMenuOption( "Day", delegate { historyShown = day; } ),
                    new FloatMenuOption( "Month", delegate { historyShown = month; } ),
                    new FloatMenuOption( "Year", delegate { historyShown = year; } )
                };
                Find.WindowStack.Add( new FloatMenu( options ) );
            }
        }

        public override bool TryDoJob()
        {
            // did we do any work?
            bool workDone = false;

            // clean designations not in area
            CleanAreaDesignations();

            // clean dead designations
            CleanDesignations();

            // unforbid if required
            if ( UnforbidCorpses )
            {
                DoUnforbidCorpses();
            }

            // get the total count of meat in storage, expected meat in corpses and expected meat in designations.
            int totalCount = Trigger.CurCount + GetMeatInCorpses() + GetMeatInDesignations();

            // get a list of huntable animals sorted by distance (ignoring obstacles) and expected meat count.
            // note; attempt to balance cost and benefit, current formula: value = meat / ( distance ^ 2)
            List< Pawn > huntableAnimals = GetHuntableAnimalsSorted();

            // while totalCount < count AND we have animals that can be designated, designate animal.
            for ( int i = 0; i < huntableAnimals.Count && totalCount < Trigger.Count; i++ )
            {
                AddDesignation( huntableAnimals[i] );
                totalCount += huntableAnimals[i].EstimatedMeatCount();
                workDone = true;
            }

            return workDone;
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

        private void AddDesignation( Pawn p )
        {
            // create designation
            Designation des = new Designation( p, DesignationDefOf.Hunt );

            // add to game
            Find.DesignationManager.AddDesignation( des );

            // add to internal list
            Designations.Add( des );
        }

        private List< Pawn > GetHuntableAnimalsSorted()
        {
            // we need to define a 'base' position to calculate distances.
            // Try to find a managerstation (in all non-debug cases this method will only fire if there is such a station).
            IntVec3 position = IntVec3.Zero;
            Building managerStation =
                Find.ListerBuildings.AllBuildingsColonistOfClass< Building_ManagerStation >().FirstOrDefault();
            if ( managerStation != null )
            {
                position = managerStation.Position;
            }

            // otherwise, use the average of the home area. Not ideal, but it'll do.
            else
            {
                List< IntVec3 > homeCells = Find.AreaManager.Get< Area_Home >().ActiveCells.ToList();
                for ( int i = 0; i < homeCells.Count(); i++ )
                {
                    position += homeCells[i];
                }
                position.x /= homeCells.Count;
                position.y /= homeCells.Count;
                position.z /= homeCells.Count;
            }

            // get a list of alive animals that are not designated in the hunting grounds and are reachable, sorted by meat / distance * 2
            List< Pawn > list = Find.ListerPawns.AllPawns.Where( p => p.RaceProps.Animal
                                                                      && !p.health.Dead
                                                                      && p.SpawnedInWorld

                // wild animals only
                                                                      && p.Faction == null

                // non-biome animals won't be on the list
                                                                      && AllowedAnimals.ContainsKey( p.kindDef )
                                                                      && AllowedAnimals[p.kindDef]
                                                                      &&
                                                                      Find.DesignationManager.DesignationOn( p ) == null
                                                                      &&
                                                                      ( HuntingGrounds == null ||
                                                                        HuntingGrounds.ActiveCells.Contains( p.Position ) )
                                                                      && p.Position.CanReachColony() )

                // OrderBy defaults to ascending, switch sign on estimated meat count to get descending
                                    .OrderBy(
                                        p =>
                                            - p.EstimatedMeatCount() /
                                            ( Math.Sqrt( position.DistanceToSquared( p.Position ) ) * 2 ) ).ToList();

            return list;
        }

        // copypasta from autohuntbeacon by Carry
        // https://ludeon.com/forums/index.php?topic=8930.0
        private static void DoUnforbidCorpses()
        {
            foreach ( Thing current in Corpses )
            {
                Corpse corpse = current as Corpse;

                // don't unforbid corpses in storage - we're going to assume they were manually set.
                if ( corpse != null &&
                     !corpse.IsInAnyStorage() &&
                     corpse.IsForbidden( Faction.OfColony ) )
                {
                    // only fresh corpses
                    CompRottable comp = corpse.GetComp< CompRottable >();
                    if ( comp != null &&
                         comp.Stage == RotStage.Fresh )
                    {
                        // unforbid
                        // note: this doesn't count as work
                        corpse.SetForbidden( false, false );
                    }
                }
            }
        }

        public int GetMeatInCorpses()
        {
            // get current count + corpses in storage that is not a grave + designated count
            // current count in storage
            int count = 0;

            // corpses not buried / forbidden
            foreach ( Thing current in Corpses )
            {
                // make sure it's a real corpse. (I dunno, poke it?)
                // and that it's not forbidden (anymore) and can be reached.
                Corpse corpse = current as Corpse;
                if ( corpse != null &&
                     !corpse.IsForbidden( Faction.OfColony ) &&
                     corpse.Position.CanReachColony() )
                {
                    // check to see if it's buried.
                    bool buried = false;
                    SlotGroup slotGroup = Find.SlotGroupManager.SlotGroupAt( corpse.Position );
                    if ( slotGroup != null )
                    {
                        Building_Storage building_Storage = slotGroup.parent as Building_Storage;

                        // Sarcophagus inherits grave, here's to hoping Ty and modders stick to that in the future.
                        if ( building_Storage != null &&
                             building_Storage.def == ThingDefOf.Grave )
                        {
                            buried = true;
                        }
                    }

                    if ( !buried )
                    {
                        count += corpse.GetMeatCount();
                    }
                }
            }

            return count;
        }

        public int GetMeatInDesignations()
        {
            int count = 0;

            // designated animals
            foreach ( Designation des in Find.DesignationManager.DesignationsOfDef( DesignationDefOf.Hunt ) )
            {
                // make sure target is a pawn, is an animal, is not forbidden and somebody can reach it.
                // note: could be rolled into a fancy LINQ chain, but this is probably clearer.
                Pawn target = des.target.Thing as Pawn;
                if ( target != null &&
                     target.RaceProps.Animal &&
                     !target.IsForbidden( Faction.OfColony ) &&
                     target.Position.CanReachColony() )
                {
                    count += target.EstimatedMeatCount();
                }
            }

            return count;
        }

        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Hunting;

        public override void Tick()
        {
            if ( Find.TickManager.TicksGame % day.Interval == 0 )
            {
                day.Add( Trigger.CurCount );
            }
            if ( Find.TickManager.TicksGame % month.Interval == 0 )
            {
                month.Add( Trigger.CurCount );
            }
            if ( Find.TickManager.TicksGame % year.Interval == 0 )
            {
                year.Add( Trigger.CurCount );
            }
        }
    }
}