using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using UnityEngine;

namespace FM
{
    public class Window_BillGiverDetails : Window
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
            input = billGivers.userBillGiverCount.ToString();
        }

        public string input;

        public override void DoWindowContents(Rect inRect)
        {
            Rect filterRect = new Rect(inRect.ContractedBy(6f));
            

            // TODO: string to int, validate
            Color oldColor = GUI.color;
            if (!input.IsInt())
            {
                GUI.color = new Color(1f, 0f, 0f);
            }
            else
            {
                billGivers.userBillGiverCount = int.Parse(input);
            }
            input = Widgets.TextField(inRect, input);
            GUI.color = oldColor;
        }

        public BillGiver_Tracker billGivers;

        public Vector2 scrollposition = new Vector2(0f, 0f);
    }
}
