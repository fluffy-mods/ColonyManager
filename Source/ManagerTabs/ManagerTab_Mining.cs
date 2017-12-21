//// ManagerTab_Mining.cs
//// Copyright Karel Kroeze, 2017-2017

//using System.Collections.Generic;
//using UnityEngine;
//using Verse;
//using static FluffyManager.Constants;

//namespace FluffyManager
//{
//    public class ManagerTab_Mining : ManagerTab
//    {
//        private float _jobListHeight;
//        private Vector2 _jobListScrollPosition = Vector2.zero;
//        private ManagerJob_Mining _selected;

//        public List<ManagerJob_Mining> Jobs;

//        public ManagerTab_Mining( Manager manager ) : base( manager )
//        {
//            _selected = new ManagerJob_Mining( manager );
//        }

//        public override Texture2D Icon => Resources.IconMining;
//        public override IconAreas IconArea => IconAreas.Middle;
//        public override string Label => "FM.Mining".Translate();
//        public override ManagerJob Selected { get; set; }
//        public override void DoWindowContents( Rect canvas )
//        {
//            var jobListRect = new Rect(
//                canvas.xMin,
//                canvas.yMin,
//                DefaultLeftRowSize,
//                canvas.height );
//            var jobDetailsRect = new Rect(
//                jobListRect.xMax + Margin,
//                canvas.yMin,
//                canvas.width - jobListRect.width - Margin,
//                canvas.height );

//            DoJobList( jobListRect );
//            DoJobDetails( jobDetailsRect );
//        }

//        private void DoJobDetails( Rect rect )
//        {            // layout: settings | animals
//            // draw background
//            Widgets.DrawMenuSection(rect);

//            // rects
//            var optionsColumnRect = new Rect(
//                rect.xMin,
//                rect.yMin,
//                rect.width * 3 / 5f,
//                rect.height - Margin - ButtonSize.y);
//            var mineralsColumnRect = new Rect(
//                optionsColumnRect.xMax,
//                rect.yMin,
//                rect.width * 2 / 5f,
//                rect.height - Margin - ButtonSize.y);
//            var buttonRect = new Rect(
//                rect.xMax - ButtonSize.x,
//                rect.yMax - ButtonSize.y,
//                ButtonSize.x - Margin,
//                ButtonSize.y - Margin);

//            Vector2 position;
//            float width;

//            // options
//            Widgets_Section.BeginSectionColumn(optionsColumnRect, "Mining.Options", out position, out width);

//            Widgets_Section.EndSectionColumn("Mining.Options", position);

//            // animals
//            Widgets_Section.BeginSectionColumn(mineralsColumnRect, "Mining.Minerals", out position, out width);
//            var refreshRect = new Rect(
//                position.x + width - SmallIconSize - 2 * Margin,
//                position.y + Margin,
//                SmallIconSize,
//                SmallIconSize);
//            if (Widgets.ButtonImage(refreshRect, Resources.Refresh, Color.grey))
//                _selected.RefreshAllowedMinerals();

//            Widgets_Section.EndSectionColumn("Mining.Minerals", position);

//            // do the button
//            if (!_selected.Managed)
//            {
//                if (Widgets.ButtonText(buttonRect, "FM.Manage".Translate()))
//                {
//                    // activate job, add it to the stack
//                    _selected.Managed = true;
//                    Manager.For(manager).JobStack.Add(_selected);

//                    // refresh source list
//                    Refresh();
//                }
//            }
//            else
//            {
//                if (Widgets.ButtonText(buttonRect, "FM.Delete".Translate()))
//                {
//                    // inactivate job, remove from the stack.
//                    Manager.For(manager).JobStack.Delete(_selected);

//                    // remove content from UI
//                    _selected = null;

//                    // refresh source list
//                    Refresh();
//                }
//            }
//        }

//        private void DoJobList( Rect rect )
//        {
//            Widgets.DrawMenuSection(rect);

//            // content
//            float height = _jobListHeight;
//            var scrollView = new Rect(0f, 0f, rect.width, height);
//            if (height > rect.height)
//                scrollView.width -= ScrollbarWidth;

//            Widgets.BeginScrollView(rect, ref _jobListScrollPosition, scrollView);
//            Rect scrollContent = scrollView;

//            GUI.BeginGroup(scrollContent);
//            Vector2 cur = Vector2.zero;
//            var i = 0;

//            foreach (var job in Jobs)
//            {
//                var row = new Rect(0f, cur.y, scrollContent.width, LargeListEntryHeight);
//                Widgets.DrawHighlightIfMouseover(row);
//                if (_selected == job)
//                {
//                    Widgets.DrawHighlightSelected(row);
//                }

//                if (i++ % 2 == 1)
//                {
//                    Widgets.DrawAltRect(row);
//                }

//                Rect jobRect = row;

//                if (ManagerTab_Overview.DrawOrderButtons(new Rect(row.xMax - 50f, row.yMin, 50f, 50f), manager, job))
//                {
//                    Refresh();
//                }
//                jobRect.width -= 50f;

//                job.DrawListEntry(jobRect, false, true);
//                if (Widgets.ButtonInvisible(jobRect))
//                {
//                    _selected = job;
//                }

//                cur.y += LargeListEntryHeight;
//            }

//            // row for new job.
//            var newRect = new Rect(0f, cur.y, scrollContent.width, LargeListEntryHeight);
//            Widgets.DrawHighlightIfMouseover(newRect);

//            if (i++ % 2 == 1)
//            {
//                Widgets.DrawAltRect(newRect);
//            }

//            Text.Anchor = TextAnchor.MiddleCenter;
//            Widgets.Label(newRect, "<" + "FMH.NewHuntingJob".Translate() + ">");
//            Text.Anchor = TextAnchor.UpperLeft;

//            if (Widgets.ButtonInvisible(newRect))
//            {
//                Selected = new ManagerJob_Hunting(manager);
//            }

//            TooltipHandler.TipRegion(newRect, "FMH.NewHuntingJobTooltip".Translate());

//            cur.y += LargeListEntryHeight;

//            _jobListHeight = cur.y;
//            GUI.EndGroup();
//            Widgets.EndScrollView();
//        }

//        public override void PreOpen()
//        {
//            Refresh();
//        }

//        public void Refresh()
//        {
//            // upate our list of jobs
//            Jobs = Manager.For(manager).JobStack.FullStack<ManagerJob_Mining>();

//            // update pawnkind options
//            foreach (var job in Jobs)
//                job.RefreshAllowedMinerals();
//            _selected?.RefreshAllowedMinerals();
//        }
//    }
//}