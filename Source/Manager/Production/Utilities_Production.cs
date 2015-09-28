using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace FM
{
    public static class Utilities_Production
    {
        /// <summary>
        /// Get the thingdefs for everything that can potentially be a billgiver for rd.
        /// </summary>
        /// <param name="rd"></param>
        /// <param name="includeNonBuilding"></param>
        /// <returns></returns>
        public static List<ThingDef> getRecipeUsers(this RecipeDef rd, bool includeNonBuilding = false)
        {
            List<ThingDef> recipeUsers = new List<ThingDef>();
            // probably redundant starting point, get recipeusers as defined in the recipe.
            if (rd.recipeUsers != null) recipeUsers.AddRange(rd.recipeUsers);

            // fetch thingdefs which have recipes, and the recipes include ours.
            recipeUsers.AddRange(DefDatabase<ThingDef>.AllDefsListForReading.Where(t => t.recipes != null && t.recipes.Contains(rd)).ToList());
            if (!includeNonBuilding) recipeUsers.Where(t => t.category == ThingCategory.Building);
            return recipeUsers.Distinct().ToList();
        }

        /// <summary>
        /// Get all currently build buildings that use the recipe
        /// </summary>
        /// <param name="rd"></param>
        /// <returns></returns>
        public static List<Building_WorkTable> getCurrentRecipeUsers(this RecipeDef rd)
        {
            List<ThingDef> recipeUsers = rd.getRecipeUsers();
            List<Building_WorkTable> currentRecipeUsers = new List<Building_WorkTable>();

            for (int i = 0; i < recipeUsers.Count; i++)
            {
                currentRecipeUsers.AddRange(Find.ListerBuildings.AllBuildingsColonistOfDef(recipeUsers[i]).Select(b => b as Building_WorkTable));
            }

            return currentRecipeUsers;
        }

        /// <summary>
        /// Does the recipe have a building billgiver, and is it built?
        /// </summary>
        /// <param name="rd"></param>
        /// <param name="built"></param>
        /// <returns></returns>
        public static bool HasBuildingRecipeUser(this RecipeDef rd, bool built = false)
        {
            List<ThingDef> recipeUsers = getRecipeUsers(rd);
            return recipeUsers.Any(t => (t.category == ThingCategory.Building) && (!built || Find.ListerThings.ThingsInGroup(ThingRequestGroup.PotentialBillGiver).Select(thing => thing.def).Contains(t)));
        }
        
        /// <summary>
        /// Amount per worker to satsify the bill.
        /// </summary>
        /// <param name="bill"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public static int CountPerWorker(this ManagerJob_Production job, int i)
        {
            int n = job.billGivers.CurBillGiverCount;
            int diff = Mathf.CeilToInt(Math.Abs(job.trigger.count - Utilities.CountProducts(job.trigger.thresholdFilter) / job.mainProduct.Count));
            float naive = diff / n;
            if (diff % n > i)
            {
                return (int) Math.Ceiling(naive);
            }
            else
            {
                return (int)Math.Floor(naive);
            }
        }

        /// <summary>
        /// Creates a shallow and barebone copy of a Bill_Production, or Bill_ProductionWithUft cast to Bill_Production if the input is an Uft Bill.
        /// </summary>
        /// <param name="bill"></param>
        /// <returns></returns>
        public static Bill_Production Copy(this Bill_Production bill)
        {
            Bill_Production copy;
            if (bill as Bill_ProductionWithUft != null)
            {
                copy = new Bill_ProductionWithUft(bill.recipe);
            }
            else
            {
                copy = new Bill_Production(bill.recipe);
            }

            // copy relevant attributes, others are set by manager when assigning
            // uft specific things are irrelevant here, and set by core
            copy.ingredientFilter = bill.ingredientFilter;
            copy.ingredientSearchRadius = bill.ingredientSearchRadius;
            copy.storeMode = bill.storeMode;
            copy.minSkillLevel = bill.minSkillLevel;
            
            return copy;
        }
    }
}
