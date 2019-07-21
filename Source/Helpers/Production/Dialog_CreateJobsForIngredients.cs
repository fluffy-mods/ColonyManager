//// Karel Kroeze
//// Dialog_CreateJobsForIngredients.cs
//// 2016-12-09

//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using Verse;

//namespace FluffyManager
//{
//    public class Dialog_CreateJobsForIngredients : Window
//    {
//        #region Fields

//        // ingredients are set by the recipe, and are given by a thingfilter and a count.
//        // we create an ingredientSelector for each ingredient, which allows selecting an ingredient from the filter. (or sets it, if there's only one thingdef allowed).
//        public static List<IngredientSelector> ingredients;

//        public Manager manager;
//        public int targetCount;
//        public RecipeDef targetRecipe;
//        private static Vector2 _countFieldSize = new Vector2( 100f, 30f );

//        // UI settings.
//        private static float _entryHeight = 30f;

//        private static float _nestingOffset = 15f;
//        private float _finalListHeight = 9999f;
//        private Vector2 _scrollPosition = Vector2.zero;

//        #endregion Fields

//        #region Constructors

//        public Dialog_CreateJobsForIngredients( Manager manager, RecipeDef recipe, int count )
//        {
//            targetCount = count;
//            targetRecipe = recipe;
//            ingredients =
//                recipe.ingredients.Select( ic => new IngredientSelector( manager, ic, targetCount, recipe ) ).ToList();
//            this.manager = manager;
//        }

//        #endregion Constructors


//        #region Methods

//        /// <summary>
//        /// Returns true if the recipe has prerequisite ingredients, and those ingredients can currently be crafted.
//        /// </summary>
//        /// <param name="recipe"></param>
//        /// <returns></returns>
//        public static bool HasPrerequisiteChoices( Map map, RecipeDef recipe )
//        {
//            return
//                recipe.ingredients.Select( ing => new IngredientSelector( Manager.For( map ), ing, 1, recipe ) )
//                      .Any( ins => IngredientSelector.HasRecipeChoices( map, ins ) );
//        }

//        public override void DoWindowContents( Rect inRect )
//        {
//            // set up rects
//            var titleRect = new Rect( inRect.xMin, inRect.yMin, inRect.width, Utilities.TitleHeight );
//            var listRect = new Rect( inRect.xMin, titleRect.yMax, inRect.width,
//                                     inRect.height - Utilities.TitleHeight - Utilities.BottomButtonHeight );
//            var buttonRect = new Rect( inRect.xMax - 200f, listRect.yMax + Utilities.Margin, 200f,
//                                       Utilities.BottomButtonHeight - Utilities.Margin );

//            // title
//            Utilities.Label( titleRect, "FMP.IngredientDialogTitle".Translate(), null, TextAnchor.MiddleCenter, 0f, 0f,
//                             GameFont.Medium );

//            // start recursive list of ingredients
//            Rect viewRect = listRect.AtZero();
//            viewRect.height = _finalListHeight;
//            if ( _finalListHeight > listRect.height )
//            {
//                viewRect.width -= 20f; // scrollbar
//            }

//            Widgets.DrawMenuSection( listRect );
//            Widgets.BeginScrollView( listRect, ref _scrollPosition, viewRect );
//            GUI.BeginGroup( viewRect );
//            Vector2 cur = Vector2.zero;
//            foreach ( IngredientSelector ingredient in ingredients )
//            {
//                // each selector row draws it's own children recursively.
//                ingredient.DrawSelectorRow( ref cur, inRect.width, 0, Vector2.zero );
//            }

//            GUI.EndGroup();
//            Widgets.EndScrollView();
//            _finalListHeight = cur.y + _entryHeight;

//            // final button
//            if ( Widgets.ButtonText( buttonRect, "FMP.AddIngredientBills".Translate() ) )
//            {
//                foreach ( IngredientSelector ingredient in ingredients )
//                {
//                    ingredient.AddBills();
//                }

