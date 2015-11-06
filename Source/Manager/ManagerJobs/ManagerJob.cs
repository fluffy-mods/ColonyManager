using System;
using System.Text;
using UnityEngine;
using Verse;

namespace FM
{
    internal interface IManagerJob
    {
        bool TryDoJob();
    }

    public abstract class ManagerJob : IManagerJob, IExposable
    {
        public int ActionInterval = 3600; // should be 1 minute.
        public int LastAction;
        public int Priority;
        public Trigger Trigger { get; set; }
        public virtual bool Active { get; set; } = true;
        public abstract string Label { get; }
        public abstract string[] Targets { get; }

        public float _lastUpdateRectWidth = 50f,
                     _progressRectWidth = 10f;

        public abstract ManagerTab Tab
        {
            get;
        }

        public virtual bool IsValid
        {
            get
            {
                return true;
            }
        }

        public virtual void Delete( bool cleanup = true )
        {
            if( cleanup )
            {
                CleanUp();
            }
            Manager.Get.JobStack.Delete( this );
        }

        public bool ShouldDoNow => Active && ( LastAction + ActionInterval ) < Find.TickManager.TicksGame;

        public virtual void ExposeData()
        {
            Scribe_Values.LookValue( ref ActionInterval, "ActionInterval" );
            Scribe_Values.LookValue( ref LastAction, "LastAction" );
            Scribe_Values.LookValue( ref Priority, "Priority" );
        }

        public abstract bool TryDoJob();

        public abstract void CleanUp();

        public void Touch()
        {
            LastAction = Find.TickManager.TicksGame;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine( "Priority: " + Priority );
            s.AppendLine( "Active: " + Active );
            s.AppendLine( "LastAction: " + LastAction );
            s.AppendLine( "Interval: " + ActionInterval );
            s.AppendLine( "GameTick: " + Find.TickManager.TicksGame );
            return s.ToString();
        }

        public virtual void Tick() { }

        public virtual void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | bar | last update

            Rect labelRect = new Rect( Manager.Margin, Manager.Margin,
                                       rect.width - (active ? (_lastUpdateRectWidth + _progressRectWidth + 4 * Manager.Margin) : 2 * Manager.Margin),
                                       rect.height - 2 * Manager.Margin ),
                 progressRect = new Rect( labelRect.xMax + Manager.Margin, Manager.Margin,
                                            _progressRectWidth,
                                            rect.height - 2 * Manager.Margin ),
                 lastUpdateRect = new Rect( progressRect.xMax + Manager.Margin, Manager.Margin,
                                            _lastUpdateRectWidth,
                                            rect.height - 2 * Manager.Margin );

            string text = Label;
            if (Targets != null )
            {
                text += "\n<i>" + string.Join( ", ", Targets ) + "</i>";
            }
                        
            GUI.BeginGroup( rect );
            try
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label( labelRect, text );

                // if the bill has a manager job, give some more info.
                if( active )
                {
                    // draw progress bar
                    if (Trigger != null )
                    {
                        // TODO: how the heck is this suddenly null...?
                        Trigger.DrawProgressBar( progressRect, Active );
                    }

                    // draw time since last action
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label( lastUpdateRect, ( Find.TickManager.TicksGame - LastAction ).TimeString() );
                    TooltipHandler.TipRegion( lastUpdateRect, "FM.LastUpdateTooltip".Translate( ( Find.TickManager.TicksGame - LastAction ).TimeString() ) );
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
    }
}