// Karel Kroeze
// Trigger_Threshold.cs
// 2016-12-09

using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class Trigger_Threshold : Trigger
    {
        #region Fields

        public static int DefaultCount = 500;

        public static int DefaultMaxUpperThreshold = 3000;

        public int TargetCount;

        public int MaxUpperThreshold;

        public Ops Op;

        public Zone_Stockpile stockpile;

        public ThingFilter ThresholdFilter;

        public ThingFilter ParentFilter;

        public bool countAllOnMap;

        #endregion Fields

        private string _stockpile_scribe;

        #region Constructors

        public Trigger_Threshold( Manager manager ) : base( manager ) { }
        
        public Trigger_Threshold( ManagerJob_Hunting job ) : base( job.manager )
        {
            Op = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            TargetCount = DefaultCount;
            ThresholdFilter = new ThingFilter();
            ThresholdFilter.SetDisallowAll();

            // limit selection to food only.
            ParentFilter = new ThingFilter();
            ParentFilter.SetDisallowAll();
            ParentFilter.SetAllow( Utilities_Hunting.FoodRaw, true );
        }

        public Trigger_Threshold( ManagerJob_Forestry job ) : base( job.manager )
        {
            Op = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            TargetCount = DefaultCount;
            ThresholdFilter = new ThingFilter();
            ThresholdFilter.SetDisallowAll();
            ThresholdFilter.SetAllow( ThingDefOf.WoodLog, true );
        }

        public Trigger_Threshold( ManagerJob_Foraging job ) : base( job.manager )
        {
            Op = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            TargetCount = DefaultCount;
            ThresholdFilter = new ThingFilter();
            ThresholdFilter.SetDisallowAll();
        }

        public Trigger_Threshold(ManagerJob_Mining job) : base(job.manager)
        {
            Op = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            TargetCount = DefaultCount;
            ThresholdFilter = new ThingFilter( job.Notify_ThresholdFilterChanged );
            ThresholdFilter.SetDisallowAll();

            // limit selection to stone, chunks, minerals and components only.
            ParentFilter = new ThingFilter();
            ParentFilter.SetDisallowAll();
            ParentFilter.SetAllow(ThingCategoryDefOf.Chunks, true);
            ParentFilter.SetAllow(ThingCategoryDefOf.ResourcesRaw, true);
            ParentFilter.SetAllow(ThingCategoryDefOf.PlantMatter, false);
            ParentFilter.SetAllow(ThingDefOf.ComponentIndustrial, true);
        }

        #endregion Constructors



        #region Enums

        public enum Ops
        {
            LowerThan,
            Equals,
            HigherThan
        }

        #endregion Enums

        public int CurrentCount => manager.map.CountProducts( ThresholdFilter, stockpile, countAllOnMap );

        public WindowTriggerThresholdDetails DetailsWindow
        {
            get
            {
                var window = new WindowTriggerThresholdDetails
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
                        return CurrentCount < TargetCount;

                    case Ops.Equals:
                        return CurrentCount == TargetCount;

                    case Ops.HigherThan:
                        return CurrentCount > TargetCount;

                    default:
                        Log.Warning( "Trigger_ThingThreshold was defined without a correct operator" );
                        return true;
                }
            }
        }

        public override string StatusTooltip
        {
            get { return "FMP.ThresholdCount".Translate( CurrentCount, TargetCount ); }
        }

        public override void DrawProgressBar( Rect rect, bool active )
        {
            // bar always goes a little beyond the actual target
            int max = Math.Max( (int)( TargetCount * 1.2f ), CurrentCount );

            // draw a box for the bar
            GUI.color = Color.gray;
            Widgets.DrawBox( rect.ContractedBy( 1f ) );
            GUI.color = Color.white;

            // get the bar rect
            Rect barRect = rect.ContractedBy( 2f );
            float unit = barRect.height / max;
            float markHeight = barRect.yMin + ( max - TargetCount ) * unit;
            barRect.yMin += ( max - CurrentCount ) * unit;

            // draw the bar
            // if the job is active and pending, make the bar blueish green - otherwise white.
            Texture2D barTex = active
                                   ? Resources.BarBackgroundActiveTexture
                                   : Resources.BarBackgroundInactiveTexture;
            GUI.DrawTexture( barRect, barTex );

            // draw a mark at the treshold
            Widgets.DrawLineHorizontal( rect.xMin, markHeight, rect.width );

            TooltipHandler.TipRegion( rect, StatusTooltip );
        }

        public override void DrawTriggerConfig( ref Vector2 cur, float width, float entryHeight, string label = null, string tooltip = null, Action onClick = null )
        {
            // target threshold
            var thresholdLabelRect = new Rect(
                cur.x, 
                cur.y, 
                width, 
                entryHeight );
            var searchIconRect = new Rect( 
                thresholdLabelRect.xMax - Margin - entryHeight, 
                cur.y, 
                entryHeight,
                entryHeight );
            searchIconRect = searchIconRect.ContractedBy( ( searchIconRect.height - SmallIconSize ) / 2 );
            cur.y += entryHeight;
            
            var thresholdRect = new Rect( 
                cur.x, 
                cur.y, 
                width, 
                SliderHeight );
            cur.y += entryHeight;
            
            var useResourceListerToggleRect = new Rect(
                cur.x,
                cur.y,
                width,
                entryHeight );
            cur.y += entryHeight;
            

            Widgets.DrawHighlightIfMouseover( thresholdLabelRect );
            if ( label.NullOrEmpty() )
            {
                label = "FMP.ThresholdCount".Translate( CurrentCount, TargetCount ) + ":";
            }
            if ( tooltip.NullOrEmpty() )
            {
                tooltip = "FMP.ThresholdCountTooltip".Translate( CurrentCount, TargetCount );
            }

            Widgets_Labels.Label( thresholdLabelRect, label, tooltip );

            // add a little icon to mark interactivity
            GUI.DrawTexture( searchIconRect, Resources.Search );

            if ( Widgets.ButtonInvisible( thresholdLabelRect ) )
            {
                onClick?.Invoke();
                Find.WindowStack.Add( DetailsWindow );
            }
            
            Utilities.DrawToggle( useResourceListerToggleRect, "FM.CountAllOnMap".Translate(), "FM.CountAllOnMap.Tip".Translate(), ref countAllOnMap, true );
            TargetCount = (int)GUI.HorizontalSlider( thresholdRect, TargetCount, 0, MaxUpperThreshold );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look( ref TargetCount, "Count" );
            Scribe_Values.Look( ref MaxUpperThreshold, "MaxUpperThreshold" );
            Scribe_Values.Look( ref Op, "Operator" );
            Scribe_Deep.Look( ref ThresholdFilter, "ThresholdFilter" );
            Scribe_Values.Look( ref countAllOnMap, "CountAllOnMap" );

            // stockpile needs special treatment - is not referenceable.
            if ( Scribe.mode == LoadSaveMode.Saving )
            {
                _stockpile_scribe = stockpile?.ToString() ?? "null";
            }
            Scribe_Values.Look( ref _stockpile_scribe, "Stockpile", "null" );
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                stockpile =
                    manager.map.zoneManager.AllZones.FirstOrDefault( z => z is Zone_Stockpile && 
                                                                          z.label == _stockpile_scribe )
                    as Zone_Stockpile;
            }
        }

        public override string ToString()
        {
            // TODO: Implement Trigger_Threshold.ToString()
            return "Trigger_Threshold.ToString() not implemented";
        }
    }
}
