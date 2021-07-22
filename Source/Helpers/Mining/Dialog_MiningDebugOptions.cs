// Dialog_MiningDebugOptions.cs
// Copyright Karel Kroeze, 2018-2020

using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class Dialog_MiningDebugOptions : Dialog_DebugOptionLister
    {
        private readonly ManagerJob_Mining job;

        public Dialog_MiningDebugOptions( ManagerJob_Mining job )
        {
            this.job = job;
        }

        protected override void DoListingItems()
        {
            DebugToolMap( "IsValidMiningTarget", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Mineable>() )
                    Messages.Message( job.IsValidMiningTarget( thing ).ToString(), MessageTypeDefOf.SilentInput );
            }, false );


            DebugToolMap( "IsValidDeconstructionTarget", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( job.IsValidDeconstructionTarget( thing ).ToString(),
                                      MessageTypeDefOf.SilentInput );
            }, false );

            DebugToolMap( "Faction", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( thing.Faction.ToStringSafe(), MessageTypeDefOf.SilentInput );
            }, false );

            DebugToolMap( "AllowedBuilding", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( job.AllowedBuilding( thing.def ).ToString(), MessageTypeDefOf.SilentInput );
            }, false);


            DebugToolMap( "AllowedMineral", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Mineable>() )
                    Messages.Message( job.AllowedMineral( thing.def ).ToString(), MessageTypeDefOf.SilentInput );
            }, false);


            DebugToolMap( "IsRelevantDeconstructionTarget", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( job.IsRelevantDeconstructionTarget( thing ).ToString(),
                                      MessageTypeDefOf.SilentInput );
            }, false);

            DebugToolMap( "IsRelevantMiningTarget", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Mineable>() )
                    Messages.Message( job.IsRelevantMiningTarget( thing ).ToString(), MessageTypeDefOf.SilentInput );
            }, false);

            DebugToolMap( "IsInAllowedArea", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( job.IsInAllowedArea( thing ).ToString(), MessageTypeDefOf.SilentInput );
            }, false);

            DebugToolMap( "IsReachable", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( job.IsReachable( thing ).ToString(), MessageTypeDefOf.SilentInput );
            }, false);

            DebugToolMap( "IsRoomDivider", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( job.IsARoomDivider( thing ).ToString(), MessageTypeDefOf.SilentInput );
            }, false);

            DebugToolMap( "IsRoofSupport: basic", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( job.IsARoofSupport_Basic( thing ).ToString(), MessageTypeDefOf.SilentInput );
            }, false); ;

            DebugToolMap( "IsRoofSupport: advanced", delegate
            {
                foreach ( var thing in Find.CurrentMap.thingGrid.ThingsAt( UI.MouseCell() ).OfType<Building>() )
                    Messages.Message( job.IsARoofSupport_Advanced( thing ).ToString(), MessageTypeDefOf.SilentInput );
            }, false);

            DebugAction( "DrawSupportGrid", delegate
            {
                foreach ( var cell in job.manager.map.AllCells )
                    if ( job.IsARoofSupport_Basic( cell ) )
                        job.manager.map.debugDrawer.FlashCell( cell, DebugSolidColorMats.MaterialOf( Color.green ) );
            }, false);

            DebugAction( "GetBaseCenter", delegate
            {
                var cell = Utilities.GetBaseCenter( job.manager );
                job.manager.map.debugDrawer.FlashCell( cell, DebugSolidColorMats.MaterialOf( Color.blue ) );
            }, false);

            DebugToolMap( "DrawPath", delegate
                {
                    var source = Utilities.GetBaseCenter( job.manager );
                    var target = UI.MouseCell();
                    var path = job.manager.map.pathFinder.FindPath( source, target,
                                                                    TraverseParms.For(
                                                                        TraverseMode.PassDoors, Danger.Some ) );
                    path.DrawPath( null );
                    path.ReleaseToPool();
                }, false
            );
        }
    }
}