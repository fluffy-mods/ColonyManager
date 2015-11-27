// Manager/ManagerJob_Lifestock.cs
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
using Verse.Noise;
using Verse.Sound;

namespace FM
{
    public class ManagerJob_Lifestock : ManagerJob
    {
        public enum ageAndSex
        {
            AdultMale,
            AdultFemale,
            JuvenileMale,
            JuvenileFemale
        }

        public static ageAndSex[]              AgeSexArray              = (ageAndSex[])Enum.GetValues( typeof (ageAndSex) );
        private List<Pawn>                     _animals;
        public bool                            ButcherTrained;
        public bool                            ButcherExcess;
        public Dictionary<ageAndSex, int>      CountTargets;
        private PawnKindDef                    _pawnKind;
        public Area                            RestrictArea;
        public Area                            TameArea;
        public TrainingTracker                 Training;
        public bool                            TryTameMore;
        private History                        _history;
        public override string                 Label                    => "FML.Livestock".Translate();

        public class TrainingTracker
        {
            public DefMap<TrainableDef, bool> TrainingTargets = new DefMap<TrainableDef, bool>(); 

            public bool this[ TrainableDef index ]
            {
                get { return TrainingTargets[index]; }
                set
                {
                    SetWantedRecursive(index, value);
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
            
            private void SetWantedRecursive( TrainableDef td, bool wanted )
            {
                TrainingTargets[td] = wanted;
                if( wanted )
                {
                    SoundDefOf.CheckboxTurnedOn.PlayOneShotOnCamera();
                    if( td.prerequisites != null )
                    {
                        foreach( TrainableDef trainable in td.prerequisites )
                        {
                            SetWantedRecursive( trainable, true );
                        }
                    }
                }
                else
                {
                    SoundDefOf.CheckboxTurnedOff.PlayOneShotOnCamera();
                    IEnumerable<TrainableDef> enumerable = from t in DefDatabase<TrainableDef>.AllDefsListForReading
                                                           where t.prerequisites != null && t.prerequisites.Contains(td)
                                                           select t;
                    foreach( TrainableDef current in enumerable )
                    {
                        SetWantedRecursive( current, false );
                    }
                }
            }
        }

        public override bool Completed
        {
            get
            {
                // TODO: job completed logic.
                return true;
            }
        }

        public override ManagerTab Tab
        {
            get { return Manager.Get.ManagerTabs.OfType<ManagerTab_Lifestock>().First(); }
        }

        public override string[] Targets
        {
            get
            {
                return
                    AgeSexArray.Select( ageSex => ( "FMP." + ageSex.ToString() + "Count" )
                                .Translate( Get( ageSex ).Count ) )
                                .ToArray();
            }
        }

        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Handling;

        public ManagerJob_Lifestock()
        {
            // animals to empty empty list
            _animals = new List<Pawn>();

            // start history tracker
            _history = new History( AgeSexArray.Select( ageSex => ageSex.ToString() ).ToArray() );

            // set all target counts to 5
            CountTargets = AgeSexArray.ToDictionary( k => k, v => 5 );

            // set all training to false
            Training = new TrainingTracker();

            // set areas for restriction and taming to unrestricted
            TameArea = null;
            RestrictArea = null;

            // set defaults for boolean options
            TryTameMore = false;
            ButcherExcess = true;
            ButcherTrained = false;
        }

        public ManagerJob_Lifestock( PawnKindDef pawnKindDef ) : this() // set defaults
        {
            // set pawnkind and get list of current colonist pawns of that def.
            _pawnKind = pawnKindDef;
            _animals = Find.ListerPawns.FreeColonistsSpawned
                           .Where( p => p.kindDef == _pawnKind && !p.health.Dead )
                           .ToList();
        }

        public int[] Counts
        {
            get { return AgeSexArray.Select( ageSex => Get( ageSex ).Count ).ToArray(); }
        }

        public List<Pawn> Get( ageAndSex ageSex )
        {
            switch ( ageSex )
            {
                // we're making the assumption here that anything with a lifestage index of 3 or greater is adult.
                // this works for vanilla and all modded animals that I know off.
                case ageAndSex.AdultFemale:
                    return _animals
                        .Where( p => p.gender == Gender.Female && p.ageTracker.CurLifeStageIndex >= 3 )
                        .ToList();
                case ageAndSex.AdultMale:
                    return _animals
                        .Where( p => p.gender == Gender.Male && p.ageTracker.CurLifeStageIndex >= 3 )
                        .ToList();
                case ageAndSex.JuvenileFemale:
                    return _animals
                        .Where( p => p.gender == Gender.Female && p.ageTracker.CurLifeStageIndex < 3 )
                        .ToList();
                default:
                    return _animals
                        .Where( p => p.gender == Gender.Male && p.ageTracker.CurLifeStageIndex < 3 )
                        .ToList();
            }
        }

        public override bool TryDoJob()
        {
            // TODO: job logic.
            return false;
        }

        public override void CleanUp()
        {
            // TODO: cleanup job.
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // TODO: list entry.
        }

        public override void DrawOverviewDetails( Rect rect )
        {
            _history.DrawPlot( rect );
        }

        public override void Tick()
        {
            // TODO: get counts.
            _history.Update();
        }

        public AcceptanceReport CanBeTrained( PawnKindDef pawnKind, TrainableDef td, out bool visible)
        {
            if( pawnKind.RaceProps.untrainableTags != null )
            {
                for( int index = 0; index < pawnKind.RaceProps.untrainableTags.Count; ++index )
                {
                    if( td.MatchesTag( pawnKind.RaceProps.untrainableTags[index] ) )
                    {
                        visible = false;
                        return (AcceptanceReport)false;
                    }
                }
            }
            if( pawnKind.RaceProps.trainableTags != null )
            {
                for( int index = 0; index < pawnKind.RaceProps.trainableTags.Count; ++index )
                {
                    if( td.MatchesTag( pawnKind.RaceProps.trainableTags[index] ) )
                    {
                        if( (double)pawnKind.RaceProps.baseBodySize < (double)td.minBodySize )
                        {
                            visible = true;
                            return new AcceptanceReport( "CannotTrainTooSmall".Translate( (object)pawnKind.LabelCap ) );
                        }
                        visible = true;
                        return (AcceptanceReport)true;
                    }
                }
            }
            if( !td.defaultTrainable )
            {
                visible = false;
                return (AcceptanceReport)false;
            }
            if( (double)pawnKind.RaceProps.baseBodySize < (double)td.minBodySize )
            {
                visible = true;
                return new AcceptanceReport( "CannotTrainTooSmall".Translate( (object)pawnKind.LabelCap ) );
            }
            if( pawnKind.RaceProps.trainableIntelligence < td.requiredTrainableIntelligence )
            {
                visible = true;
                return new AcceptanceReport( "CannotTrainNotSmartEnough".Translate( (object)td.requiredTrainableIntelligence ) );
            }
            visible = true;
            return (AcceptanceReport)true;
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

            GUI.BeginGroup(rect);
            for (int i = 0; i < Training.Count; i++)
            {
                Rect cell = new Rect(i * width, 0f, width, rect.height);
                bool vis;
                AcceptanceReport report = CanBeTrained( _pawnKind, keys[i], out vis );
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
    }
}