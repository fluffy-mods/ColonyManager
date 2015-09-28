using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using UnityEngine;

namespace FM
{
    public class Window_TriggerThresholdDetails : Window
    {
        public override Vector2 InitialWindowSize
        {
            get
            {
                return new Vector2(300f, 500);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            input = trigger.count.ToString();
        }

        public string input;

        public override void DoWindowContents(Rect inRect)
        {
            Rect filterRect = new Rect(inRect.ContractedBy(6f));
            filterRect.height -= 30f;
            ThingFilterUI_Searchable filterUI = new ThingFilterUI_Searchable();
            filterUI.DoThingFilterConfigWindow(filterRect, ref filterScrollPosition, trigger.thresholdFilter, null, 4);
            Rect buttonRect = new Rect(filterRect.xMin, filterRect.yMax + 3, (filterRect.width - 6) / 2, 25f);
            if (Widgets.TextButton(buttonRect, trigger.opString))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                list.Add(new FloatMenuOption("Lower than", delegate { trigger.op = Trigger_Threshold.ops.LowerThan; }));
                list.Add(new FloatMenuOption("Equal to", delegate { trigger.op = Trigger_Threshold.ops.Equals; }));
                list.Add(new FloatMenuOption("Greater than", delegate { trigger.op = Trigger_Threshold.ops.HigherThan; }));
                Find.WindowStack.Add(new FloatMenu(list));
            };
            buttonRect.x = buttonRect.xMax + 3f;
            // TODO: string to int, validate
            Color oldColor = GUI.color;
            if (!input.IsInt())
            {
                GUI.color = new Color(1f, 0f, 0f);
            }
            else
            {
                trigger.count = int.Parse(input);
                if (trigger.count > trigger.maxUpperThreshold) trigger.maxUpperThreshold = trigger.count;
            }
            input = Widgets.TextField(buttonRect, input);
            GUI.color = oldColor;
        }

        public Trigger_Threshold trigger;

        public Vector2 filterScrollPosition = new Vector2(0f, 0f);
    }
}
