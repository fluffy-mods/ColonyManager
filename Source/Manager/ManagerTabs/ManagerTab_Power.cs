// Manager/ManagerTab_Power.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-12-02 21:12

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FM
{
    public class ManagerTab_Power : ManagerTab
    {
        private List<ThingDef> _batteryDefs;
        private List<ThingDef> _consumerDefs;
        private List<ThingDef> _producerDefs;
        private List<List<CompPowerBattery>> _batteries;
        private List<List<CompPowerTrader>> _producers;
        private List<List<CompPowerTrader>> _consumers;
        
        private History consumptionHistory;
        private History productionHistory;
        private History overallHistory;

        public override string Label => "FME.Power".Translate();
        public override Texture2D Icon => Resources.UnkownIcon;
        public override IconAreas IconArea => IconAreas.Middle;
        public override ManagerJob Selected { get; set; }
        public override void Tick()
        {
            base.Tick();

            // once in a while, update the list of comps.
            if ( Find.TickManager.TicksGame % 2000 == 0 )
            {
                RefreshCompLists();
            }

            // update the history tracker.
            int[] production = GetCurrentProduction();
            productionHistory.Update(production);
            int[] consumption = GetCurrentConsumption();
            consumptionHistory.Update(consumption);
            overallHistory.Update( production.Sum(), consumption.Sum(), GetCurrentBatteries().Sum() );
        }

        private int[] GetCurrentProduction()
        {
            return _producers.Select( list => (int)list.Sum( producer => producer.PowerOutput ) ).ToArray();
        }

        private int[] GetCurrentConsumption()
        {
            return _consumers.Select( list => (int)list.Sum( consumer => - consumer.PowerOutput ) ).ToArray();
        }

        private int[] GetCurrentBatteries()
        {
            return _batteries.Select( list => (int)list.Sum( battery => battery.StoredEnergy ) ).ToArray();
        }

        public ManagerTab_Power()
        {
            // get list of thingdefs set to use the power comps - this should be static throughout the game (barring added mods midgame)
            _consumerDefs = GetConsumerDefs().ToList();
            _producerDefs = GetProducerDefs().ToList();
            _batteryDefs = GetBatteryDefs().ToList();

            // get a dictionary of powercomps actually existing on the map for each thingdef.
            RefreshCompLists();

            // set up the history trackers.
            consumptionHistory = new History( _consumerDefs.Select( def => def.LabelCap ).ToArray() ) { AllowTogglingLegend = false, ShowLegend = false};
            productionHistory = new History( _producerDefs.Select( def => def.LabelCap ).ToArray() ) { AllowTogglingLegend = false, ShowLegend = false };
            overallHistory = new History( new []{"Production", "Consumption", "Batteries"} ) { AllowTogglingLegend = false, ShowLegend = false };
        }

        private IEnumerable<ThingDef> GetProducerDefs()
        {
            return from td in DefDatabase<ThingDef>.AllDefsListForReading
                   where td.HasComp( typeof( CompPowerTrader ) )
                   where td.comps.Any( comp => comp.basePowerConsumption < 0 )
                   select td;
        }
        
        private IEnumerable<ThingDef> GetConsumerDefs()
        {
            return from td in DefDatabase<ThingDef>.AllDefsListForReading
                   where td.HasComp( typeof( CompPowerTrader ) )
                   where td.comps.Any( comp => comp.basePowerConsumption >= 0 )
                   select td;
        }

        private IEnumerable<ThingDef> GetBatteryDefs()
        {
            return from td in DefDatabase<ThingDef>.AllDefsListForReading
                   where td.HasComp( typeof (CompPowerBattery) )
                   select td;
        }

        private void RefreshCompLists()
        {
            // get list of power trader comps per def for consumers and producers.
            _producers = _producerDefs.Select( v => Find.ListerBuildings.AllBuildingsColonistOfDef( v )
                                                            .Select( t => t.GetComp<CompPowerTrader>() )
                                                            .ToList() )
                                                        .ToList();
            _consumers = _consumerDefs.Select( v => Find.ListerBuildings.AllBuildingsColonistOfDef( v )
                                                            .Select( t => t.GetComp<CompPowerTrader>() )
                                                            .ToList() )
                                                        .ToList();


            // get list of lists of powertrader comps per thingdef.
            _batteries = _batteryDefs
                            .Select( v => Find.ListerBuildings.AllBuildingsColonistOfDef( v )
                                .Select( t => t.GetComp<CompPowerBattery>() )
                                .ToList() )
                            .ToList();
        }

        public override void DoWindowContents( Rect canvas )
        {
            // set up rects
            Rect overviewRect = new Rect(0f, 0f, canvas.width, 150f);
            Rect consumtionRect = new Rect( 0f, overviewRect.height + Utilities.Margin,
                                            ( canvas.width - Utilities.Margin ) / 2f,
                                            canvas.height - overviewRect.height - Utilities.Margin );
            Rect productionRect = new Rect( consumtionRect.xMax + Utilities.Margin,
                                            overviewRect.height + Utilities.Margin,
                                            ( canvas.width - Utilities.Margin ) / 2f,
                                            canvas.height - overviewRect.height - Utilities.Margin );

            // draw area BG's
            GUI.DrawTexture( overviewRect, Resources.SlightlyDarkBackground );
            GUI.DrawTexture( consumtionRect, Resources.SlightlyDarkBackground );
            GUI.DrawTexture( productionRect, Resources.SlightlyDarkBackground );

            // draw contents
            DrawOverview( overviewRect );
            DrawConsumption( consumtionRect );
            DrawProduction( productionRect );
        }

        private void DrawProduction( Rect canvas )
        {
            // setup rects 
            Rect legendRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, (canvas.height - Utilities.Margin) / 2f);
            Rect plotRect = new Rect(canvas.xMin, legendRect.yMax + Utilities.Margin, canvas.width, (canvas.height - Utilities.Margin) / 2f);

            // draw the plot
            productionHistory.DrawPlot( plotRect );


        }

        private void DrawConsumption( Rect canvas )
        {
            // setup rects 
            Rect legendRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, (canvas.height - Utilities.Margin) / 2f);
            Rect plotRect = new Rect(canvas.xMin, legendRect.yMax + Utilities.Margin, canvas.width, (canvas.height - Utilities.Margin) / 2f);

            // draw the plot
            consumptionHistory.DrawPlot( plotRect );
        }

        private void DrawOverview( Rect canvas )
        {
            // setup rects 
            Rect legendRect = new Rect( canvas.xMin, canvas.yMin, (canvas.width - Utilities.Margin ) / 2f, canvas.height);
            Rect plotRect = new Rect( legendRect.xMax + Utilities.Margin, canvas.yMin, (canvas.width - Utilities.Margin ) / 2f, canvas.height);

            // draw the plot
            overallHistory.DrawPlot( plotRect );


        }


    }
}