// Manager/ManagerTab_Power.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-12-02 21:12

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
        private List<ThingDef> _powerTraderDefs;
        private List<List<CompPowerBattery>> _batteries;
        private List<List<CompPowerTrader>> _powerTraders;

        private History powerHistory;

        public override string Label => "FME.Power".Translate();
        public override Texture2D Icon { get; }
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
            powerHistory.Update(GetCurrentPower());
        }

        public int[] GetCurrentPower()
        {
            int[] values = new int[_powerTraders.Count + _batteries.Count];
            int i = 0, j = 0;
            
            while ( i < _powerTraders.Count )
            {
                values[i] = (int)_powerTraders[i].Sum( pt => pt.PowerOutput );
                i++;
            }

            while ( j < _batteries.Count )
            {
                values[i] = (int)_batteries[j].Sum( b => b.StoredEnergy );
                i++;
                j++;
            }

            return values;
        }

        public ManagerTab_Power()
        {
            // get list of thingdefs set to use the power comps - this should be static throughout the game (barring added mods midgame)
            _powerTraderDefs = GetPowerTraderDefs().ToList();
            _batteryDefs = GetBatteryDefs().ToList();

            // get a dictionary of powercomps actually existing on the map for each thingdef.
            RefreshCompLists();

            // set up the history tracker.
            List<string> labels = _powerTraderDefs.Select( td => td.LabelCap ).ToList();
            labels.AddRange(_batteryDefs.Select(td => td.LabelCap));
            powerHistory = new History(labels.ToArray());
        }

        private IEnumerable<ThingDef> GetPowerTraderDefs()
        {
            return from td in DefDatabase<ThingDef>.AllDefsListForReading
                   where td.HasComp( typeof (CompPowerTrader) )
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
            // get list of lists of powertrader comps per thingdef.
            _powerTraders = _powerTraderDefs
                            .Select( v => Find.ListerBuildings.AllBuildingsColonistOfDef( v )
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
            Rect leftRow = new Rect( 0f, 0f, DefaultLeftRowSize, canvas.height );
            Rect contentCanvas = new Rect( leftRow.xMax + Utilities.Margin, 0f,
                                           canvas.width - leftRow.width - Utilities.Margin, canvas.height );

            DoLeftRow( leftRow );
            DoContent( contentCanvas );
        }

        private void DoContent( Rect canvas )
        {
            powerHistory.DrawPlot( canvas );
        }
        private void DoLeftRow( Rect canvas ) {}
    }
}