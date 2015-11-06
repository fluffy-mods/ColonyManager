using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace FM
{
    public class Trigger_Threshold : Trigger
    {
        public enum Ops
        {
            LowerThan,
            Equals,
            HigherThan
        }

        public int Count;

        public int MaxUpperThreshold;

        public Ops Op;

        public ThingFilter ThresholdFilter;
        public static int DefaultMaxUpperThreshold = 3000;
        public static int DefaultCount = 500;
        public static ThingCategoryDef ThingCategoryDef_Meat = DefDatabase<ThingCategoryDef>.GetNamed("MeatRaw");

        public bool IsValid
        {
            get { return ThresholdFilter.AllowedDefCount > 0; }
        }

        public int CurCount
        {
            get { return Utilities.CountProducts( ThresholdFilter ); }
        }

        public virtual string OpString
        {
            get
            {
                switch ( Op )
                {
                    case Ops.LowerThan:
                        return " < ";
                    case Ops.Equals:
                        return " = ";
                    case Ops.HigherThan:
                        return " > ";
                    default:
                        return " ? ";
                }
            }
        }

        public override bool State
        {
            get
            {
                switch ( Op )
                {
                    case Ops.LowerThan:
                        return CurCount < Count;
                    case Ops.Equals:
                        return CurCount == Count;
                    case Ops.HigherThan:
                        return CurCount > Count;
                    default:
                        Log.Warning( "Trigger_ThingThreshold was defined without a correct operator" );
                        return true;
                }
            }
        }

        public WindowTriggerThresholdDetails DetailsWindow
        {
            get
            {
                WindowTriggerThresholdDetails window = new WindowTriggerThresholdDetails
                {
                    Trigger = this,
                    closeOnClickedOutside = true,
                    draggable = true
                };
                return window;
            }
        }

        public override string StatusTooltip
        {
            get
            {
                return "FMP.ThresholdCount".Translate( CurCount, Count );
            }
        }

        public Trigger_Threshold( ManagerJob_Production job )
        {
            Op = Ops.LowerThan;
            MaxUpperThreshold = job.MainProduct.MaxUpperThreshold;
            Count = MaxUpperThreshold / 5;
            ThresholdFilter = new ThingFilter();
            ThresholdFilter.SetDisallowAll();
            if ( job.MainProduct.ThingDef != null )
            {
                ThresholdFilter.SetAllow( job.MainProduct.ThingDef, true );
            }
            if ( job.MainProduct.CategoryDef != null )
            {
                ThresholdFilter.SetAllow( job.MainProduct.CategoryDef, true );
            }
        }


        public Trigger_Threshold( ManagerJob_Hunting job )
        {
            Op = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            Count = DefaultCount;
            ThresholdFilter = new ThingFilter();
            ThresholdFilter.SetDisallowAll();
            ThresholdFilter.SetAllow( ThingCategoryDef_Meat, true );
        }

        public override string ToString()
        {
            // TODO: Implement Trigger_Threshold.ToString()
            return "Trigger_Threshold.ToString() not implemented";
        }

        public override void ExposeData()
        {
            Scribe_Values.LookValue( ref Count, "Count" );
            Scribe_Values.LookValue( ref MaxUpperThreshold, "MaxUpperThreshold" );
            Scribe_Values.LookValue( ref Op, "Operator" );
            Scribe_Deep.LookDeep( ref ThresholdFilter, "ThresholdFilter" );
        }

        public override void DrawThresholdConfig( ref Listing_Standard listing )
        {
            // target threshold
            listing.DoGap( 24f );

            listing.DoLabel( "FMP.Threshold".Translate() + ":" );
            listing.DoLabel( "FMP.ThresholdCount".Translate( CurCount, Count ) );

            // TODO: implement trade screen sliders - they're so pretty! :D
            Count = Mathf.RoundToInt( listing.DoSlider( Count, 0, MaxUpperThreshold ) );
            listing.DoGap( 6f );
            if ( listing.DoTextButton( "FMP.ThresholdDetails".Translate() ) )
            {
                Find.WindowStack.Add( DetailsWindow );
            }
        }

        public override void DrawProgressBar( Rect rect, bool active )
        {
            // bar always goes a little beyond the actual target
            int max = Math.Max((int)(Count * 1.2f), CurCount);

            // get the bar rect
            float barHeight = rect.height / max * CurCount;
            float markHeight = rect.height / max * Count;
            Rect progressBarRect = new Rect(rect.xMin + 1f, rect.yMax - barHeight, 6f, barHeight);

            // draw a box for the bar
            GUI.color = Color.gray;
            Widgets.DrawBox( rect.ContractedBy( 1f ) );
            GUI.color = Color.white;

            // draw the bar
            // if the job is active and pending, make the bar blueish green - otherwise white.
            Color barColour = active ? new Color( 0.2f, 0.8f, 0.85f ) : new Color( 1f, 1f, 1f );
            Texture2D barTex = SolidColorMaterials.NewSolidColorTexture(barColour);
            GUI.DrawTexture( progressBarRect, barTex );

            // draw a mark at the treshold
            Widgets.DrawLineHorizontal( rect.xMin, rect.yMax - markHeight, rect.width );

            TooltipHandler.TipRegion( rect, StatusTooltip );
        }
    }
}