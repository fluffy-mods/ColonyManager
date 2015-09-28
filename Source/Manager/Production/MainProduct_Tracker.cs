using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;

namespace FM
{
    public class MainProduct_Tracker
    {
        public MainProduct_Tracker(RecipeDef recipe)
        {
            this.recipe = recipe;
            Set();
        }

        private RecipeDef recipe = null;

        private ThingDef thingDef = null;

        private ThingCategoryDef categoryDef = null;

        public types type = types.Unknown;

        public enum types
        {
            Thing,
            Category,
            None,
            Unknown
        }

        public ThingCategoryDef CategoryDef
        {
            get
            {
                if (type == types.Thing || type == types.None || type == types.Unknown)
                {
                    return null;
                }

                return categoryDef;
            }
        }

        public ThingDef ThingDef
        {
            get
            {
                if (type == types.Category || type == types.None || type == types.Unknown)
                {
                    return null;
                }
                
                return thingDef;
            }
        }

        public void Clear()
        {
            type = types.Unknown;
            thingDef = null;
            categoryDef = null;
            label = null;
            count = -1;
        }

        public void Set()
        {
            try
            {
                // get the (main) product
                if (recipe.products != null && recipe.products.Count > 0 && recipe.products.First().thingDef.BaseMarketValue > 0)
                {
                    Clear();
                    thingDef = recipe.products.First().thingDef;
                    type = types.Thing;
                    count = recipe.products.First().count;
                    return;
                }

                // no main, is there a special?
                if (recipe.specialProducts == null)
                {
                    Clear();
                    type = types.None;
                    count = 0;
                    return;
                }
                else if (recipe.specialProducts.Count > 0)
                {
                    // get the first special product of the first thingdef allowed by the fixedFilter.
                    if (recipe.defaultIngredientFilter.AllowedThingDefs == null) throw new Exception("AllowedThingDefs NULL");
                    ThingDef allowedThingDef = recipe.fixedIngredientFilter.AllowedThingDefs.DefaultIfEmpty(null).FirstOrDefault();
                    if (allowedThingDef == null) throw new Exception("AllowedThingDef NULL");


                    if (recipe.specialProducts[0] == SpecialProductType.Butchery)
                    {
                        if (allowedThingDef.butcherProducts != null && allowedThingDef.butcherProducts.Count > 0)
                        {
                            // butcherproducts are defined, no problem. 
                            List<ThingCount> butcherProducts = allowedThingDef.butcherProducts;
                            if (butcherProducts.Count == 0) throw new Exception("No butcherproducts defined: " + allowedThingDef.defName);

                            Clear();
                            thingDef = butcherProducts.First().thingDef;
                            type = types.Thing;
                            count = butcherProducts.First().count;
                            return;
                        }
                        else
                        {
                            // still not defined, see if we can catch corpses.
                            if (allowedThingDef.defName.Contains("Corpse") && !allowedThingDef.defName.Contains("Mechanoid"))
                            {
                                // meat for non-mech corpses
                                Clear();
                                categoryDef = ThingCategoryDef.Named("MeatRaw");
                                type = types.Category;
                                count = 50;
                            }
                            else if (allowedThingDef.defName.Contains("Corpse") && allowedThingDef.defName.Contains("Mechanoid"))
                            {
                                // plasteel for mech corpses
                                Clear();
                                thingDef = ThingDef.Named("Plasteel");
                                type = types.Thing;
                                count = 20;
                            }

                            else
                            {
                                Clear();
                                return;
                            }
                        }
                    }

                    if (recipe.specialProducts[0] == SpecialProductType.Smelted)
                    {
                        if (allowedThingDef.smeltProducts == null)
                        {
                            Clear();
                            return;
                        }

                        List<ThingCount> smeltingProducts = allowedThingDef.smeltProducts;
                        if (smeltingProducts.Count == 0)
                        {
                            Clear();
                            return;
                        }


                        Clear();
                        thingDef = smeltingProducts.First().thingDef;
                        type = types.Thing;
                        count = smeltingProducts.First().count;
                        if (thingDef == null)
                        {
                            Clear();
                        }
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Log.Warning(e.Message);
#endif
                Clear();
            }
        }

        public string Label
        {
            get
            {
                if (label != null) return label;

                switch (type)
                {
                    case types.Thing:
                        label = thingDef.LabelCap;
                        break;
                    case types.Category:
                        label = categoryDef.LabelCap;
                        break;
                    case types.None:
                        label = "None";
                        break;
                    case types.Unknown:
                        label = "Unkown";
                        break;
                    default:
                        label = "Error";
                        break;
                }

                return label;
            }
        }

        private string label;

        /// <summary>
        /// Default max to trigger count slider.
        /// </summary>
        public int MaxUpperThreshold
        {
            get
            {
                // go from stacksize
                if (ThingDef != null)
                {
                    return Math.Max(ThingDef.stackLimit * 40, 100); // stacksize * 40, 100 min.
                }

                // if product is not resolved (stone blocks, weapon smelting, category)
                return 3000;
            }
        }

        private int count = -1;

        /// <summary>
        /// Number of output for product
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }
    }
}
