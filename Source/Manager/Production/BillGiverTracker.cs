using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FM
{
    public enum AssignedBillGiverOptions
    {
        All,
        Count,
        Specific
    }

    public class BillGiverTracker
    {
        public BillGiverTracker(ManagerJobProduction job)
        {
            _recipe = job.Bill.recipe;
            _job = job;
        }

        public RecipeDef Recipe => _recipe;

        /// <summary>
        /// Specific billgivers set by user
        /// </summary>
        public List<Building_WorkTable> SpecificBillGivers = new List<Building_WorkTable>();

        /// <summary>
        /// Assignment mode for billgivers
        /// </summary>
        public AssignedBillGiverOptions BillGiverAssignment = AssignedBillGiverOptions.All;

        /// <summary>
        /// All potential billgivers count
        /// </summary>
        public int AllBillGiverCount => GetBillGiverDefs.Count;

        /// <summary>
        /// Currently allowed billgivers count (these do not necessarily actually have the bill)
        /// </summary>
        public int CurBillGiverCount => GetAssignedBillGivers.Count;

        /// <summary>
        /// User requested billgiver count, when using count assignment mode.
        /// </summary>
        public int UserBillGiverCount;

        private readonly RecipeDef _recipe;
        private ManagerJobProduction _job;

        /// <summary>
        /// All billgiver defs (by recipe).
        /// </summary>
        public List<ThingDef> GetBillGiverDefs => Recipe.GetRecipeUsers();

        public List<Building_WorkTable> GetPotentialBillGivers => Recipe.GetCurrentRecipeUsers();

        /// <summary>
        /// All currently assigned billgivers
        /// </summary>
        /// <returns></returns>
        public List<Building_WorkTable> GetAssignedBillGivers
        {
            get
            {
                List<Building_WorkTable> list = Recipe.GetCurrentRecipeUsers();

                switch (BillGiverAssignment)
                {
                    case AssignedBillGiverOptions.Count:
                        if (_job.AreaRestriction != null) list = list.Where(bw => _job.AreaRestriction.ActiveCells.Contains(bw.Position)).ToList();
                        list = list.Take(UserBillGiverCount).ToList();
                        break;
                    case AssignedBillGiverOptions.Specific:
                        list = SpecificBillGivers;
                        break;
                    case AssignedBillGiverOptions.All:
                    default:
                        break;
                }

                return list;
            }
        }

        public void DrawBillGiverConfig(ref Listing_Standard listing)
        {
            listing.DoGap();

            // workstation info
            listing.DoLabel("FMP.BillGivers".Translate());
            listing.DoLabel("FMP.BillGiversCount".Translate(GetPotentialBillGivers.Count, GetAssignedBillGivers.Count));

            string potentialString = String.Join("\n", GetPotentialBillGivers.Select(b => b.LabelCap).ToArray());
            string assignedString = String.Join("\n", GetAssignedBillGivers.Select(b => b.LabelCap).ToArray());
            string stationsTooltip = "FMP.BillGiversTooltip".Translate(potentialString, assignedString);
            // todo, fix that tooltip. Possible?
            // TooltipHandler.TipRegion(stations, stationsTooltip);

            // workstation selector
            if (listing.DoTextButton("FMP.BillGiversDetails".Translate()))
            {
                Find.WindowStack.Add(DetailsWindow);
            }
        }

        public WindowBillGiverDetails DetailsWindow
        {
            get
            {
                WindowBillGiverDetails window = new WindowBillGiverDetails
                {
                    Job = _job,
                    closeOnClickedOutside = true,
                    draggable = true
                };
                return window;
            }
        }
    }
}
