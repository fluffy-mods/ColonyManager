// Manager/TriggerThreshold.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:25

using System;
using UnityEngine;
using Verse;

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

        public static int DefaultCount                         = 500;
        public static int DefaultMaxUpperThreshold             = 3000;
        public int Count;
        public int MaxUpperThreshold;
        public Ops Op;
        public ThingFilter ThresholdFilter;
        private static Texture2D _barBackgroundActiveTexture   = SolidColorMaterials.NewSolidColorTexture( new Color( 0.2f, 0.8f, 0.85f ) );
        private static Texture2D _barBackgroundInactiveTexture = SolidColorMaterials.NewSolidColorTexture( new Color( 0.7f, 0.7f, 0.7f ) );

        public int CurCount
        {
            get { return Utilities.CountProducts( ThresholdFilter ); }
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

        public bool IsValid
        {
            get { return ThresholdFilter.AllowedDefCount > 0; }
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

        public override string StatusTooltip
        {
            get { return "FMP.ThresholdCount".Translate( CurCount, Count ); }
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
            ThresholdFilter.SetAllow( Utilities_Hunting.RawMeat, true );
        }

        public Trigger_Threshold( ManagerJob_Forestry job )
        {
            Op = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            Count = DefaultCount;
            ThresholdFilter = new ThingFilter();
            ThresholdFilter.SetDisallowAll();
            ThresholdFilter.SetAllow( Utilities_Forestry.Wood, true );
        }

        public override void DrawProgressBar( Rect rect, bool active )
        {
            // bar always goes a little beyond the actual target
            int max = Math.Max( (int)( Count * 1.2f ), CurCount );
            
            // draw a box for the bar
            GUI.color = Color.gray;
            Widgets.DrawBox( rect.ContractedBy( 1f ) );
            GUI.color = Color.white;

            // get the bar rect
            Rect barRect = rect.ContractedBy( 2f );
            float unit = barRect.height / max;
            float markHeight = barRect.yMin + ( max - Count ) * unit;
            barRect.yMin += (max - CurCount) * unit;
            
            // draw the bar
            // if the job is active and pending, make the bar blueish green - otherwise white.
            Texture2D barTex = active
                ? _barBackgroundActiveTexture
                : _barBackgroundInactiveTexture;
            GUI.DrawTexture( barRect, barTex );

            // draw a mark at the treshold
            Widgets.DrawLineHorizontal( rect.xMin, rect.yMax - markHeight, rect.width );

            TooltipHandler.TipRegion( rect, StatusTooltip );
        }

        public override void DrawThresholdConfig( ref Vector2 cur, float width, float entryHeight, bool alt = false )
        {
            // target threshold
            Rect thresholdLabelRect = new Rect( cur.x, cur.y, width, entryHeight );
            if (alt) Widgets.DrawAltRect(thresholdLabelRect);
            Widgets.DrawHighlightIfMouseover(thresholdLabelRect);
            Utilities.Label( thresholdLabelRect, 
                             "FMP.ThresholdCount".Translate( CurCount, Count ) + ":",
                             "FMP.ThresholdCountTooltip".Translate( CurCount, Count),
                             TextAnchor.MiddleLeft,
                             Utilities.Margin );
            cur.y += entryHeight;
            if ( Widgets.InvisibleButton( thresholdLabelRect ) )
            {
                Find.WindowStack.Add( DetailsWindow );
            }

            Rect thresholdRect = new Rect( cur.x, cur.y, width, Utilities.SliderHeight);
            if( alt ) Widgets.DrawAltRect( thresholdRect );
            Count = (int)GUI.HorizontalSlider( thresholdRect, Count, 0, MaxUpperThreshold );
            cur.y += Utilities.SliderHeight;
        }

        public override void ExposeData()
        {
            Scribe_Values.LookValue( ref Count, "Count" );
            Scribe_Values.LookValue( ref MaxUpperThreshold, "MaxUpperThreshold" );
            Scribe_Values.LookValue( ref Op, "Operator" );
            Scribe_Deep.LookDeep( ref ThresholdFilter, "ThresholdFilter" );
        }

        public override string ToString()
        {
            // TODO: Implement Trigger_Threshold.ToString()
            return "Trigger_Threshold.ToString() not implemented";
        }
    }
}