// Karel Kroeze
// ManagerTab_Power.cs
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
    public class ManagerTab_Power : ManagerTab, IExposable
    {
        public static bool                         unlocked = false;
        private       List<List<CompPowerBattery>> _batteries;

        private readonly List<ThingDef> _batteryDefs;

        private Vector2 _consumptionScrollPos = Vector2.zero;

        private Vector2 _overallScrollPos = Vector2.zero;

        private Vector2 _productionScrollPos = Vector2.zero;

        private readonly List<ThingDef> _traderDefs;

        private List<List<CompPowerTrader>> _traders;

        private History overallHistory;

        private History tradingHistory;

        public ManagerTab_Power( Manager manager ) : base( manager )
        {
            // get list of thingdefs set to use the power comps - this should be static throughout the game (barring added mods midgame)
            _traderDefs  = GetTraderDefs().ToList();
            _batteryDefs = GetBatteryDefs().ToList();

            // get a dictionary of powercomps actually existing on the map for each thingdef.
            RefreshCompLists();

            // set up the history trackers.
            tradingHistory =
                new History(
                    _traderDefs.Select(
                                    def =>
                                        new ThingDefCount( def,
                                                           manager.map.listerBuildings.AllBuildingsColonistOfDef( def )
                                                                  .Count() ) )
                               .ToArray() )
                {
                    DrawOptions      = false,
                    DrawInlineLegend = false,
                    Suffix           = "W",
                    DrawInfoInBar    = true,
                    DrawMaxMarkers   = true
                };

            overallHistory = new History( new[] {"Production", "Consumption", "Batteries"} )
            {
                DrawOptions      = false,
                DrawInlineLegend = false,
                Suffix           = "W",
                DrawIcons        = false,
                DrawCounts       = false,
                DrawInfoInBar    = true,
                DrawMaxMarkers   = true,
                MaxPerChapter    = true
            };
        }

        public bool AnyPoweredStationOnline
        {
            get
            {
                return manager.map.listerBuildings
                              .AllBuildingsColonistOfClass<Building_ManagerStation>()
                              .Select( t => t.TryGetComp<CompPowerTrader>() )
                              .Any( c => c != null && c.PowerOn );
            }
        }

        public override Texture2D Icon => Resources.IconPower;

        public override IconAreas IconArea => IconAreas.Middle;

        public override string Label => "FME.Power".Translate();

        public override ManagerJob Selected { get; set; }

        public override bool Enabled => unlocked && AnyPoweredStationOnline;

        public override string DisabledReason
        {
            get
            {
                if ( !unlocked )
                    return "FME.NotResearched".Translate();
                if ( !AnyPoweredStationOnline )
                    return "FME.NoPoweredStation".Translate();
                return "Not sure. It should be enabled? Send a bug report.";
            }
        }

        public void ExposeData()
        {
            Scribe_Deep.Look( ref tradingHistory, "tradingHistory" );
            Scribe_Deep.Look( ref overallHistory, "overallHistory" );
        }

        public override void DoWindowContents( Rect canvas )
        {
            // set up rects
            var overviewRect = new Rect( 0f, 0f, canvas.width, 150f );
            var consumtionRect = new Rect( 0f, overviewRect.height + Margin,
                                           ( canvas.width - Margin ) / 2f,
                                           canvas.height - overviewRect.height - Margin );
            var productionRect = new Rect( consumtionRect.xMax + Margin,
                                           overviewRect.height + Margin,
                                           ( canvas.width - Margin ) / 2f,
                                           canvas.height - overviewRect.height - Margin );

            // draw area BG's
            Widgets.DrawMenuSection( overviewRect );
            Widgets.DrawMenuSection( consumtionRect );
            Widgets.DrawMenuSection( productionRect );

            // draw contents
            DrawOverview( overviewRect );
            DrawConsumption( consumtionRect );
            DrawProduction( productionRect );
        }

        public override void PreOpen()
        {
            base.PreOpen();

            // close this tab if it was selected but no longer available
            if ( !AnyPoweredStationOnline && MainTabWindow_Manager.CurrentTab == this )
            {
                MainTabWindow_Manager.CurrentTab = MainTabWindow_Manager.DefaultTab;
                MainTabWindow_Manager.CurrentTab.PreOpen();
            }
        }

        public override void Tick()
        {
            base.Tick();

            // once in a while, update the list of comps, and history thingcounts + theoretical maxes (where known).
            if ( Find.TickManager.TicksGame % 2000 == 0 && Enabled )
            {
#if DEBUG_POWER
                Log.Message( string.Join( ", ", _traderDefs.Select( d => d.LabelCap ).ToArray() ) );
#endif

                // get all existing comps for all building defs that have power related comps (in essence, get all powertraders)
                RefreshCompLists();

                // update these counts in the history tracker + reset maxes if count changed.
                tradingHistory.UpdateThingCountAndMax( _traders.Select( list => list.Count ).ToArray(),
                                                       _traders.Select( list => 0 ).ToArray() );

                // update theoretical max for batteries, and reset observed max.
                overallHistory.UpdateMax( 0, 0,
                                          (int)
                                          _batteries.Sum(
                                              list => list.Sum( battery => battery.Props.storedEnergyMax ) ) );
            }

            // update the history tracker.
            var trade = GetCurrentTrade();
            tradingHistory.Update( trade );
            overallHistory.Update( trade.Where( i => i > 0 ).Sum(), trade.Where( i => i < 0 ).Sum( Math.Abs ),
                                   GetCurrentBatteries().Sum() );
        }

        private void DrawConsumption( Rect canvas )
        {
            // setup rects
            var plotRect = new Rect( canvas.xMin, canvas.yMin, canvas.width, ( canvas.height - Margin ) / 2f );
            var legendRect = new Rect( canvas.xMin, plotRect.yMax + Margin, canvas.width,
                                       ( canvas.height - Margin ) / 2f );

            // draw the plot
            tradingHistory.DrawPlot( plotRect, negativeOnly: true );

            // draw the detailed legend
            tradingHistory.DrawDetailedLegend( legendRect, ref _consumptionScrollPos, null, false, true );
        }

        private void DrawOverview( Rect canvas )
        {
            // setup rects
            var legendRect = new Rect( canvas.xMin, canvas.yMin, ( canvas.width - Margin ) / 2f,
                                       canvas.height - ButtonSize.y - Margin );
            var plotRect = new Rect( legendRect.xMax + Margin, canvas.yMin,
                                     ( canvas.width - Margin ) / 2f, canvas.height );
            var buttonsRect = new Rect( canvas.xMin, legendRect.yMax + Margin,
                                        ( canvas.width - Margin ) / 2f, ButtonSize.y );

            // draw the plot
            overallHistory.DrawPlot( plotRect );

            // draw the detailed legend
            overallHistory.DrawDetailedLegend( legendRect, ref _overallScrollPos, null );

            // draw period switcher
            var periodRect = buttonsRect;
            periodRect.width /= 2f;

            // label
            Widgets_Labels.Label( periodRect, "FME.PeriodShown".Translate( tradingHistory.periodShown.ToString() ),
                                  "FME.PeriodShownTooltip".Translate( tradingHistory.periodShown.ToString() ) );

            // mark interactivity
            var searchIconRect = periodRect;
            searchIconRect.xMin = searchIconRect.xMax - searchIconRect.height;
            if ( searchIconRect.height > SmallIconSize )
                // center it.
                searchIconRect = searchIconRect.ContractedBy( ( searchIconRect.height - SmallIconSize ) / 2 );
            GUI.DrawTexture( searchIconRect, Resources.Search );
            Widgets.DrawHighlightIfMouseover( periodRect );
            if ( Widgets.ButtonInvisible( periodRect ) )
            {
                var periodOptions = new List<FloatMenuOption>();
                for ( var i = 0; i < History.periods.Length; i++ )
                {
                    var period = History.periods[i];
                    periodOptions.Add( new FloatMenuOption( period.ToString(), delegate
                    {
                        tradingHistory.periodShown =
                            period;
                        overallHistory.periodShown =
                            period;
                    } ) );
                }

                Find.WindowStack.Add( new FloatMenu( periodOptions ) );
            }
        }

        private void DrawProduction( Rect canvas )
        {
            // setup rects
            var plotRect = new Rect( canvas.xMin, canvas.yMin, canvas.width, ( canvas.height - Margin ) / 2f );
            var legendRect = new Rect( canvas.xMin, plotRect.yMax + Margin, canvas.width,
                                       ( canvas.height - Margin ) / 2f );

            // draw the plot
            tradingHistory.DrawPlot( plotRect, positiveOnly: true );

            // draw the detailed legend
            tradingHistory.DrawDetailedLegend( legendRect, ref _productionScrollPos, null, true );
        }

        private IEnumerable<ThingDef> GetBatteryDefs()
        {
            return from td in DefDatabase<ThingDef>.AllDefsListForReading
                   where td.HasComp( typeof( CompPowerBattery ) )
                   select td;
        }

        private int[] GetCurrentBatteries()
        {
            return _batteries.Select( list => (int) list.Sum( battery => battery.StoredEnergy ) ).ToArray();
        }

        private int[] GetCurrentTrade()
        {
            return
                _traders.Select( list => (int) list.Sum( trader => trader.PowerOn ? trader.PowerOutput : 0f ) )
                        .ToArray();
        }

        private IEnumerable<ThingDef> GetTraderDefs()
        {
            return from td in DefDatabase<ThingDef>.AllDefsListForReading
                   where td.HasCompOrChildCompOf( typeof( CompPowerTrader ) )
                   select td;
        }

        private void RefreshCompLists()
        {
            // get list of power trader comps per def for consumers and producers.
            _traders = _traderDefs.Select( def => manager.map.listerBuildings.AllBuildingsColonistOfDef( def )
                                                         .Select( t => t.GetComp<CompPowerTrader>() )
                                                         .ToList() )
                                  .ToList();

            // get list of lists of powertrader comps per thingdef.
            _batteries = _batteryDefs
                        .Select( v => manager.map.listerBuildings.AllBuildingsColonistOfDef( v )
                                             .Select( t => t.GetComp<CompPowerBattery>() )
                                             .ToList() )
                        .ToList();
        }
    }
}