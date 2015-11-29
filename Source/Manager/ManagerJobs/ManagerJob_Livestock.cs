// Manager/ManagerJob_Livestock.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-22 15:53

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace FM
{
    public class ManagerJob_Livestock : ManagerJob
    {
        public enum AgeAndSex
        {
            AdultFemale    = 0,
            AdultMale      = 1,
            JuvenileFemale = 2,
            JuvenileMale   = 3
        }

        public static AgeAndSex[] AgeSexArray = (AgeAndSex[])Enum.GetValues( typeof (AgeAndSex) );
        private History _history;
        public bool ButcherExcess;
        public bool ButcherTrained;
        private List<Designation> Designations;
        public bool RestrictToArea;
        public List<Area> RestrictArea;
        public Area TameArea;
        public TrainingTracker Training;
        public new Trigger_PawnKind Trigger;
        public bool TryTameMore;
        public override string Label => "FML.Livestock".Translate();

        public override bool Completed
        {
            get
            {
                return Trigger.State;

                // we're only done when all counts are spot on.
            }
        }

        public override ManagerTab Tab
        {
            get { return Manager.Get.ManagerTabs.OfType<ManagerTab_Livestock>().First(); }
        }

        public override string[] Targets
        {
            get
            {
                return
                    AgeSexArray.Select( ageSex => ( "FMP." + ageSex.ToString() + "Count" )
                                            .Translate( Trigger.GetTame( ageSex ).Count, Trigger.CountTargets[ageSex] ) )
                               .ToArray();
            }
        }

        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Handling;

        public ManagerJob_Livestock()
        {
            // init designations
            Designations = new List<Designation>();

            // start history tracker
            _history = new History( AgeSexArray.Select( ageSex => ageSex.ToString() ).ToArray() );

            // set up the trigger, set all target counts to 5
            Trigger = new Trigger_PawnKind { CountTargets = AgeSexArray.ToDictionary( k => k, v => 5 ) };

            // set all training to false
            Training = new TrainingTracker();

            // set areas for restriction and taming to unrestricted
            TameArea = null;
            RestrictToArea = false;
            RestrictArea = AgeSexArray.Select( k => (Area)null).ToList();

            // set defaults for boolean options
            TryTameMore = false;
            ButcherExcess = true;
            ButcherTrained = false;
        }

        public ManagerJob_Livestock( PawnKindDef pawnKindDef ) : this() // set defaults
        {
            // set pawnkind and get list of current colonist pawns of that def.
            Trigger.pawnKind = pawnKindDef;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.LookValue( ref ButcherExcess, "ButcherExcess", true );
            Scribe_Values.LookValue( ref ButcherTrained, "ButcherTrained", false );
            Scribe_Values.LookValue( ref RestrictToArea, "RestrictToArea", false );
            Scribe_Collections.LookList( ref RestrictArea, "AreaRestrictions", LookMode.MapReference);
            Scribe_References.LookReference( ref TameArea, "TameArea" );
            Scribe_Values.LookValue( ref TryTameMore, "TryTameMore", false );
            Scribe_Deep.LookDeep( ref Training, "Training" );
            Scribe_Deep.LookDeep( ref Trigger, "Trigger" );

            // our current designations
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                // populate with all designations.
                Designations.AddRange( Find.DesignationManager.DesignationsOfDef( DesignationDefOf.Slaughter ).Where( des => ( (Pawn)des.target.Thing ).kindDef == Trigger.pawnKind ) );
                Designations.AddRange( Find.DesignationManager.DesignationsOfDef( DesignationDefOf.Tame ).Where( des => ( (Pawn)des.target.Thing ).kindDef == Trigger.pawnKind ) );
            }

            // this is an array of strings as the first (and only) parameter - make sure it doesn't get cast to array of objects for multiple parameters.
            Scribe_Deep.LookDeep( ref _history, "History",
                                  new object[] { AgeSexArray.Select( ageSex => ageSex.ToString() ).ToArray() } );
        }

        public override bool TryDoJob()
        {
            // work done?
            bool actionTaken = false;

#if DEBUG_LIFESTOCK
            Log.Message("Doing livestock (" + Trigger.pawnKind.LabelCap + ") job");
#endif

            // update changes in game designations in our managed list 
            // intersect filters our list down to designations that exist both in our list and in the game state. 
            // This should handle manual cancellations and natural completions. 
            // it deliberately won't add new designations made manually.
            Designations = Designations.Intersect( Find.DesignationManager.allDesignations ).ToList();

            // area restrictions
            DoAreaRestrictions( ref actionTaken );

            // handle butchery
            DoButcherJobs( ref actionTaken );

            // handle training
            DoTrainingJobs( ref actionTaken );

            // handle taming
            DoTamingJobs( ref actionTaken );

            return actionTaken;
        }

        private void DoAreaRestrictions( ref bool actionTaken )
        {
            if ( RestrictToArea )
            {
                for (int i = 0; i < AgeSexArray.Length; i++ )
                {
                    foreach ( Pawn p in Trigger.GetTame( AgeSexArray[i] ) )
                    {
                        if ( p.playerSettings.AreaRestriction != RestrictArea[i] )
                        {
                            actionTaken = true;
                            p.playerSettings.AreaRestriction = RestrictArea[i];
                        }
                    }
                }
            }
        }

        public List<Designation> DesignationsOfOn( DesignationDef def, AgeAndSex ageSex )
        {
            return Designations.Where( des => des.def == def
                                           && des.target.HasThing
                                           && des.target.Thing is Pawn
                                           && Trigger.PawnIsOfAgeSex( (Pawn)des.target.Thing, ageSex ) )
                            .ToList();
        }

        private bool TryRemoveDesignation( AgeAndSex ageSex, DesignationDef def )
        {
            // get current designations
            List<Designation> currentDesignations = DesignationsOfOn( def, ageSex );

            // if none, return false
            if ( currentDesignations.Count == 0 )
            {
                return false;
            }

            // else, remove one from the game as well as our managed list. (delete last - this should be the youngest/oldest).
            Designations.Remove( currentDesignations.Last() );
            currentDesignations.Last().Delete();
            return true;
        }

        public void AddDesignation( Pawn p, DesignationDef def )
        {
            // create and add designation to the game and our managed list.
            Designation des = new Designation( p, def );
            Designations.Add( des );
            Find.DesignationManager.AddDesignation( des );
        }

        private void DoTrainingJobs( ref bool actionTaken )
        {
            actionTaken = false;

            foreach ( AgeAndSex ageSex in AgeSexArray )
            {
                foreach ( Pawn animal in Trigger.GetTame( ageSex ) )
                {
                    foreach ( TrainableDef def in Training.Defs )
                    {
                        if ( !animal.training.IsCompleted( def ) &&

                             // only ever assign training, never de-asign.
                             animal.training.GetWanted( def ) != Training[def] &&
                             Training[def] )
                        {
                            animal.training.SetWanted( def, Training[def] );
                            actionTaken = true;
                        }
                    }
                }
            }
        }

        private void DoTamingJobs( ref bool actionTaken )
        {
            if ( !TryTameMore )
            {
                return;
            }

            foreach ( AgeAndSex ageSex in AgeSexArray )
            {
                // not enough animals?
                int deficit = Trigger.CountTargets[ageSex] 
                                - Trigger.GetTame( ageSex ).Count 
                                - DesignationsOfOn( DesignationDefOf.Tame, ageSex ).Count;


#if DEBUG_LIFESTOCK
                Log.Message( "Taming " + ageSex + ", deficit: " + deficit );
#endif

                if( deficit > 0 )
                {
                    // get the 'home' position
                    IntVec3 position = Utilities.GetBasePosition();

                    // get list of animals in sorted by youngest weighted to distance.
                    List<Pawn> animals = Trigger.GetWild( ageSex )
                                                .Where(p => Find.DesignationManager.DesignationOn( p ) == null
                                                        && TameArea == null || TameArea.ActiveCells.Contains(p.Position))
                                                .OrderBy( p => p.ageTracker.AgeBiologicalTicks / (p.Position.DistanceToSquared(position) * 2 ))
                                                .ToList();

#if DEBUG_LIFESTOCK
                    Log.Message( "Wild: " + animals.Count );
#endif

                    for( int i = 0; i < deficit && i < animals.Count; i++ )
                    {
#if DEBUG_LIFESTOCK
                        Log.Message( "Adding taming designation: " + animals[i].GetUniqueLoadID() );
#endif
                        AddDesignation( animals[i], DesignationDefOf.Tame );
                    }
                }

                // remove extra designations
                while ( deficit < 0 )
                {
                    if ( TryRemoveDesignation( ageSex, DesignationDefOf.Tame ) )
                    {
#if DEBUG_LIFESTOCK
                        Log.Message( "Removed extra taming designation" );
#endif
                        actionTaken = true;
                        deficit++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void DoButcherJobs( ref bool actionTaken )
        {
            if ( !ButcherExcess )
            {
                return;
            }


#if DEBUG_LIFESTOCK
            Log.Message("Doing butchery: " + Trigger.pawnKind.LabelCap);
#endif

            foreach( AgeAndSex ageSex in AgeSexArray )
            {
                // too many animals?
                int surplus = Trigger.GetTame( ageSex ).Count
                              - DesignationsOfOn( DesignationDefOf.Slaughter, ageSex ).Count
                              - Trigger.CountTargets[ageSex];


#if DEBUG_LIFESTOCK
                Log.Message( "Butchering " + ageSex + ", surplus" + surplus );
#endif

                if( surplus > 0 )
                {
                    // should slaughter oldest adults, youngest juveniles.
                    bool oldestFirst = ageSex == AgeAndSex.AdultFemale || ageSex == AgeAndSex.AdultMale;

                    // get list of animals in correct sort order.
                    List<Pawn> animals = Trigger.GetTame( ageSex )
                                         .Where( p => Find.DesignationManager.DesignationOn( p, DesignationDefOf.Slaughter ) == null
                                                 && ButcherTrained || !p.training.IsCompleted( TrainableDefOf.Obedience ) )
                                         .OrderBy( p => ( oldestFirst ? -1 : 1 ) * p.ageTracker.AgeBiologicalTicks )
                                         .ToList();


#if DEBUG_LIFESTOCK
                    Log.Message("Tame animals: " + animals.Count);
#endif

                    for( int i = 0; i < surplus && i < animals.Count; i++ )
                    {
#if DEBUG_LIFESTOCK
                        Log.Message( "Butchering " + animals[i].GetUniqueLoadID() );
#endif
                        AddDesignation( animals[i], DesignationDefOf.Slaughter);
                    }
                }

                // remove extra designations
                while ( surplus < 0 )
                {
                    if( TryRemoveDesignation( ageSex, DesignationDefOf.Slaughter ) )
                    {
#if DEBUG_LIFESTOCK
                        Log.Message( "Removed extra butchery designation" );
#endif
                        actionTaken = true;
                        surplus++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public override void CleanUp()
        {
            foreach ( Designation des in Designations )
            {
                des.Delete();
            }
            Designations.Clear();
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | (bar | last update)/(stamp) -> handled in Utilities.DrawStatusForListEntry

            // set up rects
            Rect labelRect = new Rect( Utilities.Margin, Utilities.Margin, rect.width -
                                                                           ( active
                                                                               ? StatusRectWidth + 4 * Utilities.Margin
                                                                               : 2 * Utilities.Margin ),
                                       rect.height - 2 * Utilities.Margin ),
                 statusRect = new Rect( labelRect.xMax + Utilities.Margin, Utilities.Margin, StatusRectWidth,
                                        rect.height - 2 * Utilities.Margin );

            // create label string
            string text = Label + "\n<i>" + Trigger.pawnKind.LabelCap + "</i>";
            string tooltip = Trigger.StatusTooltip;

            // do the drawing
            GUI.BeginGroup( rect );

            // draw label
            Utilities.Label( labelRect, text, tooltip );

            // if the bill has a manager job, give some more info.
            if ( active )
            {
                this.DrawStatusForListEntry( statusRect, Trigger );
            }
            GUI.EndGroup();
        }

        public override void DrawOverviewDetails( Rect rect )
        {
            _history.DrawPlot( rect );
        }

        public override void Tick()
        {
            _history.Update( Trigger.Counts );
        }

        public AcceptanceReport CanBeTrained( PawnKindDef pawnKind, TrainableDef td, out bool visible )
        {
            if ( pawnKind.RaceProps.untrainableTags != null )
            {
                for ( int index = 0; index < pawnKind.RaceProps.untrainableTags.Count; ++index )
                {
                    if ( td.MatchesTag( pawnKind.RaceProps.untrainableTags[index] ) )
                    {
                        visible = false;
                        return false;
                    }
                }
            }
            if ( pawnKind.RaceProps.trainableTags != null )
            {
                for ( int index = 0; index < pawnKind.RaceProps.trainableTags.Count; ++index )
                {
                    if ( td.MatchesTag( pawnKind.RaceProps.trainableTags[index] ) )
                    {
                        if ( pawnKind.RaceProps.baseBodySize < (double)td.minBodySize )
                        {
                            visible = true;
                            return new AcceptanceReport( "CannotTrainTooSmall".Translate( (object)pawnKind.LabelCap ) );
                        }
                        visible = true;
                        return true;
                    }
                }
            }
            if ( !td.defaultTrainable )
            {
                visible = false;
                return false;
            }
            if ( pawnKind.RaceProps.baseBodySize < (double)td.minBodySize )
            {
                visible = true;
                return new AcceptanceReport( "CannotTrainTooSmall".Translate( (object)pawnKind.LabelCap ) );
            }
            if ( pawnKind.RaceProps.trainableIntelligence < td.requiredTrainableIntelligence )
            {
                visible = true;
                return
                    new AcceptanceReport(
                        "CannotTrainNotSmartEnough".Translate( (object)td.requiredTrainableIntelligence ) );
            }
            visible = true;
            return true;
        }

        public void DrawTrainingSelector( Rect rect, float lrMargin = 0f )
        {
            if ( lrMargin > 0 )
            {
                rect.xMin += lrMargin;
                rect.width -= 2 * lrMargin;
            }

            float width = rect.width / Training.Count;
            List<TrainableDef> keys = Training.Defs;

            GUI.BeginGroup( rect );
            for ( int i = 0; i < Training.Count; i++ )
            {
                Rect cell = new Rect( i * width, 0f, width, rect.height );
                bool vis;
                AcceptanceReport report = CanBeTrained( Trigger.pawnKind, keys[i], out vis );
                if ( vis && report.Accepted )
                {
                    bool checkOn = Training[keys[i]];
                    Utilities.DrawToggle( cell, keys[i].LabelCap, ref checkOn, 16f, 0f, GameFont.Tiny );
                    Training[keys[i]] = checkOn;
                }
                else if ( vis )
                {
                    Utilities.Label( cell, keys[i].LabelCap, report.Reason, font: GameFont.Tiny, color: Color.grey );
                }
            }
            GUI.EndGroup();
        }

        public class TrainingTracker : IExposable
        {
            public DefMap<TrainableDef, bool> TrainingTargets = new DefMap<TrainableDef, bool>();

            public bool this[ TrainableDef index ]
            {
                get { return TrainingTargets[index]; }
                set { SetWantedRecursive( index, value ); }
            }

            public int Count
            {
                get { return TrainingTargets.Count; }
            }

            public List<TrainableDef> Defs
            {
                get { return DefDatabase<TrainableDef>.AllDefsListForReading; }
            }

            private void SetWantedRecursive( TrainableDef td, bool wanted )
            {
                TrainingTargets[td] = wanted;
                if ( wanted )
                {
                    SoundDefOf.CheckboxTurnedOn.PlayOneShotOnCamera();
                    if ( td.prerequisites != null )
                    {
                        foreach ( TrainableDef trainable in td.prerequisites )
                        {
                            SetWantedRecursive( trainable, true );
                        }
                    }
                }
                else
                {
                    SoundDefOf.CheckboxTurnedOff.PlayOneShotOnCamera();
                    IEnumerable<TrainableDef> enumerable = from t in DefDatabase<TrainableDef>.AllDefsListForReading
                                                           where
                                                               t.prerequisites != null && t.prerequisites.Contains( td )
                                                           select t;
                    foreach ( TrainableDef current in enumerable )
                    {
                        SetWantedRecursive( current, false );
                    }
                }
            }

            public void ExposeData()
            {
                Scribe_Deep.LookDeep( ref TrainingTargets, "TrainingTargets" );
            }
        }
    }
}