using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace FM
{
    class AreaAllowedGUI
    {
        // RimWorld.AreaAllowedGUI
        public static void DoAllowedAreaSelectors(Rect rect, ManagerJob job, AllowedAreaMode mode = AllowedAreaMode.Humanlike)
        {
            List<Area> allAreas = Find.AreaManager.AllAreas;
            int areaCount = 1;
            for (int i = 0; i < allAreas.Count; i++)
            {
                if (allAreas[i].AssignableAsAllowed(mode))
                {
                    areaCount++;
                }
            }
            float widthPerArea = rect.width / (float)areaCount;
            Text.WordWrap = false;
            Text.Font = GameFont.Tiny;
            Rect nullAreaRect = new Rect(rect.x, rect.y, widthPerArea, rect.height);
            AreaAllowedGUI.DoAreaSelector(nullAreaRect, job, null);
            int areaIndex = 1;
            for (int j = 0; j < allAreas.Count; j++)
            {
                if (allAreas[j].AssignableAsAllowed(mode))
                {
                    float xOffset = (float)areaIndex * widthPerArea;
                    Rect areaRect = new Rect(rect.x + xOffset, rect.y, widthPerArea, rect.height);
                    AreaAllowedGUI.DoAreaSelector(areaRect, job, allAreas[j]);
                    areaIndex++;
                }
            }
            Text.WordWrap = true;
        }

        // RimWorld.AreaAllowedGUI
        private static void DoAreaSelector(Rect rect, ManagerJob job, Area area)
        {
            rect = rect.ContractedBy(1f);
            GUI.DrawTexture(rect, (area == null) ? BaseContent.GreyTex : area.ColorTexture);
            Text.Anchor = TextAnchor.MiddleLeft;
            string text = AreaUtility.AreaAllowedLabel_Area(area);
            Rect rect2 = rect;
            rect2.xMin += 3f;
            rect2.yMin += 2f;
            Widgets.Label(rect2, text);
            if (job.AreaRestriction == area)
            {
                Widgets.DrawBox(rect, 2);
            }
            if (Mouse.IsOver(rect))
            {
                if (area != null)
                {
                    area.MarkForDraw();
                }
                if (Input.GetMouseButton(0) && job.AreaRestriction != area)
                {
                    job.AreaRestriction = area;
                    SoundDefOf.DesignateDragStandardChanged.PlayOneShotOnCamera();
                }
            }
            TooltipHandler.TipRegion(rect, text);
        }

    }
}
