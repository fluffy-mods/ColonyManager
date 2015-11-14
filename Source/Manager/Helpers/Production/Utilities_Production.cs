// Manager/Utilities_Production.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:31

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FM
{
    public static class Utilities_Production
    {
        /// <summary>
        ///     Creates a shallow and barebone copy of a Bill_Production, or Bill_ProductionWithUft cast to Bill_Production if the
        ///     input is an Uft Bill.
        /// </summary>
        /// <param name="bill"></param>
        /// <returns></returns>
        public static Bill_Production Copy( this Bill_Production bill )
        {
            Bill_Production copy;
            copy = bill is Bill_ProductionWithUft
                ? new Bill_ProductionWithUft( bill.recipe )
                : new Bill_Production( bill.recipe );

            // copy relevant attributes, others are set by manager when assigning
            // uft specific things are irrelevant here, and set by core
            copy.ingredientFilter = bill.ingredientFilter;
            copy.ingredientSearchRadius = bill.ingredientSearchRadius;
            copy.storeMode = bill.storeMode;
            copy.minSkillLevel = bill.minSkillLevel;

            return copy;
        }

        /// <summary>
        ///     Amount per worker to satsify the bill.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public static int CountPerWorker( this ManagerJob_Production job, int i )
        {
            int n = job.BillGivers.CurBillGiverCount;
            int diff = Mathf.CeilToInt( job.Trigger.Count - Utilities.CountProducts( job.Trigger.ThresholdFilter ) );
            int bills = Mathf.CeilToInt( diff / job.MainProduct.Count );
            float naive = bills / (float)n;
            if ( bills % n > i )
            {
                return (int)Math.Ceiling( naive );
            }
            return (int)Math.Floor( naive );
        }

        /// <summary>
        ///     Get all currently build buildings that use the recipe
        /// </summary>
        /// <param name="rd"></param>
        /// <returns></returns>
        public static List< Building_WorkTable > GetCurrentRecipeUsers( this RecipeDef rd )
        {
            List< ThingDef > recipeUsers = rd.GetRecipeUsers();
            List< Building_WorkTable > currentRecipeUsers = new List< Building_WorkTable >();

            foreach ( ThingDef td in recipeUsers )
            {
                currentRecipeUsers.AddRange(
                    Find.ListerBuildings.AllBuildingsColonistOfDef( td ).Select( b => b as Building_WorkTable ) );
            }

            return currentRecipeUsers;
        }

        /// <summary>
        ///     Get the thingdefs for everything that can potentially be a billgiver for rd.
        /// </summary>
        /// <param name="rd"></param>
        /// <param name="includeNonBuilding"></param>
        /// <returns></returns>
        public static List< ThingDef > GetRecipeUsers( this RecipeDef rd, bool includeNonBuilding = false )
        {
            List< ThingDef > recipeUsers = new List< ThingDef >();

            // probably redundant starting point, get recipeusers as defined in the recipe.
            if ( rd.recipeUsers != null )
            {
                recipeUsers.AddRange( rd.recipeUsers );
            }

            // fetch thingdefs which have recipes, and the recipes include ours.
            recipeUsers.AddRange(
                DefDatabase< ThingDef >.AllDefsListForReading.Where( t => t.recipes != null && t.recipes.Contains( rd ) )
                                       .ToList() );
            if ( !includeNonBuilding )
            {
                recipeUsers = recipeUsers.Where( t => t.category == ThingCategory.Building ).ToList();
            }
            return recipeUsers.Distinct().ToList();
        }

        /// <summary>
        ///     Does the recipe have a building billgiver, and is it built?
        /// </summary>
        /// <param name="rd"></param>
        /// <param name="built"></param>
        /// <returns></returns>
        public static bool HasBuildingRecipeUser( this RecipeDef rd, bool built = false )
        {
            List< ThingDef > recipeUsers = GetRecipeUsers( rd );
            return
                recipeUsers.Any(
                    t =>
                        ( t.category == ThingCategory.Building ) &&
                        ( !built ||
                          Find.ListerThings.ThingsInGroup( ThingRequestGroup.PotentialBillGiver )
                              .Select( thing => thing.def )
                              .Contains( t ) ) );
        }
    }
}