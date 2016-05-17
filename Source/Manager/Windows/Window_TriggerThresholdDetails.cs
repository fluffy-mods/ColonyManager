// Manager/Window_TriggerThresholdDetails.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:29

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class WindowTriggerThresholdDetails : Window
    {
        private readonly Color red = new Color(1f, 0f, 0f);
        public Vector2 FilterScrollPosition = Vector2.zero;
        public string InputUpperThreshold;
        public string InputLowerThreshold;
        public Trigger_Threshold Trigger;
        public override Vector2 InitialWindowSize => new Vector2( 300f, 500 );
        ThingFilterUI filterUI = new ThingFilterUI();

        public override void DoWindowContents( Rect inRect )
        {
            // set up rects
            Rect filterRect = new Rect( inRect.ContractedBy( 6f ) );
            filterRect.height -= 2 * (Utilities.ListEntryHeight + Utilities.Margin);
            Rect zoneRect = new Rect(filterRect.xMin, filterRect.yMax + Utilities.Margin, filterRect.width, Utilities.ListEntryHeight);
            Rect buttonRect = new Rect( filterRect.xMin, zoneRect.yMax + Utilities.Margin, filterRect.width, Utilities.ListEntryHeight );

            // draw thingfilter
            filterUI.DoThingFilterConfigWindow(filterRect, ref FilterScrollPosition, Trigger.ThresholdFilter );

            // draw zone selector
            StockpileGUI.DoStockpileSelectors(zoneRect, ref Trigger.stockpile);

            // Draw the input fields
            this.DrawInputFields(buttonRect, ref Trigger.CountLowerThreshold, ref Trigger.CountUpperThreshold);
        }

        public override void PreOpen()
        {
            base.PreOpen();
            InputLowerThreshold = Trigger.CountLowerThreshold.ToString();
            InputUpperThreshold = Trigger.CountUpperThreshold.ToString();
        }

        private void DrawInputFields(Rect buttonRect, ref int LowerThreshold, ref int UpperThreshold)
        {
            bool margins = (Trigger.Op == Trigger_Threshold.Ops.Margins);
            if (margins)
            {
                // We need two input fields and stay away from the right border
                buttonRect.xMax = (buttonRect.xMax + Utilities.Margin) / 3;
            }
            else
            {
                buttonRect.xMax = (buttonRect.xMax + Utilities.Margin) / 2;
            }

            // draw operator button
            if (Widgets.TextButton(buttonRect, Trigger.OpString))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>
                {
                    new FloatMenuOption( "Lower than", delegate { Trigger.Op = Trigger_Threshold.Ops.LowerThan; } ),
                    new FloatMenuOption( "Equal to", delegate { Trigger.Op = Trigger_Threshold.Ops.Equals; } ),
                    new FloatMenuOption( "Greater than", delegate { Trigger.Op = Trigger_Threshold.Ops.HigherThan; } ),
                    new FloatMenuOption( "Between", delegate { Trigger.Op = Trigger_Threshold.Ops.Margins; } )
                };
                Find.WindowStack.Add(new FloatMenu(list));
            }

            
            if (margins)
            {
                // Reposition the placement
                buttonRect.x = buttonRect.xMax + Utilities.Margin / 3;
                // We need two input fields
                DrawLowerInputField(buttonRect);
                // Reposition the placement
                buttonRect.x = buttonRect.xMax + Utilities.Margin / 3;
                DrawUpperInputField(buttonRect);
            }
            else
            {
                buttonRect.x = buttonRect.xMax + Utilities.Margin / 2;
                // Draw only a single input field
                DrawLowerInputField(buttonRect);
                Trigger.CountUpperThreshold = 1;
            }

            // close on enter
            if (Event.current.type == EventType.KeyDown &&
                 Event.current.keyCode == KeyCode.Return)
            {
                Event.current.Use();
                Find.WindowStack.TryRemove(this);
            }
        }
        private void DrawUpperInputField(Rect buttonRect)
        {
            Color oldColor = GUI.color;
            if (!InputUpperThreshold.IsInt())
            {
                GUI.color = red;
            }
            else
            {
                Trigger.CountUpperThreshold = int.Parse(InputUpperThreshold);
                if (Trigger.CountUpperThreshold > Trigger.MaxUpperThreshold)
                {
                    Trigger.CountUpperThreshold = Trigger.MaxUpperThreshold;
                }
                else if (Trigger.CountUpperThreshold < Trigger.CountLowerThreshold + Trigger.MinimumThresholdSeparation)
                {
                    Trigger.CountUpperThreshold = Trigger.CountLowerThreshold + Trigger.MinimumThresholdSeparation;
                }
            }

            // draw the input field
            InputUpperThreshold = Widgets.TextField(buttonRect, InputUpperThreshold);
            GUI.color = oldColor;
        }

        private void DrawLowerInputField(Rect buttonRect)
        {
            // if current input is invalid color the element red
            Color oldColor = GUI.color;
            if (!InputLowerThreshold.IsInt())
            {
                GUI.color = red;
            }
            else
            {
                Trigger.CountLowerThreshold = int.Parse(InputLowerThreshold);
                if (Trigger.CountLowerThreshold > Trigger.MaxUpperThreshold)
                {
                    Trigger.MaxUpperThreshold = Trigger.CountLowerThreshold;
                }
            }

            // draw the input field
            InputLowerThreshold = Widgets.TextField(buttonRect, InputLowerThreshold);
            GUI.color = oldColor;
        }
    }
}