// Karel Kroeze
// Trigger_Threshold.cs
// 2016-12-09

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    public class Trigger_Threshold : Trigger
    {
        public enum Ops
        {
            LowerThan,
            Equals,
            HigherThan
        }

        public static int DefaultCount = 500;

        public static int DefaultMaxUpperThreshold = 3000;

        private string _stockpile_scribe;

        public bool countAllOnMap;

        public int MaxUpperThreshold;

        public Ops Op;

        public ThingFilter ParentFilter;

        public Zone_Stockpile stockpile;

        public int TargetCount;

        public ThingFilter ThresholdFilter;

        public Trigger_Threshold( Manager manager ) : base( manager )
        {
        }

        public Trigger_Threshold( ManagerJob_Hunting job ) : base( job.manager )
        {
            Op                = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            TargetCount       = DefaultCount;
            ThresholdFilter   = new ThingFilter();
            ThresholdFilter.SetDisallowAll();

            // limit selection to food only.
            ParentFilter = new ThingFilter();
            ParentFilter.SetDisallowAll();
            ParentFilter.SetAllow( Utilities_Hunting.FoodRaw, true );
        }

        public Trigger_Threshold( ManagerJob_Forestry job ) : base( job.manager )
        {
            Op                = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            TargetCount       = DefaultCount;
            ThresholdFilter   = new ThingFilter();
            ThresholdFilter.SetDisallowAll();
            ThresholdFilter.SetAllow( ThingDefOf.WoodLog, true );
        }

        public Trigger_Threshold( ManagerJob_Foraging job ) : base( job.manager )
        {
            Op                = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            TargetCount       = DefaultCount;
            ThresholdFilter   = new ThingFilter();
            ThresholdFilter.SetDisallowAll();
        }

        public Trigger_Threshold( ManagerJob_Mining job ) : base( job.manager )
        {
            Op                = Ops.LowerThan;
            MaxUpperThreshold = DefaultMaxUpperThreshold;
            TargetCount       = DefaultCount;
            ThresholdFilter   = new ThingFilter( job.Notify_ThresholdFilterChanged );
            ThresholdFilter.SetDisallowAll();

            // limit selection to stone, chunks, minerals and components only.
            ParentFilter = new ThingFilter();
            ParentFilter.SetDisallowAll();
            ParentFilter.SetAllow( ThingCategoryDefOf.Chunks, true );
            ParentFilter.SetAllow( ThingCategoryDefOf.ResourcesRaw, true );
            ParentFilter.SetAllow( ThingCategoryDefOf.PlantMatter, false );
            ParentFilter.SetAllow( ThingDefOf.ComponentIndustrial, true );
        }

        public int CurrentCount => manager.map.CountProducts( ThresholdFilter, stockpile, countAllOnMap );

        public WindowTriggerThresholdDetails DetailsWindow
        {
            get
            {
                var window = new WindowTriggerThresholdDetails
                {
                    Trigger               = this,
                    closeOnClickedOutside = true,
                    draggable             = true
                };
                return window;
            }
        }

        public bool IsValid => ThresholdFilter.AllowedDefCount > 0;

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

        public override string StatusTooltip => "FMP.ThresholdCount".Translate( CurrentCount, TargetCount );

        public override void DrawProgressBar( Rect rect, bool active )
        {
            // bar always goes a little beyond the actual target
            var max = Math.Max( (int) ( TargetCount * 1.2f ), CurrentCount );

            // draw a box for the bar
            GUI.color = Color.gray;
            Widgets.DrawBox( rect.ContractedBy( 1f ) );
            GUI.color = Color.white;

            // get the bar rect
            var barRect    = rect.ContractedBy( 2f );
            var unit       = barRect.height / max;
            var markHeight = barRect.yMin + ( max - TargetCount ) * unit;
            barRect.yMin += ( max         - CurrentCount ) * unit;

            // draw the bar
            // if the job is active and pending, make the bar blueish green - otherwise white.
            var barTex = active
                ? Resources.BarBackgroundActiveTexture
                : Resources.BarBackgroundInactiveTexture;
            GUI.DrawTexture( barRect, barTex );

            // draw a mark at the treshold
            Widgets.DrawLineHorizontal( rect.xMin, markHeight, rect.width );

            TooltipHandler.TipRegion( rect, ( ) => StatusTooltip, GetHashCode() );
        }

        public override void DrawTriggerConfig( ref Vector2 cur, float width, float entryHeight, string label = null,
                                                string tooltip = null, List<Designation> designations = null,
                                                Action onOpenFilterDetails = null,
                                                Func<Designation, string> designationLabelGetter = null )
        {
            var hasTargets = !designations.NullOrEmpty();

            // target threshold
            var thresholdLabelRect = new Rect(
                cur.x,
                cur.y,
                width - ( hasTargets ? SmallIconSize + Margin * 2 : 0f ),
                entryHeight );
            var detailsWindowButtonRect = new Rect(
                thresholdLabelRect.xMax - SmallIconSize - Margin,
                cur.y                                   + ( entryHeight - SmallIconSize ) / 2f,
                SmallIconSize,
                SmallIconSize );
            var targetsButtonRect = new Rect(
                thresholdLabelRect.xMax + Margin,
                cur.y                   + ( entryHeight - SmallIconSize ) / 2f,
                SmallIconSize,
                SmallIconSize
            );
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
            if ( label.NullOrEmpty() ) label     = "FMP.ThresholdCount".Translate( CurrentCount, TargetCount ) + ":";
            if ( tooltip.NullOrEmpty() ) tooltip = "FMP.ThresholdCountTooltip".Translate( CurrentCount, TargetCount );

            Widgets_Labels.Label( thresholdLabelRect, label, tooltip );

            // add a little icon to mark interactivity
            GUI.color = Mouse.IsOver( thresholdLabelRect ) ? GenUI.MouseoverColor : Color.white;
            GUI.DrawTexture( detailsWindowButtonRect, Resources.Cog );
            GUI.color = Color.white;
            if ( Widgets.ButtonInvisible( thresholdLabelRect ) )
            {
                onOpenFilterDetails?.Invoke();
                Find.WindowStack.Add( DetailsWindow );
            }

            // target list
            if ( hasTargets )
                if ( Widgets.ButtonImage( targetsButtonRect, Resources.Search ) )
                {
                    var options = new List<FloatMenuOption>();
                    foreach ( var designation in designations )
                    {
                        var    option  = string.Empty;
                        Action onClick = () => Find.WindowStack.TryRemove( typeof( MainTabWindow_Manager ), false );
                        Action onHover = null;
                        if ( designation.target.HasThing )
                        {
                            var thing = designation.target.Thing;
                            option  =  designationLabelGetter?.Invoke( designation ) ?? thing.LabelCap;
                            onClick += () => CameraJumper.TryJumpAndSelect( thing );
                            onHover += () => CameraJumper.TryJump( thing );
                        }
                        else
                        {
                            var cell = designation.target.Cell;
                            var map  = Find.CurrentMap;
                            // designation.map would be better, but that's private. We should only ever be looking at jobs on the current map anyway,
                            // so I suppose it doesn't matter -- Fluffy.
                            option  =  designationLabelGetter?.Invoke( designation ) ?? cell.GetTerrain( map ).LabelCap;
                            onClick += () => CameraJumper.TryJump( cell, map );
                            onHover += () => CameraJumper.TryJump( cell, map );
                        }

                        options.Add( new FloatMenuOption( option, onClick, MenuOptionPriority.Default, onHover ) );
                    }

                    Find.WindowStack.Add( new FloatMenu( options ) );
                }

            Utilities.DrawToggle( useResourceListerToggleRect, "FM.CountAllOnMap".Translate(),
                                  "FM.CountAllOnMap.Tip".Translate(), ref countAllOnMap, true );
            TargetCount = (int) GUI.HorizontalSlider( thresholdRect, TargetCount, 0, MaxUpperThreshold );
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
            if ( Scribe.mode == LoadSaveMode.Saving ) _stockpile_scribe = stockpile?.ToString() ?? "null";
            Scribe_Values.Look( ref _stockpile_scribe, "Stockpile", "null" );
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
                stockpile =
                    manager.map.zoneManager.AllZones.FirstOrDefault( z => z is Zone_Stockpile &&
                                                                          z.label == _stockpile_scribe )
                        as Zone_Stockpile;
        }

        public override string ToString()
        {
            // TODO: Implement Trigger_Threshold.ToString()
            return "Trigger_Threshold.ToString() not implemented";
        }
    }
}