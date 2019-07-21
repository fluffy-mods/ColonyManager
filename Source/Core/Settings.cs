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
        public static int DefaultUpdateInterval = GenDate.TicksPerHour;

        public static void DoSettingsWindowContents( Rect rect )
        {
            var row = new Rect( rect.xMin, rect.yMin, rect.width, Constants.ListEntryHeight );

            // labels
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label( row, "FM.DefaultUpdateInterval".Translate() );
            Text.Anchor = TextAnchor.LowerRight;
            Widgets.Label(
                row, Utilities.updateIntervalOptions.FirstOrDefault( p => p.Value == DefaultUpdateInterval ).Key );
            Text.Anchor = TextAnchor.UpperLeft;

            // interaction
            Widgets.DrawHighlightIfMouseover( row );
            if ( Widgets.ButtonInvisible( row ) )
            {
                var options = new List<FloatMenuOption>();
                foreach ( var period in Utilities.updateIntervalOptions )
                {
                    var label = period.Key;
                    var time  = period.Value;
                    options.Add( new FloatMenuOption( label, delegate { DefaultUpdateInterval = time; }
                                 ) );
                }

                Find.WindowStack.Add( new FloatMenu( options ) );
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look( ref DefaultUpdateInterval, "defaultUpdateInterval", GenDate.TicksPerHour );
        }
    }
}