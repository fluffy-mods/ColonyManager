// Manager/ManagerJob_Ranching.cs
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

namespace FM
{
    public class ManagerJob_Ranching : ManagerJob
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
        public Dictionary<TrainableDef, bool>  TrainingTargets;
        public bool                            TryTameMore;
        private History                        _history;
        public override string                 Label                    => "FMR.Ranching".Translate();

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
            get { return Manager.Get.ManagerTabs.OfType<ManagerTab_Ranching>().First(); }
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

        public ManagerJob_Ranching()
        {
            // animals to empty empty list
            _animals = new List<Pawn>();

            // start history tracker
            _history = new History( AgeSexArray.Select( ageSex => ageSex.ToString() ).ToArray() );

            // set all target counts to 5
            CountTargets = AgeSexArray.ToDictionary( k => k, v => 5 );

            // set all training to false
            TrainingTargets = TrainableUtility.TrainableDefsInListOrder.ToDictionary( k => k, v => false );

            // set areas for restriction and taming to unrestricted
            TameArea = null;
            RestrictArea = null;

            // set defaults for boolean options
            TryTameMore = false;
            ButcherExcess = true;
            ButcherTrained = false;
        }

        public ManagerJob_Ranching( PawnKindDef pawnKindDef ) : this() // set defaults
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

        public void DrawTrainingSelector( Rect rect )
        {
            float width = rect.width / TrainingTargets.Count;
            List<TrainableDef> keys = TrainingTargets.Keys.ToList();

            GUI.BeginGroup(rect);
            for (int i = 0; i < TrainingTargets.Count; i++)
            {
                Rect cell = new Rect(i * width, 0f, width, rect.height);
                bool checkOn = TrainingTargets[keys[i]];
                Utilities.DrawToggle(cell, keys[i].LabelCap, ref checkOn, 16f, 0f, GameFont.Tiny);
                TrainingTargets[keys[i]] = checkOn;
            }
            GUI.EndGroup();
        }
    }
}