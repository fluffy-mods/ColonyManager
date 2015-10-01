using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace FM
{
    public class ManagerTabProduction : ManagerTab
    {
        public ManagerTabProduction()
        {
            RefreshSourceList();
        }

        public override string Label { get; } = "FMP.Production".Translate();

        public float LeftRowSize = 300f;

        public enum SourceOptions
        {
            All,
            Available,
            Current
        }

        public static SourceOptions Source = SourceOptions.All;

        public static Vector2 LeftRowScrollPosition = new Vector2(0f, 0f);

        public static Vector2 BillScrollPosition = new Vector2(0f, 0f);

        public static List<ManagerJobProduction> SourceList;

        public static string SourceFilter = "";

        public static float SourceListHeight;

        public static void RefreshSourceList()
        {
            SourceList = new List<ManagerJobProduction>();

            switch (Source)
            {
                case SourceOptions.Available:
                    SourceList = (from rd in DefDatabase<RecipeDef>.AllDefsListForReading
                                  where rd.HasBuildingRecipeUser(true)
                                  select (new ManagerJobProduction(rd))).ToList();
                                  // select (rd.UsesUnfinishedThing ? new Bill_Managed(rd) : (Bill_Managed) new Bill_ManagedWithUft(rd))).ToList();
                    break;
                case SourceOptions.Current:
                    SourceList = Manager.JobStack.FullStack.OfType<ManagerJobProduction>().ToList();
                    break;
                case SourceOptions.All:
                    SourceList = (from rd in DefDatabase<RecipeDef>.AllDefsListForReading
                                  where rd.HasBuildingRecipeUser()
                                  select (new ManagerJobProduction(rd))).ToList();
                    // select (rd.UsesUnfinishedThing ? new Bill_Managed(rd) : (Bill_Managed) new Bill_ManagedWithUft(rd))).ToList();
                    break;
            }
        }

        public static ManagerJobProduction Job;
        private bool _postOpenFocus;

        public override void DoWindowContents(Rect canvas)
        {
            Rect leftRow = new Rect(0f, 35f, LeftRowSize, canvas.height - 35f);
            Rect contentCanvas = new Rect(leftRow.xMax + 10f, 5f, canvas.width - leftRow.width - 10f, canvas.height - 5f);
            
            DoLeftRow(leftRow);
            DoContent(contentCanvas);
        }

        public void DoContent(Rect canvas)
        {
            Widgets.DrawMenuSection(canvas);

            if (Job != null)
            {
                // leave some space for bottom buttons.
                float bottomButtonsHeight = 30f;
                float bottomButtonsGap = 6f;
                canvas.height = canvas.height - bottomButtonsHeight - bottomButtonsGap;

                // bottom buttons
                Rect bottomButtons = new Rect(canvas.xMin, canvas.height + bottomButtonsGap, canvas.width, bottomButtonsHeight);
                GUI.BeginGroup(bottomButtons);

                // add / remove to the stack
                Rect add = new Rect(bottomButtons.width * .75f, 0f, bottomButtons.width / 4f - 6f, bottomButtons.height);
                if (Source == SourceOptions.Current)
                {
                    if (Widgets.TextButton(add, "FM.Delete".Translate()))
                    {
                        Manager.JobStack.Delete(Job);
                        Job = null;
                        RefreshSourceList();
                        return; // hopefully that'll just skip to the next tick without any further errors?
                    }
                    TooltipHandler.TipRegion(add, "FMP.DeleteBillTooltip".Translate());
                }
                else
                {
                    if (Job.Trigger.IsValid)
                    {
                        if (Widgets.TextButton(add, "FM.Manage".Translate()))
                        {
                            Manager.JobStack.Add(Job);

                            Source = SourceOptions.Current;
                            RefreshSourceList();
                            SourceFilter = "";
                        }
                        TooltipHandler.TipRegion(add, "FMP.ManageBillTooltip".Translate());
                    } else
                    {
                        TextAnchor oldAnchor = Text.Anchor;
                        Color oldColor = GUI.color;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        GUI.color = new Color(.6f, .6f, .6f);
                        Widgets.DrawBox(add);
                        GUI.Label(add, "FMP.NoThreshold".Translate());
                        Text.Anchor = oldAnchor;
                        GUI.color = oldColor;
                        TooltipHandler.TipRegion(add, "FMP.NoThresholdTooltip".Translate());
                    }
                }

                GUI.EndGroup();

                GUI.BeginGroup(canvas);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Rect recta = new Rect(0f, 0f, canvas.width, 50f);
                Widgets.Label(recta, Job.Bill.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                Rect rect2 = new Rect(6f, 50f, canvas.width * .3f, canvas.height - 50f);
                Listing_Standard listingStandard = new Listing_Standard(rect2);
                if (Job.Bill.suspended)
                {
                    if (listingStandard.DoTextButton("Suspended".Translate()))
                    {
                        Job.Bill.suspended = false;
                    }
                }
                else if (listingStandard.DoTextButton("NotSuspended".Translate()))
                {
                    Job.Bill.suspended = true;
                }
                string billStoreModeLabel = ("BillStoreMode_" + Job.Bill.storeMode).Translate();
                if (listingStandard.DoTextButton(billStoreModeLabel))
                {
                    List<FloatMenuOption> list = (from BillStoreMode mode in Enum.GetValues(typeof (BillStoreMode)) select new FloatMenuOption(("BillStoreMode_" + mode).Translate(), delegate { Job.Bill.storeMode = mode; })).ToList();
                    Find.WindowStack.Add(new FloatMenu(list));
                }

                // other stuff
                listingStandard.DoGap();
                listingStandard.DoLabel("IngredientSearchRadius".Translate() + ": " + Job.Bill.ingredientSearchRadius.ToString("#####0"));
                Job.Bill.ingredientSearchRadius = Mathf.RoundToInt(listingStandard.DoSlider(Job.Bill.ingredientSearchRadius, 0f, 250f));

                if (Job.Bill.recipe.workSkill != null)
                {
                    listingStandard.DoLabel("MinimumSkillLevel".Translate(Job.Bill.recipe.workSkill.label.ToLower()) + ": " + Job.Bill.minSkillLevel.ToString("#####0"));
                    Job.Bill.minSkillLevel = Mathf.RoundToInt(listingStandard.DoSlider(Job.Bill.minSkillLevel, 0f, 20f));
                }

                // draw threshold config
                Job.Trigger.DrawThresholdConfig(ref listingStandard);
                Job.BillGivers.DrawBillGiverConfig(ref listingStandard);
                listingStandard.End();

                // ingredient picker
                Rect rect3 = new Rect(rect2.xMax + 6f, 50f, canvas.width * .4f, canvas.height - 50f);
                ThingFilterUI.DoThingFilterConfigWindow(rect3, ref BillScrollPosition, Job.Bill.ingredientFilter, Job.Bill.recipe.fixedIngredientFilter, 4);

                // description
                Rect rect4 = new Rect(rect3.xMax + 6f, rect3.y + 30f, canvas.width - rect3.xMax - 12f, canvas.height - 50f);
                StringBuilder stringBuilder = new StringBuilder();

                // add mainproduct line
                stringBuilder.AppendLine("FMP.MainProduct".Translate(Job.MainProduct.Label, Job.MainProduct.Count));
                stringBuilder.AppendLine();

                if (Job.Bill.recipe.description != null)
                {
                    stringBuilder.AppendLine(Job.Bill.recipe.description);
                    stringBuilder.AppendLine();
                }
                stringBuilder.AppendLine("WorkAmount".Translate() + ": " + Job.Bill.recipe.WorkAmountTotal(null).ToStringWorkAmount());
                stringBuilder.AppendLine();
                foreach (IngredientCount ingredientCount in Job.Bill.recipe.ingredients)
                {
                    if (!ingredientCount.filter.Summary.NullOrEmpty())
                    {
                        stringBuilder.AppendLine(Job.Bill.recipe.IngredientValueGetter.BillRequirementsDescription(ingredientCount));
                    }
                }
                stringBuilder.AppendLine();
                string text4 = Job.Bill.recipe.IngredientValueGetter.ExtraDescriptionLine();
                if (text4 != null)
                {
                    stringBuilder.AppendLine(text4);
                    stringBuilder.AppendLine();
                }
                stringBuilder.AppendLine("MinimumSkills".Translate());
                stringBuilder.AppendLine(Job.Bill.recipe.MinSkillString);
                Text.Font = GameFont.Small;
                string text5 = stringBuilder.ToString();
                if (Text.CalcHeight(text5, rect4.width) > rect4.height)
                {
                    Text.Font = GameFont.Tiny;
                }
                Widgets.Label(rect4, text5);
                Text.Font = GameFont.Small;
                if (Job.Bill.recipe.products.Count == 1)
                {
                    Widgets.InfoCardButton(rect4.x, rect3.y, Job.Bill.recipe.products[0].thingDef);
                }
            }
            GUI.EndGroup();

        }

        public override void PostOpen()
        {
            // focus on the filter on open, flag is checked after the field is actually drawn.
            _postOpenFocus = false;
        }

        public void DoLeftRow(Rect canvas)
        {
            Widgets.DrawMenuSection(canvas, false);

            // filter
            Rect filterRect = new Rect(10f, canvas.yMin + 5f, canvas.width - 50f, 30f);

            GUI.SetNextControlName("filterTextfield");
            SourceFilter = Widgets.TextField(filterRect, SourceFilter);

            if (!_postOpenFocus)
            {
                GUI.FocusControl("filterTextfield");
                _postOpenFocus = true;
            }

            if (SourceFilter != "")
            {
                Rect clearFilter = new Rect(filterRect.width + 10f, filterRect.yMin, 30f, 30f);
                if (Widgets.ImageButton(clearFilter, Widgets.CheckboxOffTex))
                {
                    SourceFilter = "";
                }
                TooltipHandler.TipRegion(clearFilter, "FMP.ClearFilterDesc".Translate());
            }
            TooltipHandler.TipRegion(filterRect, "FMP.FilterDesc".Translate());

            // tabs
            List<TabRecord> list = new List<TabRecord>();
            TabRecord item = new TabRecord("FMP.All".Translate(), delegate
            {
                Source = SourceOptions.All;
                RefreshSourceList();
            }, Source == SourceOptions.All);
            list.Add(item);
            TabRecord item2 = new TabRecord("FMP.Available".Translate(), delegate
            {
                Source = SourceOptions.Available;
                RefreshSourceList();
            }, Source == SourceOptions.Available);
            list.Add(item2);
            TabRecord item3 = new TabRecord("FMP.Current".Translate(), delegate
            {
                Source = SourceOptions.Current;
                RefreshSourceList();
            }, Source == SourceOptions.Current);
            list.Add(item3);
            TabDrawer.DrawTabs(canvas, list);

            // content
            Rect scrollCanvas = canvas.ContractedBy(10f);
            scrollCanvas.yMin = scrollCanvas.yMin + 40f;
            float height = SourceListHeight + 20f;
            Rect scrollView = new Rect(0f, 0f, scrollCanvas.width - 16f, height);
            Widgets.BeginScrollView(scrollCanvas, ref LeftRowScrollPosition, scrollView);
            Rect scrollContent = scrollView.ContractedBy(10f);

            GUI.BeginGroup(scrollContent);
            float y = 0;

            foreach (ManagerJobProduction current in from job in SourceList
                                             where job.Bill.recipe.label.ToUpper().Contains(SourceFilter.ToUpper()) || job.MainProduct.Label.ToUpper().Contains(SourceFilter.ToUpper())
                                             orderby job.Bill.recipe.LabelCap
                                             select job)
            {
                Rect recipeRow = new Rect(0f, y, scrollContent.width, 25f);

                string text = current.Bill.recipe.LabelCap + " (";
                try
                {
                    text += String.Join(", ", current.BillGivers.GetBillGiverDefs.Select(ru => ru.LabelCap).ToArray());
                }
                catch
                {
                    text += "error";
                }
                text += ")";

                // resize the row if label grow too big.
                Rect recipeRowResized = new Rect(recipeRow);
                recipeRowResized.x += 6f;
                recipeRowResized.width -= 6f;
                float calculatedHeight = Text.CalcHeight(text, recipeRowResized.width);
                if (recipeRowResized.height < calculatedHeight)
                {
                    recipeRowResized.height = calculatedHeight + 3f;
                }


                if (Widgets.TextButton(recipeRowResized, text, false, true))
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Job = current;
                }

                if (Job != null && Job == current)
                {
                    GUI.DrawTexture(recipeRowResized, TexUI.HighlightTex);
                }

                y += recipeRowResized.height;
            }
            SourceListHeight = y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }
    }
}