//                // we've probably added some bills, so refresh the tab.
//                manager.Tabs.OfType<ManagerTab_Production>().FirstOrDefault()?.Refresh();

//                // close this window.
//                Close();
//            }
//        }

//        #endregion Methods


//        #region Classes

//        public class IngredientSelector
//        {
//            #region Fields

//            public List<ThingDef> allowedThingDefs;

//            // the parent recipe itself.
//            // the manager instance we're doing this for.
//            // the list of thingdefs allowed by the ingredient
//            public IngredientCount ingredient;

//            // the vanilla ingredientcount of the parent recipe (filter + count)
//            public Manager manager;

//            // the ingredient selector itself is an intermediate step, it links the target recipe with (one of multiple possible) prerequisite recipes.
//            public RecipeSelector recipeSelector;

//            public int targetCount;

//            // the number of ingredients required * sqrt(number of parent recipe crafts)
//            public RecipeDef targetRecipe;

//            #endregion Fields

//            #region Constructors

//            public IngredientSelector( Manager manager, IngredientCount ingredient, int count, RecipeDef targetRecipe )
//            {
//                // set up vars
//                this.ingredient = ingredient;
//                this.targetRecipe = targetRecipe;
//                this.manager = manager;
//                targetCount = (int)Math.Sqrt( count ) * (int)ingredient.GetBaseCount();
//                allowedThingDefs = ingredient.filter.AllowedThingDefs.ToList();

//                // if there's only one allowed we don't need to manually choose.
//                if ( allowedThingDefs.Count == 1 )
//                {
//                    recipeSelector = new RecipeSelector( manager, allowedThingDefs.First(), targetCount );
//                }
//            }

//            #endregion Constructors


//            #region Methods

//            public static bool HasRecipeChoices( Map map, IngredientSelector ingredient )
//            {
//                return ingredient.allowedThingDefs.Any( td => RecipeSelector.HasRecipe( map, td ) );
//            }

//            public void AddBills()
//            {
//                // only proceed if we selected an ingredient/thingdef (recipeSelector != null), and there is a recipe selected.
//                if ( recipeSelector?.selectedRecipe == null )
//                {
//                    return;
//                }

//                // try to get a job with our recipe
//                RecipeDef curRecipe = recipeSelector.selectedRecipe;
//                ManagerJob_Production curJob = manager.JobStack.FullStack<ManagerJob_Production>()
//                                                      .FirstOrDefault( job => job.Bill.recipe == curRecipe );

//                // if there is a job for the recipe, add our job's count - any settings beyond that are user responsibility.
//                if ( curJob != null && curJob.Trigger.Count < targetCount )
//                {
//                    curJob.Trigger.Count = targetCount;
//                    Messages.Message( "FMP.IncreasedThreshold".Translate( curRecipe.LabelCap, targetCount ),
//                                      MessageSound.Benefit );
//                }
//                // otherwise create a new job.
//                else
//                {
//                    curJob = new ManagerJob_Production( manager, curRecipe );
//                    // make sure the trigger is valid (everything else is user responsibility).
//                    if ( curJob.Trigger.IsValid )
//                    {
//                        curJob.Managed = true;
//                        manager.JobStack.Add( curJob );
//                        Messages.Message( "FMP.AddedJob".Translate( curRecipe.LabelCap ), MessageSound.Benefit );
//                    }
//                    else
//                    {
//                        Messages.Message( "FMP.CouldNotAddJob".Translate( curRecipe.LabelCap ), MessageSound.RejectInput );
//                    }
//                }

//                // finally, call this method on all of our children
//                foreach ( IngredientSelector child in recipeSelector.children )
//                {
//                    child.AddBills();
//                }
//            }

