using System.Linq;
using RimWorld;
using Verse;

namespace FluffyManager
{
    class Alert_NoTable : Alert
    {
        public override AlertReport GetReport()
        {
            return Manager.For( Find.VisibleMap ).JobStack.FullStack().Count > 0 && !AnyManagerTable();
        }

        public override AlertPriority Priority => AlertPriority.Medium;
        
        private bool AnyManagerTable()
        {
            return Find.VisibleMap.listerBuildings.AllBuildingsColonistOfClass<Building_ManagerStation>().Any();
        }

        public Alert_NoTable()
        {
            defaultLabel = "FM.AlertNoTableLabel".Translate();
            defaultExplanation = "FM.AlertNoTable".Translate();
        }
    }

    class Alert_NoManager : Alert
    {
        public override AlertReport GetReport()
        {
            return Manager.For( Find.VisibleMap ).JobStack.FullStack().Count > 0 && !AnyConsciousManagerPawn();
        }

        public override AlertPriority Priority => AlertPriority.Medium;
        
        private bool AnyConsciousManagerPawn()
        {
            return
                Find.VisibleMap.mapPawns.FreeColonistsSpawned.Any(
                    pawn => !pawn.health.Dead && !pawn.Downed &&
                            pawn.workSettings.WorkIsActive( Utilities.WorkTypeDefOf_Managing ) ) ||
                Find.VisibleMap.listerBuildings.ColonistsHaveBuilding( DefDatabase<ThingDef>.GetNamed( "FM_AIManager" ) );
        }

        public Alert_NoManager()
        {
            defaultLabel = "FM.AlertNoManagerLabel".Translate();
            defaultExplanation = "FM.AlertNoManager".Translate();
        }
    }
}
