// Karel Kroeze
// Alerts.cs
// 2016-12-09

using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    internal class Alert_NoManager : Alert
    {
        #region Constructors

        public Alert_NoManager()
        {
            defaultLabel = "FM.AlertNoManagerLabel".Translate();
            defaultExplanation = "FM.AlertNoManager".Translate();
        }

        #endregion Constructors



        #region Properties

        public override AlertPriority Priority => AlertPriority.Medium;

        #endregion Properties



        #region Methods

        public override AlertReport GetReport()
        {
            return Manager.For( Find.VisibleMap ).JobStack.FullStack().Count > 0 && !AnyConsciousManagerPawn();
        }

        private bool AnyConsciousManagerPawn()
        {
            return
                Find.VisibleMap.mapPawns.FreeColonistsSpawned.Any(
                                                                  pawn => !pawn.health.Dead && !pawn.Downed &&
                                                                          pawn.workSettings.WorkIsActive(
                                                                                                         Utilities
                                                                                                             .WorkTypeDefOf_Managing ) ) ||
                Find.VisibleMap.listerBuildings.ColonistsHaveBuilding( DefDatabase<ThingDef>.GetNamed( "FM_AIManager" ) );
        }

        #endregion Methods
    }

    internal class Alert_NoTable : Alert
    {
        #region Constructors

        public Alert_NoTable()
        {
            defaultLabel = "FM.AlertNoTableLabel".Translate();
            defaultExplanation = "FM.AlertNoTable".Translate();
        }

        #endregion Constructors



        #region Properties

        public override AlertPriority Priority => AlertPriority.Medium;

        #endregion Properties



        #region Methods

        public override AlertReport GetReport()
        {
            return Manager.For( Find.VisibleMap ).JobStack.FullStack().Count > 0 && !AnyManagerTable();
        }

        private bool AnyManagerTable()
        {
            return Find.VisibleMap.listerBuildings.AllBuildingsColonistOfClass<Building_ManagerStation>().Any();
        }

        #endregion Methods
    }
}
