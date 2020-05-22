// Settings.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class Settings : ModSettings
    {
        private static int _defaultUpdateIntervalTicks_Scribe = GenDate.TicksPerDay;

        public static UpdateInterval DefaultUpdateInterval
        {
            get => ticksToInterval( _defaultUpdateIntervalTicks_Scribe );
            set => _defaultUpdateIntervalTicks_Scribe = value.ticks;
        }

        public static void DoSettingsWindowContents( Rect rect )
        {
            var row = new Rect( rect.xMin, rect.yMin, rect.width, Constants.ListEntryHeight );

            // labels
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label( row, "FM.DefaultUpdateInterval".Translate() );
            Text.Anchor = TextAnchor.LowerRight;
            Widgets.Label( row, DefaultUpdateInterval.label );
            Text.Anchor = TextAnchor.UpperLeft;

            // interaction
            Widgets.DrawHighlightIfMouseover( row );
            if ( Widgets.ButtonInvisible( row ) )
            {
                var options = new List<FloatMenuOption>();
                foreach ( var interval in Utilities.UpdateIntervalOptions )
                    options.Add( new FloatMenuOption( interval.label, () => DefaultUpdateInterval = interval ) );

                Find.WindowStack.Add( new FloatMenu( options ) );
            }
        }

        private static UpdateInterval ticksToInterval( int ticks )
        {
            foreach ( var interval in Utilities.UpdateIntervalOptions )
                if ( interval.ticks == ticks )
                    return interval;
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look( ref _defaultUpdateIntervalTicks_Scribe, "defaultUpdateInterval", GenDate.TicksPerDay );
        }
    }
}