//            public void DrawSelectorRow( ref Vector2 cur, float width, int nesting, Vector2 parentPosition )
//            {
//                // draw a label / dropdown for the thingdef to use in this ingredient slot.
//                // once selected, draw a label / dropdown for the recipe to use for that thingdef.
//                // finally, a textbox for the target count of that thing ( with a sensible default ).
//                cur.x = nesting * _nestingOffset;
//                float colWidth = ( width - _countFieldSize.x ) / 2;

//                var thingRect = new Rect( cur.x, cur.y, colWidth - cur.x, _entryHeight );
//                var recipeRect = new Rect( thingRect.xMax, cur.y, colWidth, _entryHeight );
//                var countRect = new Rect( width - _countFieldSize.x, cur.y, _countFieldSize.x, _countFieldSize.y );
//                cur.y += _entryHeight;

//                // Draw line from parent to here.
//                if ( parentPosition != Vector2.zero )
//                {
//                    // vertical line segment
//                    Widgets.DrawLineVertical( parentPosition.x + Utilities.Margin, parentPosition.y,
//                                              cur.y - parentPosition.y - _entryHeight / 2 );
//                    // horizontal line segment
//                    Widgets.DrawLineHorizontal( parentPosition.x + Utilities.Margin, cur.y - _entryHeight / 2,
//                                                cur.x - parentPosition.x - Utilities.Margin );
//                }

//                // THINGDEF SELECTOR
//                // draw the label
//                string label = recipeSelector?.target.LabelCap ?? "FMP.SelectIngredient".Translate();
//                Utilities.Label( thingRect, label,
//                                 "FMP.SelectIngredientTooltip".Translate( targetRecipe.LabelCap,
//                                                                          ingredient.GetBaseCount() ),
//                                 TextAnchor.MiddleLeft,
//                                 Utilities.Margin );

//                // if there are choices do a dropdown on click
//                if ( allowedThingDefs.Count > 1 )
//                {
//                    Widgets.DrawHighlightIfMouseover( thingRect );
//                    if ( Widgets.ButtonInvisible( thingRect ) )
//                    {
//                        List<FloatMenuOption> options = allowedThingDefs
//                            .Where( td => RecipeSelector.HasRecipe( manager, td ) )
//                            .Select(
//                                    td =>
//                                    new FloatMenuOption( td.LabelCap,
//                                                         delegate
//                                                             {
//                                                                 recipeSelector = new RecipeSelector( manager, td,
//                                                                                                      ingredient
//                                                                                                          .CountRequiredOfFor
//                                                                                                          (
//                                                                                                           td,
//                                                                                                           targetRecipe ) *
//                                                                                                      (int)
//                                                                                                      Math.Sqrt(
//                                                                                                                targetCount ) );
//                                                             } ) ).ToList();
//                        if ( allowedThingDefs.Any( td => !RecipeSelector.HasRecipe( manager, td ) ) )
//                        {
//                            options.Add( new FloatMenuOption( "FMP.RawResource".Translate(),
//                                                              delegate
//                                                              { recipeSelector = null; } ) );
//                        }
//                        Find.WindowStack.Add( new FloatMenu( options ) );
//                    }
//                }

//                // RECIPE SELECTOR
//                recipeSelector?.DrawRecipeSelector( recipeRect );

//                // COUNT FIELD
//                if ( recipeSelector?.selectedRecipe != null )
//                {
//                    recipeSelector?.DrawCountField( countRect );
//                }

//                // DRAW YOUR CHILDREN
//                if ( recipeSelector != null &&
//                     recipeSelector.selectedRecipe != null &&
//                     recipeSelector.children != null )
//                {
//                    // For some reason just plain copying cur (or even the elements of cur) doesn't work (I'm quite possibly misunderstanding how this works)
//                    float x = cur.x;
//                    float y = cur.y;
//                    var pos = new Vector2( x, y );
//                    foreach ( IngredientSelector child in recipeSelector.children )
//                    {
//                        child.DrawSelectorRow( ref cur, width, nesting + 1, pos );
//                    }
//                }
//            }

//            #endregion Methods
//        }

//        public class RecipeSelector
//        {
//            #region Fields

