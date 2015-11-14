// Manager/ManagerJob.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:29

using System.Text;
using UnityEngine;
using Verse;

namespace FM
{
    public abstract class ManagerJob : IManagerJob, IExposable
    {
        public float _lastUpdateRectWidth = 50f,
                     _progressRectWidth = 10f;

        public int ActionInterval = 3600; // should be 1 minute.
        public int LastAction;
        public int Priority;

        public virtual bool Active { get; set; } = false;

        public virtual bool IsValid
        {
            get { return true; }
        }

        public abstract string Label { get; }
        public bool ShouldDoNow => Active && !Suspended && LastAction + ActionInterval < Find.TickManager.TicksGame;
        public virtual bool Suspended { get; set; } = false;

        public abstract ManagerTab Tab { get; }

        public abstract string[] Targets { get; }
        public Trigger Trigger { get; set; }

        public virtual void ExposeData()
        {
            Scribe_Values.LookValue( ref ActionInterval, "ActionInterval" );
            Scribe_Values.LookValue( ref LastAction, "LastAction" );
            Scribe_Values.LookValue( ref Priority, "Priority" );
        }

        public abstract bool TryDoJob();

        public abstract void CleanUp();

        public virtual void Delete( bool cleanup = true )
        {
            if ( cleanup )
            {
                CleanUp();
            }
            Manager.Get.JobStack.Delete( this );
        }

        public virtual void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | bar | last update

            Rect labelRect = new Rect( Utilities.Margin, Utilities.Margin,
                                       rect.width -
                                       ( active
                                           ? _lastUpdateRectWidth + _progressRectWidth + 4 * Utilities.Margin
                                           : 2 * Utilities.Margin ),
                                       rect.height - 2 * Utilities.Margin ),
                 progressRect = new Rect( labelRect.xMax + Utilities.Margin, Utilities.Margin,
                                          _progressRectWidth,
                                          rect.height - 2 * Utilities.Margin ),
                 lastUpdateRect = new Rect( progressRect.xMax + Utilities.Margin, Utilities.Margin,
                                            _lastUpdateRectWidth,
                                            rect.height - 2 * Utilities.Margin );

            string text = Label;
            if ( Targets != null )
            {
                text += "\n<i>" + string.Join( ", ", Targets ) + "</i>";
            }

            GUI.BeginGroup( rect );
            try
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label( labelRect, text );

                // if the bill has a manager job, give some more info.
                if ( active )
                {
                    // draw progress bar
                    if ( Trigger != null )
                    {
                        Trigger.DrawProgressBar( progressRect, Suspended );
                    }

                    // draw time since last action
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label( lastUpdateRect, ( Find.TickManager.TicksGame - LastAction ).TimeString() );
                    TooltipHandler.TipRegion( lastUpdateRect,
                                              "FM.LastUpdateTooltip".Translate(
                                                  ( Find.TickManager.TicksGame - LastAction ).TimeString() ) );
                }
            }
            finally
            {
                // make sure everything is always properly closed / reset to defaults.
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.EndGroup();
            }
        }

        public abstract void DrawOverviewDetails( Rect rect );

        public virtual void Tick() {}

        public override string ToString()
        {
            var s = new StringBuilder();
            s.AppendLine( "Priority: " + Priority );
            s.AppendLine( "Active: " + Suspended );
            s.AppendLine( "LastAction: " + LastAction );
            s.AppendLine( "Interval: " + ActionInterval );
            s.AppendLine( "GameTick: " + Find.TickManager.TicksGame );
            return s.ToString();
        }

        public void Touch()
        {
            LastAction = Find.TickManager.TicksGame;
        }
    }

    internal interface IManagerJob
    {
        bool TryDoJob();
    }
}