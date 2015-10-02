using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FM
{
    public enum AssignedBillGiverOptions
    {
        All,
        Count,
        Specific
    }

    public class BillGiverTracker : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.LookValue(ref BillGiverSelection, "BillGiverSelection");
            Scribe_Values.LookValue(ref UserBillGiverCount, "UserBillGiverCount");
            Scribe_References.LookReference(ref AreaRestriction, "AreaRestriction");
            Scribe_Collections.LookDictionary(ref AssignedBills, "AssignedBills", LookMode.MapReference, LookMode.MapReference);
            Scribe_Collections.LookList(ref SpecificBillGivers, "SpecificBillGivers", LookMode.MapReference);

            foreach (KeyValuePair<Bill_Production, Building_WorkTable> pair in AssignedBills)
            {
                Log.Message(pair.Key.LabelCap + " | " + pair.Value.LabelCap);
            }
        }

        public BillGiverTracker(ManagerJobProduction job)
        {
            _recipe = job.Bill.recipe;
            _job = job;
            AssignedBills = new Dictionary<Bill_Production, Building_WorkTable>();
            SpecificBillGivers = new List<Building_WorkTable>();
        }

        public BillGiverTracker(ManagerJobProduction job, Boolean thisIsAScribeConstructor)
        {
            _recipe = job.Bill.recipe;
            _job = job;
        }

        public RecipeDef Recipe => _recipe;

        /// <summary>
        /// Specific billgivers set by user
        /// </summary>
        public List<Building_WorkTable> SpecificBillGivers;

        /// <summary>
        /// Assignment mode for billgivers
        /// </summary>
        public AssignedBillGiverOptions BillGiverSelection = AssignedBillGiverOptions.All;

        /// <summary>
        /// All potential billgivers count
        /// </summary>
        public int AllBillGiverCount => GetBillGiverDefs.Count;

        public Area AreaRestriction = null;

        public Dictionary<Bill_Production, Building_WorkTable> AssignedBills;

        /// <summary>
        /// Currently allowed billgivers count (these do not necessarily actually have the bill)
        /// </summary>
        public int CurBillGiverCount => GetSelectedBillGivers.Count;

        /// <summary>
        /// User requested billgiver count, when using count assignment mode.
        /// </summary>
        public int UserBillGiverCount;

        private RecipeDef _recipe;

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
        public List<Building_WorkTable> GetSelectedBillGivers
        {
            get
            {
                List<Building_WorkTable> list = Recipe.GetCurrentRecipeUsers();

                switch (BillGiverSelection)
                {
                    case AssignedBillGiverOptions.Count:
                        if (AreaRestriction != null) list = list.Where(bw => AreaRestriction.ActiveCells.Contains(bw.Position)).ToList();
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

        public List<Building_WorkTable> GetAssignedBillGivers
        {
            get { return AssignedBills.Values.ToList(); }
        }

        public void DrawBillGiverConfig(ref Listing_Standard listing)
        {
            listing.DoGap();

            // workstation info
            listing.DoLabel("FMP.BillGivers".Translate());
            listing.DoLabel("FMP.BillGiversCount".Translate(GetPotentialBillGivers.Count, GetSelectedBillGivers.Count, GetAssignedBillGivers.Count));

            string potentialString = String.Join("\n", GetPotentialBillGivers.Select(b => b.LabelCap).ToArray());
            string assignedString = String.Join("\n", GetSelectedBillGivers.Select(b => b.LabelCap).ToArray());
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
