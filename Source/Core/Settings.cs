// Karel Kroeze
// Settings.cs
// 2017-05-27

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class Settings : ModSettings
    {
        public static UpdateInterval DefaultUpdateInterval = UpdateInterval.Daily;
        private static int _defaultUpdateIntervalTicks_Scribe;

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
                {
                    options.Add( new FloatMenuOption( interval.label, () => DefaultUpdateInterval = interval ) );
                }

                Find.WindowStack.Add( new FloatMenu( options ) );
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if ( Scribe.mode == LoadSaveMode.Saving )
                _defaultUpdateIntervalTicks_Scribe = DefaultUpdateInterval.ticks;

            Scribe_Values.Look( ref _defaultUpdateIntervalTicks_Scribe, "defaultUpdateInterval", GenDate.TicksPerDay );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
                DefaultUpdateInterval = Utilities.UpdateIntervalOptions.Find( ui => ui.ticks == _defaultUpdateIntervalTicks_Scribe ) ?? UpdateInterval.Daily;

        }
    }
}