// Manager/Window_TriggerThresholdDetails.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:29

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FM
{
    public class WindowTriggerThresholdDetails : Window
    {
        public Vector2 FilterScrollPosition = new Vector2( 0f, 0f );

        public string Input;

        public Trigger_Threshold Trigger;

        public override Vector2 InitialWindowSize => new Vector2( 300f, 500 );

        public override void DoWindowContents( Rect inRect )
        {
            Rect filterRect        = new Rect( inRect.ContractedBy( 6f ) );
            filterRect.height     -= 30f;
            ThingFilterUI filterUI = new ThingFilterUI();
            filterUI.DoThingFilterConfigWindow( filterRect, ref FilterScrollPosition, Trigger.ThresholdFilter, null, 4 );
            Rect buttonRect        = new Rect( filterRect.xMin, filterRect.yMax + 3, ( filterRect.width - 6 ) / 2, 25f );
            if ( Widgets.TextButton( buttonRect, Trigger.OpString ) )
            {
                List< FloatMenuOption > list = new List< FloatMenuOption >
                {
                    new FloatMenuOption( "Lower than",   delegate { Trigger.Op = Trigger_Threshold.Ops.LowerThan; } ),
                    new FloatMenuOption( "Equal to",     delegate { Trigger.Op = Trigger_Threshold.Ops.Equals; } ),
                    new FloatMenuOption( "Greater than", delegate { Trigger.Op = Trigger_Threshold.Ops.HigherThan; } )
                };
                Find.WindowStack.Add( new FloatMenu( list ) );
            }
            buttonRect.x = buttonRect.xMax + 3f;
            Color oldColor = GUI.color;
            if ( !Input.IsInt() )
            {
                GUI.color = new Color( 1f, 0f, 0f );
            }
            else
            {
                Trigger.Count = int.Parse( Input );
                if ( Trigger.Count > Trigger.MaxUpperThreshold )
                {
                    Trigger.MaxUpperThreshold = Trigger.Count;
                }
            }
            Input = Widgets.TextField( buttonRect, Input );
            GUI.color = oldColor;
            if ( Event.current.type == EventType.KeyDown &&
                 Event.current.keyCode == KeyCode.Return )
            {
                Event.current.Use();
                Find.WindowStack.TryRemove( this );
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Input = Trigger.Count.ToString();
        }
    }
}