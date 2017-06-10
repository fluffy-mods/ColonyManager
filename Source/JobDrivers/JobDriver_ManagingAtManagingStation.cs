// Karel Kroeze
// JobDriver_ManagingAtManagingStation.cs
// 2016-12-09

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FluffyManager
{
    internal class JobDriver_ManagingAtManagingStation : JobDriver
    {
        private float workNeeded = 0;
        private float workDone = 0;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve( TargetIndex.A ).FailOnDespawnedOrForbiddenPlacedThings();
            yield return Toils_Goto.GotoThing( TargetIndex.A, PathEndMode.InteractionCell )
                                   .FailOnDespawnedOrForbiddenPlacedThings();
            yield return Manage( TargetIndex.A ).FailOnDespawnedOrForbiddenPlacedThings();
            yield return Toils_Reserve.Release( TargetIndex.A );
        }

        #region Overrides of JobDriver

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look( ref workNeeded, "WorkNeeded", 100 );
            Scribe_Values.Look( ref workDone, "WorkDone", 0 );
        }

        #endregion

        private Toil Manage( TargetIndex targetIndex )
        {
            var station = CurJob.GetTarget( targetIndex ).Thing as Building_ManagerStation;
            if ( station == null )
            {
                Log.Error( "Target of manager job was not a manager station. This should never happen." );
                return null;
            }

            var comp = station.GetComp<Comp_ManagerStation>();
            if ( comp == null )
            {
                Log.Error( "Target of manager job does not have manager station comp. This should never happen." );
                return null;
            }

            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = () =>
                                  {
                                      workDone = 0;
                                      workNeeded = (int)
                                          ( comp.Props.Speed *
                                            ( 1 - pawn.GetStatValue( StatDef.Named( "ManagingSpeed" ) ) + .5 ) );
                                  };

            toil.tickAction = () =>
                                  {
                                      // learn a bit
                                      pawn.skills.GetSkill( DefDatabase<SkillDef>.GetNamed( "Intellectual" ) )
                                                 .Learn( 0.11f );

                                      // update counter
                                      workDone++;

                                      // are we done yet?
                                      if ( workDone > workNeeded )
                                      {
                                          Manager.For( pawn.Map ).TryDoWork();
                                          ReadyForNextToil();
                                      }
                                  };

            toil.WithProgressBar( TargetIndex.A, () => workDone / workNeeded );
            return toil;
        }
    }
}
