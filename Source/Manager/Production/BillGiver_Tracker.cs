using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using UnityEngine;

namespace FM
{
    public enum assignedBillGiverOptions
    {
        all,
        count,
        specific
    }

    public class BillGiver_Tracker
    {
        public BillGiver_Tracker(RecipeDef recipe)
        {
            this.recipe = recipe;
        }

        public RecipeDef recipe;

        /// <summary>
        /// Specific billgivers set by user
        /// </summary>
        public List<Building_WorkTable> specificBillGivers = new List<Building_WorkTable>();

        /// <summary>
        /// Assignment mode for billgivers
        /// </summary>
        public assignedBillGiverOptions billGiverAssignment = assignedBillGiverOptions.all;

        /// <summary>
        /// All potential billgivers count
        /// </summary>
        public int AllBillGiverCount
        {
            get
            {
                return GetBillGiverDefs.Count;
            }
        }

        /// <summary>
        /// Currently allowed billgivers count (these do not necessarily actually have the bill)
        /// </summary>
        public int CurBillGiverCount
        {
            get
            {
                return GetAssignedBillGivers.Count;
            }
        }
        
        /// <summary>
        /// User requested billgiver count, when using count assignment mode.
        /// </summary>
        public int userBillGiverCount;

        /// <summary>
        /// All billgiver defs (by recipe).
        /// </summary>
        public List<ThingDef> GetBillGiverDefs
        {
            get
            {
                return recipe.getRecipeUsers();
            }
        }

       public List<Building_WorkTable> GetPotentialBillGivers
        {
            get
            {
                return recipe.getCurrentRecipeUsers();
            }
        }

        /// <summary>
        /// All currently assigned billgivers
        /// </summary>
        /// <returns></returns>
        public List<Building_WorkTable> GetAssignedBillGivers
        {
            get
            {
                List<Building_WorkTable> list = recipe.getCurrentRecipeUsers();

                switch (billGiverAssignment)
                {
                    case assignedBillGiverOptions.count:
                        list.Take(userBillGiverCount).ToList();
                        break;
                    case assignedBillGiverOptions.specific:
                        list = specificBillGivers;
                        break;
                    case assignedBillGiverOptions.all:
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

            string PotentialString = String.Join("\n", GetPotentialBillGivers.Select(b => b.LabelCap).ToArray());
            string AssignedString = String.Join("\n", GetAssignedBillGivers.Select(b => b.LabelCap).ToArray());
            string stationsTooltip = "FMP.BillGiversTooltip".Translate(PotentialString, AssignedString);
            // todo, fix that tooltip. 
            // TooltipHandler.TipRegion(stations, stationsTooltip);

            // workstation selector
            if (listing.DoTextButton("FMP.BillGiversDetails".Translate()))
            {
                Find.WindowStack.Add(DetailsWindow);
            }
        }

        public Window_BillGiverDetails DetailsWindow
        {
            get
            {
                Window_BillGiverDetails window = new Window_BillGiverDetails();
                window.billGivers = this;
                window.closeOnClickedOutside = true;
                window.draggable = true;
                return window;
            }
        }
    }
}
