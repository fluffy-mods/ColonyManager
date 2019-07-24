// Karel Kroeze
// JobDriver_ManagingAtManagingStation.cs
// 2016-12-09

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FluffyManager
{
    internal class JobDriver_ManagingAtManagingStation : JobDriver
    {
        private float workDone;
        private float workNeeded;

        public override bool TryMakePreToilReservations( bool errorOnFailed )
        {
            return pawn.Reserve( job.targetA, job );
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden( TargetIndex.A );
            yield return Toils_Goto.GotoThing( TargetIndex.A, PathEndMode.InteractionCell );
            var manage = Manage( TargetIndex.A );
            yield return manage;

            // if made to by player, keep doing that untill we're out of jobs
            yield return Toils_Jump.JumpIf(
                manage, () => GetActor().CurJob.playerForced && Manager.For( Map ).JobStack.NextJob != null );
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look( ref workNeeded, "WorkNeeded", 100 );
            Scribe_Values.Look( ref workDone, "WorkDone" );
        }

        private Toil Manage( TargetIndex targetIndex )
        {
            var station = GetActor().jobs.curJob.GetTarget( targetIndex ).Thing as Building_ManagerStation;
            if ( station == null )
            {
                Log.Error( "Target of manager job was not a manager station." );
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
                workDone   = 0;
                workNeeded = comp.Props.speed;
            };

            toil.tickAction = () =>
            {
                // learn a bit
                pawn.skills.GetSkill( DefDatabase<SkillDef>.GetNamed( "Intellectual" ) )
                    .Learn( 0.11f );

                // update counter
                workDone += pawn.GetStatValue( StatDef.Named( "ManagingSpeed" ) );

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