using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace FM
{
    public class Trigger_Threshold : Trigger
    {
        public Trigger_Threshold(ManagerJob_Production job)
        {
            this.job = job;
            op = ops.LowerThan;
            maxUpperThreshold = job.mainProduct.MaxUpperThreshold;
            count = (int)maxUpperThreshold / 5;
            thresholdFilter = new ThingFilter();
            thresholdFilter.SetDisallowAll();
            if (job.mainProduct.ThingDef != null) thresholdFilter.SetAllow(job.mainProduct.ThingDef, true);
            if (job.mainProduct.CategoryDef != null) thresholdFilter.SetAllow(job.mainProduct.CategoryDef, true);
        }

        private ManagerJob job;

        public int maxUpperThreshold;

        public bool IsValid
        {
            get
            {
                return thresholdFilter.AllowedDefCount > 0;
            }
        }

        public int CurCount
        {
            get
            {
#if DEBUG_DEEP
                Log.Message("Getting count...");
#endif
                return Utilities.CountProducts(thresholdFilter);
            }
        }

        public ThingFilter thresholdFilter;

        public enum ops
        {
            LowerThan,
            Equals,
            HigherThan
        }

        public ops op;

        public virtual string opString
        {
            get
            {
                switch (op)
                {
                    case ops.LowerThan:
                        return " < ";
                    case ops.Equals:
                        return " = ";
                    case ops.HigherThan:
                        return " > ";
                    default:
                        return " ? ";
                }
            }
        }

        public override bool state
        {
            get
            {
                switch (op)
                {
                    case ops.LowerThan:
                        return CurCount < count;
                    case ops.Equals:
                        return CurCount == count;
                    case ops.HigherThan:
                        return CurCount > count;
                    default:
                        Log.Warning("Trigger_ThingThreshold was defined without a correct operator");
                        return true;
                }
            }
        }


        public override string ToString()
        {
            // TODO: implement for ThingFilter
            //switch (thresholdTargetMode)
            //{
            //    case ThresholdTargetModes.Thing:
            //        return (thing == null ? "null" : thing.LabelCap) + opString + count + " (" + CurCount + " " + state.ToString() + ")";
            //    case ThresholdTargetModes.Category:
            //        return (category == null ? "null" : category.LabelCap) + opString + count + " (" + CurCount + " " + state.ToString() + ")";
            //    default:
            //        return "Incorrectly initialized trigger";
            //}
            return "Trigger_Threshold.ToString() not implemented";
        }

        public int count = 0;

        public override void DrawThresholdConfig(ref Listing_Standard listing)
        {
            // target threshold
            listing.DoGap(12f);
            listing.DoLabel("FMP.Threshold".Translate() + ":");
            listing.DoLabel("FMP.ThresholdCount".Translate(CurCount, count));
            // TODO: implement trade screen sliders - they're so pretty! :D
            count = Mathf.RoundToInt(listing.DoSlider(count, 0, maxUpperThreshold));
            listing.DoGap(6f);
            if (listing.DoTextButton("FMP.ThresholdDetails".Translate()))
            {
                Find.WindowStack.Add(DetailsWindow);
            }
        }

        public Window_TriggerThresholdDetails DetailsWindow
        {
            get
            {
                Window_TriggerThresholdDetails window = new Window_TriggerThresholdDetails();
                window.trigger = this;
                window.closeOnClickedOutside = true;
                window.draggable = true;
                return window;
            }
        }
    }
}