//            public List<IngredientSelector> children;

//            public Manager manager;

//            public string newCount;

//            public int outCount;

//            public List<RecipeDef> recipes;

//            public RecipeDef selectedRecipe;

//            public ThingDef target;

//            public int targetCount;

//            #endregion Fields

//            #region Constructors

//            public RecipeSelector( Manager manager, ThingDef thingDef, int count )
//            {
//                target = thingDef;
//                targetCount = count;
//                this.manager = manager;
//                newCount = count.ToString();

//                recipes = GetRecipesFor( manager, thingDef );
//            }

//            #endregion Constructors


//            #region Methods

//            public static List<RecipeDef> GetRecipesFor( Map map, ThingDef td, bool currentlyAvailable = true )
//            {
//                return DefDatabase<RecipeDef>.AllDefsListForReading
//                                             .Where(
//                                                    rd =>
//                                                    rd.products.Any( tc => tc.thingDef == td ) &&
//                                                    rd.CurrentRecipeUsers( map ).Count > 0 )
//                                             .ToList();
//            }

//            public static bool HasRecipe( Map map, ThingDef thingDef )
//            {
//                return GetRecipesFor( map, thingDef ).Count > 0;
//            }

//            public void DrawCountField( Rect rect )
//            {
//                if ( !int.TryParse( newCount, out outCount ) )
//                {
//                    GUI.color = Color.red;
//                }
//                newCount = Widgets.TextField( rect, newCount );
//                GUI.color = Color.white;
//            }

//            public void DrawRecipeSelector( Rect rect )
//            {
//                // draw the label
//                string label;
//                string tooltip;
//                if ( recipes.Count == 0 ) // raw resource / no recipe
//                {
//                    label = "FMP.RawResource".Translate();
//                    tooltip = "FMP.RawResourceTooltip".Translate( target.LabelCap );
//                }
//                else
//                {
//                    label = selectedRecipe?.LabelCap ?? "FMP.SelectRecipe".Translate();
//                    tooltip = "FMP.SelectRecipeTooltip".Translate( target.LabelCap );
//                    if ( selectedRecipe != null )
//                    {
//                        tooltip += "FMP.SelectRecipeTooltipSelected".Translate();
//                        foreach ( IngredientCount ingredient in selectedRecipe.ingredients )
//                        {
//                            tooltip += "FMP.IngredientCount".Translate( ingredient.filter.Summary,
//                                                                        ingredient.GetBaseCount() );
//                        }
//                    }
//                }

//                Utilities.Label( rect, label, tooltip, TextAnchor.MiddleLeft,
//                                 Utilities.Margin );

//                // if there are choices do a dropdown on click
//                if ( recipes.Count > 0 )
//                {
//                    Widgets.DrawHighlightIfMouseover( rect );
//                    if ( Widgets.ButtonInvisible( rect ) )
//                    {
//                        List<FloatMenuOption> options = recipes
//                            .Select(
//                                    rd =>
//                                    new FloatMenuOption(
//                                        rd.LabelCap + " (" +
//                                        string.Join( ", ", rd.GetRecipeUsers().Select( td => td.LabelCap ).ToArray() ) +
//                                        ")",
//                                        delegate
//                                        { SelectRecipe( rd ); } ) ).ToList();
//                        options.Add( new FloatMenuOption( "FMP.DoNotUseRecipe".Translate(),
//                                                          delegate
//                                                          { SelectRecipe( null ); } ) );
//                        Find.WindowStack.Add( new FloatMenu( options ) );
//                    }
//                }
//            }

//            public void SelectRecipe( RecipeDef recipe )
//            {
//                selectedRecipe = recipe;
//                newCount = targetCount.ToString();
//                children =
//                    recipe?.ingredients.Select( ic => new IngredientSelector( manager, ic, targetCount, recipe ) )
//                           .ToList();
//            }

//            #endregion Methods
//        }

//        #endregion Classes
//    }
//}

