using RimWorld;
using UnityEngine;
using Verse;

namespace FM
{
    public class WindowBillGiverDetails : Window
    {
        public override Vector2 InitialWindowSize => new Vector2(300f, 500);

        public override void PreOpen()
        {
            base.PreOpen();
            _input = BillGivers.UserBillGiverCount.ToString();
        }

        private string _input;

        public override void DoWindowContents(Rect inRect)
        {
            Rect contentRect = new Rect(inRect.ContractedBy(6f));
            GUI.BeginGroup(contentRect);
            //TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            float x = 0;
            float y = 6;

            // All workstations
            Rect all = new Rect(x, y, contentRect.width, 30f);
            Rect allCheck = new Rect(x, y, 30f, 30f);
            Rect allLabel = new Rect(36f, y, contentRect.width - 36f, 30f);
            y += 30;

            if (BillGivers.BillGiverAssignment == AssignedBillGiverOptions.All)
            {
                Widgets.DrawMenuSection(all);
                GUI.DrawTexture(allCheck, Widgets.CheckboxOnTex);
            }
            else
            {
                TooltipHandler.TipRegion(all, "FMP.AllWorkstationTooltip");
                if (Mouse.IsOver(all)) GUI.DrawTexture(all, TexUI.HighlightTex);
                if (Widgets.InvisibleButton(all))
                {
                    BillGivers.BillGiverAssignment = AssignedBillGiverOptions.All;
                }
            }
            Widgets.Label(allLabel, "FMP.AllWorkstations".Translate());
            y += 6;

            // By area / count
            Rect area = new Rect(x, y, contentRect.width, 30f);
            Rect areaCheck = new Rect(x, y, 30f, 30f);
            Rect areaLabel = new Rect(36f, y, contentRect.width - 36f, 30f);
            y += 30f;

            if (BillGivers.BillGiverAssignment == AssignedBillGiverOptions.Count)
            {
                area.height += 60f;
                Widgets.DrawMenuSection(area);
                GUI.DrawTexture(areaCheck, Widgets.CheckboxOnTex);
                Rect areaAreaLabel = new Rect(6f, y, 100f, 30f);
                Rect areaAreaSelector = new Rect(106f, y, contentRect.width - 106f, 30f);
                y += 30;
                Rect areaCountLabel = new Rect(6f, y, 100f, 30f);
                Rect areaCountSelector = new Rect(106f, y, contentRect.width - 106f, 30f);
                y += 30;

                Widgets.Label(areaAreaLabel, "FMP.AllowedAreas".Translate());
                // todo: area selector row
                Color oldColor = GUI.color;
                if (_input.IsInt())
                {
                    BillGivers.UserBillGiverCount = int.Parse(_input);
                }
                else
                {
                    GUI.color = new Color(1f, 0f, 0f);
                }
                Widgets.Label(areaCountLabel, "FMP.AllowedWorkstationCount".Translate());
                _input = Widgets.TextField(areaCountSelector, _input);
                GUI.color = oldColor;
            }
            else
            {
                TooltipHandler.TipRegion(area, "FMP.ByAreaAndCountTooltip");
                if (Mouse.IsOver(area)) GUI.DrawTexture(area, TexUI.HighlightTex);
                if (Widgets.InvisibleButton(area))
                {
                    BillGivers.BillGiverAssignment = AssignedBillGiverOptions.Count;
                }
            }
            Widgets.Label(areaLabel, "FMP.ByAreaAndCount".Translate());
            y += 6f;

            // Specific billgivers
            Rect specific = new Rect(x, y, contentRect.width, 30f);
            Rect specificCheck = new Rect(x, y, 30f, 30f);
            Rect specificLabel = new Rect(36f, y, contentRect.width - 36f, 30f);
            y += 30;

            if (BillGivers.BillGiverAssignment == AssignedBillGiverOptions.Specific)
            {

                specific.height += 24f * BillGivers.GetPotentialBillGivers.Count;
                Widgets.DrawMenuSection(specific);
                GUI.DrawTexture(specificCheck, Widgets.CheckboxOnTex);

                foreach (Building_WorkTable billgiver in BillGivers.GetPotentialBillGivers)
                {
                    Rect row = new Rect(x, y, contentRect.width, 24f);
                    DrawRow(billgiver, row);
                    y += 24f;
                }

            }
            else
            {
                if (Mouse.IsOver(specific)) GUI.DrawTexture(specific, TexUI.HighlightTex);
                TooltipHandler.TipRegion(specific, "FMP.SpecificWorkstationsTooltip");
                if (Widgets.InvisibleButton(specific))
                {
                    BillGivers.BillGiverAssignment = AssignedBillGiverOptions.Specific;
                }
            }
            Widgets.Label(specificLabel, "FMP.SpecificWorkstations".Translate());
            

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        public void DrawRow(Building_WorkTable billgiver, Rect row)
        {
            Rect labelRect = new Rect(row);
            labelRect.width -= 36f;
            labelRect.xMin += 6f;
            Rect iconRect = new Rect(row);
            iconRect.xMin = iconRect.xMax - 24f;

            Text.Font = GameFont.Tiny;
            Widgets.Label(labelRect, billgiver.LabelCap + ", " + billgiver.GetRoom().Role.LabelCap);
            Text.Font = GameFont.Small;
            if (BillGivers.SpecificBillGivers.Contains(billgiver))
            {
                GUI.DrawTexture(iconRect, Widgets.CheckboxOnTex);
                if (Widgets.InvisibleButton(row))
                {
                    BillGivers.SpecificBillGivers.Remove(billgiver);
                }
            }
            else
            {
                if (Widgets.InvisibleButton(row))
                {
                    BillGivers.SpecificBillGivers.Add(billgiver);
                }
            }

            if (Mouse.IsOver(row))
            {
                GUI.DrawTexture(row, TexUI.HighlightTex);
                Find.CameraMap.JumpTo(billgiver.PositionHeld);
            }
        }

        public BillGiverTracker BillGivers;

        public Vector2 Scrollposition = new Vector2(0f, 0f);
    }
}
