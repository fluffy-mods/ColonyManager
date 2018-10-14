// Karel Kroeze
// ManagerJob_Livestock.cs
// 2016-12-09

using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class ManagerJob_Livestock : ManagerJob
    {
        private History _history;
        public bool ButcherExcess;
        public bool ButcherTrained;
        public bool ButcherPregnant;
        public bool ButcherBonded;
        private List<Designation> _designations;
        public List<Designation> Designations => new List<Designation>( _designations );
        public List<Area> RestrictArea;
        public bool RestrictToArea;
        public Area TameArea;
        public bool SendToSlaughterArea;
        public Area SlaughterArea;
        public bool SendToTrainingArea;
        public Area TrainingArea;
        public TrainingTracker Training;
        public new Trigger_PawnKind Trigger;
        public bool TryTameMore;
        public bool SetFollow;
        public bool FollowDrafted;
        public bool FollowFieldwork;
        public bool FollowTraining;
        public MasterMode Masters;
        public Pawn Master;
        public MasterMode Trainers;
        public Pawn Trainer;

        static ManagerJob_Livestock()
        {
            SetWanted_MI = typeof( Pawn_TrainingTracker ).GetMethod( "SetWanted", BindingFlags.NonPublic | BindingFlags.Instance );
            if ( SetWanted_MI == null )
                throw new NullReferenceException( "Could not find Pawn_TrainingTracker.SetWanted()" );
        }

        public ManagerJob_Livestock( Manager manager ) : base( manager )
        {
            // init designations
            _designations = new List<Designation>();

            // start history tracker
            _history = new History( Utilities_Livestock.AgeSexArray.Select( ageSex => ageSex.ToString() ).ToArray() );

            // set up the trigger, set all target counts to 5
            Trigger = new Trigger_PawnKind( this.manager );

            // set all training to false
            Training = new TrainingTracker();

            // set areas for restriction and taming to unrestricted
            TameArea = null;
            RestrictToArea = false;
            RestrictArea = Utilities_Livestock.AgeSexArray.Select( k => (Area)null ).ToList();

            // set up sending animals designated for slaughter to an area (freezer)
            SendToSlaughterArea = false;
            SlaughterArea = null;

            // set up training area
            SendToTrainingArea = false;
            TrainingArea = null;

            // taming
            TryTameMore = false;
            TameArea = null;

            // set defaults for butchering
            ButcherExcess = true;
            ButcherTrained = false;
            ButcherPregnant = false;
            ButcherBonded = false;

            // following
            SetFollow = true;
            FollowDrafted = true;
            FollowFieldwork = true;
            FollowTraining = false;
            Masters = MasterMode.Default;
            Master = null;
            Trainers = MasterMode.Default;
            Trainer = null;
        }

        public ManagerJob_Livestock( PawnKindDef pawnKindDef, Manager manager ) : this( manager ) // set defaults
        {
            // set pawnkind and get list of current colonist pawns of that def.
            Trigger.pawnKind = pawnKindDef;
        }

        public override string Label => Trigger.pawnKind.LabelCap;

        public override bool Completed
        {
            get
            {
                // state for lifestock trigger includes counts as well as training targets.
                return Trigger.State;
            }
        }

        public override ManagerTab Tab
        {
            get { return Manager.For( manager ).Tabs.OfType<ManagerTab_Livestock>().First(); }
        }

        public override string[] Targets
        {
            get
            {
                return Utilities_Livestock.AgeSexArray
                    .Select( ageSex =>
                        ( "FMP." + ageSex.ToString() + "Count" ).Translate(
                            Trigger.pawnKind.GetTame( manager, ageSex ).Count,
                            Trigger.CountTargets[ageSex] ) )
                    .ToArray();
            }
        }

        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Handling;
        
        public override void ExposeData()
        {
            base.ExposeData();

            // settings, references first!
            Scribe_References.Look( ref TameArea, "TameArea" );
            Scribe_References.Look( ref SlaughterArea, "SlaughterArea" );
            Scribe_References.Look( ref TrainingArea, "TrainingArea" );
            Scribe_References.Look( ref Master, "Master" );
            Scribe_References.Look( ref Trainer, "Trainer" );
            Scribe_Collections.Look( ref RestrictArea, "AreaRestrictions", LookMode.Reference );
            Scribe_Deep.Look( ref Trigger, "trigger", manager );
            Scribe_Deep.Look( ref Training, "Training" );
            Scribe_Values.Look( ref ButcherExcess, "ButcherExcess", true );
            Scribe_Values.Look( ref ButcherTrained, "ButcherTrained", false );
            Scribe_Values.Look( ref ButcherPregnant, "ButcherPregnant", false );
            Scribe_Values.Look(ref ButcherBonded, "ButcherBonded", false);
            Scribe_Values.Look( ref RestrictToArea, "RestrictToArea", false );
            Scribe_Values.Look( ref SendToSlaughterArea, "SendToSlaughterArea", false );
            Scribe_Values.Look( ref SendToTrainingArea, "SendToTrainingArea", false );
            Scribe_Values.Look( ref TryTameMore, "TryTameMore", false );
            Scribe_Values.Look( ref SetFollow, "SetFollow", true );
            Scribe_Values.Look( ref FollowDrafted, "FollowDrafted", true );
            Scribe_Values.Look( ref FollowFieldwork, "FollowFieldwork", true );
            Scribe_Values.Look( ref FollowTraining, "FollowTraining", false );
            Scribe_Values.Look( ref Masters, "Masters", MasterMode.Default );
            Scribe_Values.Look( ref Trainers, "Trainers", MasterMode.Default );

            // our current designations
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                // populate with all designations.
                _designations.AddRange(
                                      manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.Slaughter )
                                             .Where( des => ( (Pawn)des.target.Thing ).kindDef == Trigger.pawnKind ) );
                _designations.AddRange(
                                      manager.map.designationManager.SpawnedDesignationsOfDef( DesignationDefOf.Tame )
                                             .Where( des => ( (Pawn)des.target.Thing ).kindDef == Trigger.pawnKind ) );
            }

            Scribe_Deep.Look( ref _history, "History" );
        }

        public override bool TryDoJob()
        {
            // work done?
            var actionTaken = false;

#if DEBUG_LIFESTOCK
            Log.Message( "Doing livestock (" + Trigger.pawnKind.LabelCap + ") job" );
#endif

            // update changes in game designations in our managed list
            // intersect filters our list down to designations that exist both in our list and in the game state.
            // This should handle manual cancellations and natural completions.
            // it deliberately won't add new designations made manually.
            // Note that this also has the unfortunate side-effect of not re-adding designations after loading a game.
            _designations = _designations.Intersect( manager.map.designationManager.allDesignations ).ToList();

            // handle butchery
            DoButcherJobs( ref actionTaken );

            // area restrictions
            DoAreaRestrictions( ref actionTaken );

            // handle training
            DoTrainingJobs( ref actionTaken );

            // handle taming
            DoTamingJobs( ref actionTaken );

            // follow settings
            DoFollowSettings( ref actionTaken );

            return actionTaken;
        }

        private void DoAreaRestrictions( ref bool actionTaken )
        {
            if ( RestrictToArea )
            {
                for ( var i = 0; i < Utilities_Livestock.AgeSexArray.Length; i++ )
                {
                    foreach ( Pawn p in Trigger.pawnKind.GetTame( manager, Utilities_Livestock.AgeSexArray[i] ) )
                    {
                        // slaughter
                        if ( SendToSlaughterArea && manager.map.designationManager.DesignationOn( p, DesignationDefOf.Slaughter ) != null )
                        {
                            actionTaken = p.playerSettings.AreaRestriction != SlaughterArea;
                            p.playerSettings.AreaRestriction = SlaughterArea;
                        }

                        // training
                        else if ( SendToTrainingArea && p.training.NextTrainableToTrain() != null )
                        {
                            if ( p.playerSettings.AreaRestriction != TrainingArea )
                            {
                                actionTaken = true;
                                p.playerSettings.AreaRestriction = TrainingArea;
                            }
                        }

                        // all
                        else if ( p.playerSettings.AreaRestriction != RestrictArea[i] )
                        {
                            actionTaken = true;
                            p.playerSettings.AreaRestriction = RestrictArea[i];
                        }
                    }
                }
            }
        }
        
        public void DoFollowSettings( ref bool actionTaken )
        {
            foreach ( var animal in Trigger.pawnKind.GetTame( manager ) )
            {
                // training
                Logger.Follow( animal.LabelShort );
                if ( FollowTraining && animal.training.NextTrainableToTrain() != null )
                {
                    Logger.Follow( "\ttraining"  );
                    if ( Trainers != MasterMode.Default )
                    {
                        SetMaster( animal, Trainers, Trainer, ref actionTaken );
                        SetFollowing( animal, false, true, ref actionTaken);
                    }
                }

                // default 
                else
                {
                    if ( Masters != MasterMode.Default )
                    {
                        SetMaster( animal, Masters, Master, ref actionTaken);
                    }
                    if ( SetFollow )
                    {
                        SetFollowing( animal, FollowDrafted, FollowFieldwork, ref actionTaken);
                    }
                }
            }
        }

        public void SetMaster( Pawn animal, MasterMode mode, Pawn specificMaster, ref bool actionTaken )
        {
            switch ( mode )
            {
                case MasterMode.Default:
                    break;
                case MasterMode.Specific:
                    SetMaster( animal, specificMaster, ref actionTaken );
                    break;
                default:
                    var master = GetMaster( animal, mode );
                    SetMaster( animal, master, ref actionTaken );
                    break;
            }
        }

        public Pawn GetMaster( Pawn animal, MasterMode mode )
        {
            var master = animal.playerSettings.Master;
            var options = animal.kindDef.GetMasterOptions( manager, mode );

            Logger.Follow( $"Getting master for {animal.LabelShort}:\n\tcurrent: {master?.LabelShort ?? "None"}\n\toptions:\n"  );
#if DEBUG_FOLLOW
            foreach ( var option in options )
            {
                Logger.Follow( $"\t\t{option.LabelShort}\n" );
            }
#endif

            // cop out if no options
            if ( options.NullOrEmpty() )
                return null;

            // if we currently have a master, our current master is a valid option, 
            // and all the options have roughly equal amounts of pets following them, we don't need to take action
            if ( master != null && options.Contains( master ) && RoughlyEquallyDistributed( options ) )
                return master;

            // otherwise, assign a master that has the least amount of current followers.
            return options.MinBy( p => p.GetFollowers().Count );
        }

        private bool RoughlyEquallyDistributed( List<Pawn> masters )
        {
            var followerCounts = masters.Select( p => p.GetFollowers( Trigger.pawnKind ).Count );
            return followerCounts.Max() - followerCounts.Min() <= 1;
        }

        public void SetMaster( Pawn animal, Pawn master, ref bool actionTaken )
        {
            Logger.Follow( $"Current: {master?.LabelShort ?? "None"}, New: {master?.LabelShort ?? "None"}"  );
            if ( animal.playerSettings.Master != master )
            {
                animal.playerSettings.Master = master;
                actionTaken = true;
            }
        }

        public void SetFollowing( Pawn animal, bool drafted, bool fieldwork, ref bool actionTaken )
        {
            if ( animal?.playerSettings == null )
            {
                Log.Warning( "NULL!" );
                return;
            }
            Logger.Follow( $"Current: {animal.playerSettings.followDrafted} | {animal.playerSettings.followFieldwork}, {drafted} | {fieldwork}" );
            if ( animal.playerSettings.followDrafted != drafted )
            {
                animal.playerSettings.followDrafted = drafted;
                actionTaken = true;
            }
            if ( animal.playerSettings.followFieldwork != fieldwork )
            {
                animal.playerSettings.followFieldwork = fieldwork;
                actionTaken = true;
            }
        }

        public List<Designation> DesignationsOfOn( DesignationDef def, AgeAndSex ageSex )
        {
            return _designations.Where( des => des.def == def
                                              && des.target.HasThing
                                              && des.target.Thing is Pawn
                                              && ( (Pawn)des.target.Thing ).PawnIsOfAgeSex( ageSex ) )
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
            var designation = currentDesignations.Last();
            _designations.Remove(designation);
            designation.Delete();
            return true;
        }

        public void AddDesignation( Pawn p, DesignationDef def )
        {
            // create and add designation to the game and our managed list.
            var des = new Designation( p, def );
            _designations.Add( des );
            manager.map.designationManager.AddDesignation( des );
        }

        private static MethodInfo SetWanted_MI;
        
        internal void DoTrainingJobs( ref bool actionTaken, bool assign = true )
        {
            actionTaken = false;

            foreach ( AgeAndSex ageSex in Utilities_Livestock.AgeSexArray )
            {
                // skip juveniles if TrainYoung is not enabled.
                if ( ageSex.Juvenile() && !Training.TrainYoung )
                    continue;

                foreach ( Pawn animal in Trigger.pawnKind.GetTame( manager, ageSex ) )
                {
                    foreach ( TrainableDef def in Training.Defs )
                    {
                        bool dump;
                        if ( !animal.training.HasLearned( def ) &&

                             // only train if allowed.
                             animal.training.CanAssignToTrain( def, out dump ).Accepted &&

                             // only ever assign training, never de-asign.
                             animal.training.GetWanted( def ) != Training[def] &&
                             Training[def] )
                        {
                            if ( assign ) SetWanted_MI.Invoke( animal.training, new object[] {def, Training[def]} );
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

            foreach ( AgeAndSex ageSex in Utilities_Livestock.AgeSexArray )
            {
                // not enough animals?
                int deficit = Trigger.CountTargets[ageSex]
                              - Trigger.pawnKind.GetTame( manager, ageSex ).Count
                              - DesignationsOfOn( DesignationDefOf.Tame, ageSex ).Count;

#if DEBUG_LIFESTOCK
                Log.Message( "Taming " + ageSex + ", deficit: " + deficit );
#endif

                if ( deficit > 0 )
                {
                    // get the 'home' position
                    IntVec3 position = manager.map.GetBaseCenter();

                    // get list of animals in sorted by youngest weighted to distance.
                    List<Pawn> animals = Trigger.pawnKind.GetWild( manager, ageSex )
                        .Where( p => p != null && p.Spawned &&
                                     manager.map.designationManager.DesignationOn( p ) == null &&
                                     ( TameArea == null || TameArea.ActiveCells.Contains( p.Position ) ) &&
                                     IsReachable( p ) ).ToList();

                    // skip if no animals available.
                    if ( animals.Count == 0 )
                        continue;

                    animals =
                        animals.OrderBy( p => p.ageTracker.AgeBiologicalTicks / Distance( p, position ) ).ToList();

#if DEBUG_LIFESTOCK
                    Log.Message( "Wild: " + animals.Count );
#endif

                    for ( var i = 0; i < deficit && i < animals.Count; i++ )
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
            Log.Message( "Doing butchery: " + Trigger.pawnKind.LabelCap );
#endif

            foreach ( AgeAndSex ageSex in Utilities_Livestock.AgeSexArray )
            {
                // too many animals?
                int surplus = Trigger.pawnKind.GetTame( manager, ageSex ).Count
                              - DesignationsOfOn( DesignationDefOf.Slaughter, ageSex ).Count
                              - Trigger.CountTargets[ageSex];

#if DEBUG_LIFESTOCK
                Log.Message( "Butchering " + ageSex + ", surplus" + surplus );
#endif

                if ( surplus > 0 )
                {
                    // should slaughter oldest adults, youngest juveniles.
                    bool oldestFirst = ageSex == AgeAndSex.AdultFemale ||
                                       ageSex == AgeAndSex.AdultMale;

                    // get list of animals in correct sort order.
                    List<Pawn> animals = Trigger.pawnKind.GetTame( manager, ageSex )
                                                .Where(
                                                       p => manager.map.designationManager.DesignationOn( p, DesignationDefOf.Slaughter ) == null
                                                       && ( ButcherTrained || !p.training.HasLearned( TrainableDefOf.Obedience ) )
                                                       && ( ButcherPregnant || !p.VisiblyPregnant() )
                                                       && ( ButcherBonded || !p.BondedWithColonist() ) )
                                                .OrderBy(
                                                         p => ( oldestFirst ? -1 : 1 ) * p.ageTracker.AgeBiologicalTicks )
                                                .ToList();

#if DEBUG_LIFESTOCK
                    Log.Message( "Tame animals: " + animals.Count );
#endif

                    for ( var i = 0; i < surplus && i < animals.Count; i++ )
                    {
#if DEBUG_LIFESTOCK
                        Log.Message( "Butchering " + animals[i].GetUniqueLoadID() );
#endif
                        AddDesignation( animals[i], DesignationDefOf.Slaughter );

                    }
                }

                // remove extra designations
                while ( surplus < 0) { 
                    if ( TryRemoveDesignation( ageSex, DesignationDefOf.Slaughter ) )
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
            foreach ( Designation des in _designations )
            {
                des.Delete();
            }

            _designations.Clear();
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | (bar | last update)/(stamp) -> handled in Utilities.DrawStatusForListEntry

            // set up rects
            Rect labelRect = new Rect(
                Margin, Margin, rect.width - ( active ? StatusRectWidth + 4 * Margin : 2 * Margin ),
                rect.height - 2 * Margin ),
                statusRect = new Rect( labelRect.xMax + Margin, Margin, StatusRectWidth,
                rect.height - 2 * Margin );

            // create label string
            string text = Label + "\n<i>";
            foreach ( AgeAndSex ageSex in Utilities_Livestock.AgeSexArray )
            {
                text += Trigger.pawnKind.GetTame( manager, ageSex ).Count + "/" + Trigger.CountTargets[ageSex] + ", ";
            }

            text += Trigger.pawnKind.GetWild( manager ).Count + "</i>";
            string tooltip = Trigger.StatusTooltip;

            // do the drawing
            GUI.BeginGroup( rect );

            // draw label
            Widgets_Labels.Label( labelRect, text, tooltip );

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
            if( _history.IsRelevantTick )
                _history.Update( Trigger.Counts );
        }

        public AcceptanceReport CanBeTrained( PawnKindDef pawnKind, TrainableDef td, out bool visible )
        {
            if ( pawnKind.RaceProps.untrainableTags != null )
            {
                for ( var index = 0; index < pawnKind.RaceProps.untrainableTags.Count; ++index )
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
                for ( var index = 0; index < pawnKind.RaceProps.trainableTags.Count; ++index )
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

            if ( pawnKind.RaceProps.trainability.intelligenceOrder < td.requiredTrainability.intelligenceOrder )
            {
                visible = true;
                return
                    new AcceptanceReport(
                        "CannotTrainNotSmartEnough".Translate( (object)td.requiredTrainability ) );
            }

            visible = true;
            return true;
        }


        public class TrainingTracker : IExposable
        {
            public DefMap<TrainableDef, bool> TrainingTargets = new DefMap<TrainableDef, bool>();
            public bool TrainYoung;

            public bool this[TrainableDef index]
            {
                get { return TrainingTargets[index]; }
                set { SetWantedRecursive( index, value ); }
            }

            public bool Any
            {
                get
                {
                    foreach ( TrainableDef def in Defs )
                    {
                        if ( TrainingTargets[def] )
                            return true;
                    }

                    return false;
                }
            }

            public int Count
            {
                get { return TrainingTargets.Count; }
            }

            public List<TrainableDef> Defs
            {
                get { return DefDatabase<TrainableDef>.AllDefsListForReading; }
            }

            public void ExposeData()
            {
                Scribe_Values.Look( ref TrainYoung, "TrainYoung", false );
                Scribe_Deep.Look( ref TrainingTargets, "TrainingTargets" );
            }

            private void SetWantedRecursive( TrainableDef td, bool wanted )
            {
                // cop out if nothing changed
                if ( TrainingTargets[td] == wanted )
                    return;

                // make changes
                TrainingTargets[td] = wanted;
                if ( wanted )
                {
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
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
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
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
        }
    }
}
