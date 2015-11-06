using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace FM
{
    public class ManagerJob_Hunting : ManagerJob
    {
        public List<Pair<PawnKindDef, bool>> AllowedPawnKinds = new List<Pair<PawnKindDef, bool>>();
        public List<Designation> Designations = new List<Designation>();


        private readonly float _margin = Manager.Margin;
        private static int     _histSize = 100;
        private Texture2D      _cogTex = ContentFinder<Texture2D>.Get("UI/Buttons/Cog");

        public History day = new History(_histSize);
        public History month = new History(_histSize, History.period.month);
        public History year = new History(_histSize, History.period.year);
        public History historyShown;

        public new Trigger_Threshold Trigger;

        public override ManagerTab Tab
        {
            get
            {
                return Manager.Get.ManagerTabs.Find( tab => tab is ManagerTab_Hunting );
            }
        }

        public ManagerJob_Hunting()
        {
            Trigger = new Trigger_Threshold( this );
        }

        public override void CleanUp()
        {
            // clear the list of obsolete designations
            CleanDeadDesignations();

            // cancel outstanding designation
            foreach( Designation des in Designations )
            {
                des.Delete();
            }

            // clear the list completely
            Designations.Clear();
        }

        /// <summary>
        /// Remove obsolete designations from the list.
        /// </summary>
        public void CleanDeadDesignations()
        {
            // get the intersection of bills in the game and bills in our list.
            var GameDesignations = Find.DesignationManager.DesignationsOfDef(DesignationDefOf.Hunt).ToList();
            Designations = Designations.Intersect( GameDesignations ).ToList();
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            // (detailButton) | name | bar | last update

            Rect labelRect = new Rect( _margin, _margin,
                                       rect.width - (active ? (_lastUpdateRectWidth + _progressRectWidth + 4 * _margin) : 2 * _margin),
                                       rect.height - 2 * _margin ),
                 progressRect = new Rect( labelRect.xMax + _margin, _margin,
                                            _progressRectWidth,
                                            rect.height - 2 * _margin ),
                 lastUpdateRect = new Rect( progressRect.xMax + _margin, _margin,
                                            _lastUpdateRectWidth,
                                            rect.height - 2 * _margin );

            string text = "FMH.Hunting".Translate() + "\n<i>" +
                          string.Join( ", ", AllowedPawnKinds.Select( p => p.First.LabelCap ).ToArray() ) + "</i>";

#if DEBUG
            text += Priority;
#endif

            GUI.BeginGroup( rect );
            try
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label( labelRect, text );
                // if the bill has a manager job, give some more info.
                if( active )
                {
                    // draw progress bar
                    Trigger.DrawProgressBar( progressRect, Active );

                    // draw time since last action
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label( lastUpdateRect, ( Find.TickManager.TicksGame - LastAction ).TimeString() );

                    // set tooltips
                    TooltipHandler.TipRegion( progressRect, "FMP.ThresholdCount".Translate( Trigger.CurCount, Trigger.Count ) );
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

        public override void DrawOverviewDetails( Rect rect )
        {
            if( historyShown == null )
            {
                historyShown = day;
            }
            historyShown.DrawPlot( rect, Trigger.Count );

            Rect switchRect = new Rect(rect.xMax - 16f - _margin, rect.yMin + _margin, 16f, 16f);
            Widgets.DrawHighlightIfMouseover( switchRect );
            if( Widgets.ImageButton( switchRect, _cogTex ) )
            {
                List<FloatMenuOption> options = new List<FloatMenuOption> {
                    new FloatMenuOption("day", delegate { historyShown = day; } ),
                    new FloatMenuOption("month", delegate { historyShown = month; } ),
                    new FloatMenuOption("year", delegate { historyShown = year; })
                };
                Find.WindowStack.Add( new FloatMenu( options ) );
            }
        }

        public override bool Active
        {
            get
            {
                return base.Active;
            }
        }

        public override string Label
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string[] Targets
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool TryDoJob()
        {
            // TODO: Implement job logic.
            return true;
        }

        public override void Tick()
        {
            if( Find.TickManager.TicksGame % day.Interval == 0 )
            {
                day.Add( Trigger.CurCount );
            }
            if( Find.TickManager.TicksGame % month.Interval == 0 )
            {
                month.Add( Trigger.CurCount );
            }
            if( Find.TickManager.TicksGame % year.Interval == 0 )
            {
                year.Add( Trigger.CurCount );
            }
        }
    }
}
