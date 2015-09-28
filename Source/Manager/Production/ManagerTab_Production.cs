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
    public class ManagerTab_Production : ManagerTab
    {
        private string label = "FMP.Production".Translate();

        public ManagerTab_Production()
        {
            RefreshSourceList();
        }

        public override string Label
        {
            get
            {
                return label;
            }
        }

        public float leftRowSize = 300f;

        public enum sourceOptions
        {
            all,
            available,
            current
        }

        public static sourceOptions source = sourceOptions.all;

        public static Vector2 leftRowScrollPosition = new Vector2(0f, 0f);

        public static Vector2 billScrollPosition = new Vector2(0f, 0f);

        public static List<ManagerJob_Production> sourceList;

        public static string sourceFilter = "";

        public static float sourceListHeight;

        public static void RefreshSourceList()
        {
            sourceList = new List<ManagerJob_Production>();

            switch (source)
            {
                case sourceOptions.available:
                    sourceList = (from rd in DefDatabase<RecipeDef>.AllDefsListForReading
                                  where rd.HasBuildingRecipeUser(true)
                                  select (new ManagerJob_Production(rd))).ToList();
                                  // select (rd.UsesUnfinishedThing ? new Bill_Managed(rd) : (Bill_Managed) new Bill_ManagedWithUft(rd))).ToList();
                    break;
                case sourceOptions.current:
                    sourceList = Manager.JobStack.FullStack.OfType<ManagerJob_Production>().ToList();
                    break;
                case sourceOptions.all:
                default:
                    sourceList = (from rd in DefDatabase<RecipeDef>.AllDefsListForReading
                                  where rd.HasBuildingRecipeUser()
                                  select (new ManagerJob_Production(rd))).ToList();
                    // select (rd.UsesUnfinishedThing ? new Bill_Managed(rd) : (Bill_Managed) new Bill_ManagedWithUft(rd))).ToList();
                    break;
            }
        }

        public static ManagerJob_Production job;

        public override void DoWindowContents(Rect canvas)
        {
            Rect leftRow = new Rect(0f, 35f, leftRowSize, canvas.height - 35f);
            Rect contentCanvas = new Rect(leftRow.xMax + 10f, 5f, canvas.width - leftRow.width - 10f, canvas.height - 5f);
            
            DoLeftRow(leftRow);
            DoContent(contentCanvas);
        }

        public void DoContent(Rect canvas)
        {
            Widgets.DrawMenuSection(canvas, true);

            if (job != null)
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
                if (source == sourceOptions.current)
                {
                    if (Widgets.TextButton(add, "FM.Delete".Translate()))
                    {
                        Manager.JobStack.Delete(job);
                        job = null;
                        RefreshSourceList();
                        return; // hopefully that'll just skip to the next tick without any further errors?
                    }
                    TooltipHandler.TipRegion(add, "FMP.DeleteBillTooltip".Translate());
                }
                else
                {
                    if (job.trigger.IsValid)
                    {
                        if (Widgets.TextButton(add, "FM.Manage".Translate()))
                        {
                            Manager.JobStack.Add(job);

                            source = sourceOptions.current;
                            RefreshSourceList();
                            sourceFilter = "";
                        }
                        TooltipHandler.TipRegion(add, "FMP.ManageBillTooltip".Translate());
                    } else
                    {
                        TextAnchor oldAnchor = Text.Anchor;
                        Color oldColor = GUI.color;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        GUI.color = new Color(.6f, .6f, .6f);
                        Widgets.DrawBox(add, 1);
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
                Widgets.Label(recta, job.bill.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                Rect rect2 = new Rect(6f, 50f, canvas.width * .3f, canvas.height - 50f);
                Listing_Standard listing_Standard = new Listing_Standard(rect2);
                if (job.bill.suspended)
                {
                    if (listing_Standard.DoTextButton("Suspended".Translate()))
                    {
                        job.bill.suspended = false;
                    }
                }
                else if (listing_Standard.DoTextButton("NotSuspended".Translate()))
                {
                    job.bill.suspended = true;
                }
                string label = ("BillStoreMode_" + job.bill.storeMode).Translate();
                if (listing_Standard.DoTextButton(label))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (BillStoreMode mode in Enum.GetValues(typeof(BillStoreMode)))
                    {
                        list.Add(new FloatMenuOption(("BillStoreMode_" + mode).Translate(), delegate
                        {
                            job.bill.storeMode = mode;
                        }, MenuOptionPriority.Medium, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(list, false));
                }

                // other stuff
                listing_Standard.DoGap(12f);
                listing_Standard.DoLabel("IngredientSearchRadius".Translate() + ": " + job.bill.ingredientSearchRadius.ToString("#####0"));
                job.bill.ingredientSearchRadius = Mathf.RoundToInt(listing_Standard.DoSlider((float)job.bill.ingredientSearchRadius, 0f, 250f));

                if (job.bill.recipe.workSkill != null)
                {
                    listing_Standard.DoLabel("MinimumSkillLevel".Translate(new object[]
                    {
                    job.bill.recipe.workSkill.label.ToLower()
                    }) + ": " + job.bill.minSkillLevel.ToString("#####0"));
                    job.bill.minSkillLevel = Mathf.RoundToInt(listing_Standard.DoSlider((float)job.bill.minSkillLevel, 0f, 20f));
                }

                // draw threshold config
                job.trigger.DrawThresholdConfig(ref listing_Standard);
                job.billGivers.DrawBillGiverConfig(ref listing_Standard);
                listing_Standard.End();

                // ingredient picker
                Rect rect3 = new Rect(rect2.xMax + 6f, 50f, canvas.width * .4f, canvas.height - 50f);
                ThingFilterUI.DoThingFilterConfigWindow(rect3, ref billScrollPosition, job.bill.ingredientFilter, job.bill.recipe.fixedIngredientFilter, 4);

                // description
                Rect rect4 = new Rect(rect3.xMax + 6f, rect3.y + 30f, canvas.width - rect3.xMax - 12f, canvas.height - 50f);
                StringBuilder stringBuilder = new StringBuilder();

                // add mainproduct line
                stringBuilder.AppendLine("FMP.MainProduct".Translate(job.mainProduct.Label, job.mainProduct.Count));
                stringBuilder.AppendLine();

                if (job.bill.recipe.description != null)
                {
                    stringBuilder.AppendLine(job.bill.recipe.description);
                    stringBuilder.AppendLine();
                }
                stringBuilder.AppendLine("WorkAmount".Translate() + ": " + job.bill.recipe.WorkAmountTotal(null).ToStringWorkAmount());
                stringBuilder.AppendLine();
                for (int i = 0; i < job.bill.recipe.ingredients.Count; i++)
                {
                    IngredientCount ingredientCount = job.bill.recipe.ingredients[i];
                    if (!ingredientCount.filter.Summary.NullOrEmpty())
                    {
                        stringBuilder.AppendLine(job.bill.recipe.IngredientValueGetter.BillRequirementsDescription(ingredientCount));
                    }
                }
                stringBuilder.AppendLine();
                string text4 = job.bill.recipe.IngredientValueGetter.ExtraDescriptionLine();
                if (text4 != null)
                {
                    stringBuilder.AppendLine(text4);
                    stringBuilder.AppendLine();
                }
                stringBuilder.AppendLine("MinimumSkills".Translate());
                stringBuilder.AppendLine(job.bill.recipe.MinSkillString);
                Text.Font = GameFont.Small;
                string text5 = stringBuilder.ToString();
                if (Text.CalcHeight(text5, rect4.width) > rect4.height)
                {
                    Text.Font = GameFont.Tiny;
                }
                Widgets.Label(rect4, text5);
                Text.Font = GameFont.Small;
                if (job.bill.recipe.products.Count == 1)
                {
                    Widgets.InfoCardButton(rect4.x, rect3.y, job.bill.recipe.products[0].thingDef);
                }
            }
            GUI.EndGroup();

        }

        // focus on the filter on open.
        // TODO: make this actually work.
        public override void PostOpen()
        {
            GUI.FocusControl("filterTextfield");
        }

        public void DoLeftRow(Rect canvas)
        {
            Widgets.DrawMenuSection(canvas, false);

            // filter
            Rect filterRect = new Rect(10f, canvas.yMin + 5f, canvas.width - 50f, 30f);
            GUI.SetNextControlName("filterTextfield");
            sourceFilter = Widgets.TextField(filterRect, sourceFilter);
            if (sourceFilter != "")
            {
                Rect clearFilter = new Rect(filterRect.width + 10f, filterRect.yMin, 30f, 30f);
                if (Widgets.ImageButton(clearFilter, Widgets.CheckboxOffTex))
                {
                    sourceFilter = "";
                }
                TooltipHandler.TipRegion(clearFilter, "FMP.ClearFilterDesc".Translate());
            }
            TooltipHandler.TipRegion(filterRect, "FMP.FilterDesc".Translate());

            // tabs
            List<TabRecord> list = new List<TabRecord>();
            TabRecord item = new TabRecord("FMP.All".Translate(), delegate
            {
                source = sourceOptions.all;
                RefreshSourceList();
            }, source == sourceOptions.all);
            list.Add(item);
            TabRecord item2 = new TabRecord("FMP.Available".Translate(), delegate
            {
                source = sourceOptions.available;
                RefreshSourceList();
            }, source == sourceOptions.available);
            list.Add(item2);
            TabRecord item3 = new TabRecord("FMP.Current".Translate(), delegate
            {
                source = sourceOptions.current;
                RefreshSourceList();
            }, source == sourceOptions.current);
            list.Add(item3);
            TabDrawer.DrawTabs(canvas, list);

            // content
            Rect scrollCanvas = canvas.ContractedBy(10f);
            scrollCanvas.yMin = scrollCanvas.yMin + 40f;
            float height = sourceListHeight + 20f;
            Rect scrollView = new Rect(0f, 0f, scrollCanvas.width - 16f, height);
            Widgets.BeginScrollView(scrollCanvas, ref leftRowScrollPosition, scrollView);
            Rect scrollContent = scrollView.ContractedBy(10f);

            GUI.BeginGroup(scrollContent);
            float y = 0;

            foreach (ManagerJob_Production current in from job in sourceList
                                             where job.bill.recipe.label.ToUpper().Contains(sourceFilter.ToUpper()) || job.mainProduct.Label.ToUpper().Contains(sourceFilter.ToUpper())
                                             orderby job.bill.recipe.LabelCap
                                             select job)
            {
                Rect recipeRow = new Rect(0f, y, scrollContent.width, 25f);

                string text = current.bill.recipe.LabelCap + " (";
                try
                {
                    text += String.Join(", ", current.billGivers.GetBillGiverDefs.Select(ru => ru.LabelCap).ToArray());
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
                    job = current;
                }

                if (job != null && job == current)
                {
                    GUI.DrawTexture(recipeRowResized, TexUI.HighlightTex);
                }

                y += recipeRowResized.height;
            }
            sourceListHeight = y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }
    }
}
