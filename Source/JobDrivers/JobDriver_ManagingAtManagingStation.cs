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
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve( TargetIndex.A ).FailOnDespawnedOrForbiddenPlacedThings();
            yield return Toils_Goto.GotoThing( TargetIndex.A, PathEndMode.InteractionCell )
                                   .FailOnDespawnedOrForbiddenPlacedThings();
            yield return Manage( TargetIndex.A ).FailOnDespawnedOrForbiddenPlacedThings();
            yield return DoWork();
            yield return Toils_Reserve.Release( TargetIndex.A );
        }

        private Toil DoWork()
        {
            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.AddFinishAction( () => Manager.For( pawn.Map ).TryDoWork() );

            return toil;
        }

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
            toil.defaultDuration =
                (int)( comp.Props.Speed * ( 1 - pawn.GetStatValue( StatDef.Named( "ManagingSpeed" ) ) + .5 ) );
#if DEBUG_WORKGIVER
            Log.Message("Pawn stat: " + pawn.GetStatValue(StatDef.Named("ManagingSpeed")) + " (+0.5) Station speed: " + comp.Props.Speed + "Total time: " + toil.defaultDuration);
#endif
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.tickAction = () => toil.actor.skills.GetSkill( DefDatabase<SkillDef>.GetNamed( "Intellectual" ) ).Learn( 0.11f );
            return toil;
        }
    }
}
