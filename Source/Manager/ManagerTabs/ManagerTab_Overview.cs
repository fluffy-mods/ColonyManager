using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FM
{
    internal class ManagerTab_Overview : ManagerTab
    {
        public const float Margin = Manager.Margin,
                           OverviewWidthRatio = .6f,
                           RowHeight = Manager.ListEntryHeight,
                           IconSize = 30f;

        public static readonly Texture2D ArrowTop = ContentFinder< Texture2D >.Get( "UI/Buttons/ArrowTop" ),
                                         ArrowUp = ContentFinder< Texture2D >.Get( "UI/Buttons/ArrowUp" ),
                                         ArrowDown = ContentFinder< Texture2D >.Get( "UI/Buttons/ArrowDown" ),
                                         ArrowBottom = ContentFinder< Texture2D >.Get( "UI/Buttons/ArrowBottom" );

        private Vector2         _overviewScrollPosition     = Vector2.zero;
        private ManagerJob      _selectedJob;
        public float            OverviewHeight              = 9999f;

        public static List< ManagerJob > Jobs
        {
            get { return Manager.Get.JobStack.FullStack; }
        }

        public override string Label { get; } = "FM.Overview".Translate();

        public override ManagerJob Selected
        {
            get
            {
                return _selectedJob;
            }

            set
            {
                _selectedJob = value;
            }
        }

        public override void DoWindowContents( Rect canvas )
        {
            Rect overviewRect = new Rect( 0f, 0f, OverviewWidthRatio * canvas.width, canvas.height );
            Rect sideRectUpper = new Rect( overviewRect.xMax + Margin, 0f,
                                           ( 1 - OverviewWidthRatio ) * canvas.width - Margin,
                                           ( canvas.height - Margin ) / 2 );
            Rect sideRectLower = new Rect( overviewRect.xMax + Margin, sideRectUpper.yMax + Margin, sideRectUpper.width,
                                           sideRectUpper.height - 1 );

            // draw the listing of current jobs.
            Widgets.DrawMenuSection( overviewRect );
            DrawOverview( overviewRect );
            
            // draw the selected job's details
            Widgets.DrawMenuSection( sideRectUpper );
            if (_selectedJob != null )
            {
                _selectedJob.DrawOverviewDetails( sideRectUpper );
            }

            // TODO: draw some stuff I haven't thought of yet.
            // Save/load here?
            // Overview of managers?
            Widgets.DrawMenuSection( sideRectLower );
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( sideRectLower, "Not implemented." );
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void DrawOverview( Rect rect )
        {
            if ( Jobs.NullOrEmpty() )
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.grey;
                Widgets.Label( rect, "FM.NoJobs".Translate() );
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                Rect viewRect = rect;
                Rect contentRect = rect.AtZero();
                if ( OverviewHeight > viewRect.height )
                {
                    contentRect.width -= 16f;
                }

                GUI.BeginGroup( viewRect );
                Widgets.BeginScrollView( viewRect, ref _overviewScrollPosition, contentRect );

                Vector2 cur = Vector2.zero;

                for ( int i = 0; i < Jobs.Count; i++ )
                {
                    Rect row = new Rect( cur.x, cur.y, contentRect.width, 50f );

                    // highlights
                    if ( i % 2 == 1 )
                    {
                        GUI.DrawTexture( row, Manager.OddRowBG );
                    }
                    if ( Jobs[i] == _selectedJob )
                    {
                        Widgets.DrawHighlightSelected( row );
                    }

                    // go to job icon
                    Rect iconRect = new Rect(Margin, row.yMin + (RowHeight - IconSize) / 2, IconSize, IconSize);
                    if (Widgets.ImageButton( iconRect, Jobs[i].Tab.Icon ) )
                    {
                        MainTabWindow_Manager.GoTo( Jobs[i].Tab, Jobs[i] );
                    }

                    // order buttons
                    DrawOrderButtons( new Rect( row.xMax - 50f, row.yMin, 50f, 50f ), Jobs[i] );

                    // job specific overview.
                    Rect jobRect = row;
                    jobRect.width -= RowHeight + IconSize + 2 * Margin; // - (a + b)?
                    jobRect.x += IconSize + 2 * Margin;
                    Jobs[i].DrawListEntry( jobRect, true, true );
                    Widgets.DrawHighlightIfMouseover( row );
                    if ( Widgets.InvisibleButton( jobRect ) )
                    {
                        _selectedJob = Jobs[i];
                    }

                    cur.y += 50f;
                }

                GUI.EndScrollView();
                GUI.EndGroup();

                OverviewHeight = cur.y;
            }
        }

        /// <summary>
        /// Draw a square group of ordering buttons for a job in rect.
        /// This is an OVERALL method that does not care about job types.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        public static bool DrawOrderButtons( Rect rect, ManagerJob job )
        {
            bool ret = false;

            float width = rect.width / 2,
                  height = rect.height / 2;

            Rect upRect = new Rect( rect.xMin, rect.yMin, width, height ).ContractedBy( 1f ),
                 downRect = new Rect( rect.xMin, rect.yMin + height, width, height ).ContractedBy( 1f ),
                 topRect = new Rect( rect.xMin + width, rect.yMin, width, height ).ContractedBy( 1f ),
                 bottomRect = new Rect( rect.xMin + width, rect.yMin + height, width, height ).ContractedBy( 1f );

            bool top = Jobs.IndexOf( job ) == 0,
                 bottom = Jobs.IndexOf( job ) == Jobs.Count - 1;

            if ( !top )
            {
                DrawOrderTooltips( upRect, topRect );
                if ( Widgets.ImageButton( topRect, ArrowTop ) )
                {
                    Manager.Get.JobStack.TopPriority( job );
                    ret = true;
                }

                if ( Widgets.ImageButton( upRect, ArrowUp ) )
                {
                    Manager.Get.JobStack.IncreasePriority( job );
                    ret = true;
                }
            }

            if ( !bottom )
            {
                DrawOrderTooltips( downRect, bottomRect, false );
                if ( Widgets.ImageButton( downRect, ArrowDown ) )
                {
                    Manager.Get.JobStack.DecreasePriority( job );
                    ret = true;
                }

                if ( Widgets.ImageButton( bottomRect, ArrowBottom ) )
                {
                    Manager.Get.JobStack.BottomPriority( job );
                    ret = true;
                }
            }
            return ret;
        }

        /// <summary>
        /// Draw a square group of ordering buttons for a job in rect.
        /// This is an LOCAL method that within the specified job type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rect"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        public static bool DrawOrderButtons< T >( Rect rect, T job ) where T : ManagerJob
        {
            bool ret = false;

            float width = rect.width / 2,
                  height = rect.height / 2;

            Rect upRect = new Rect( rect.xMin, rect.yMin, width, height ).ContractedBy( 1f ),
                 downRect = new Rect( rect.xMin, rect.yMin + height, width, height ).ContractedBy( 1f ),
                 topRect = new Rect( rect.xMin + width, rect.yMin, width, height ).ContractedBy( 1f ),
                 bottomRect = new Rect( rect.xMin + width, rect.yMin + height, width, height ).ContractedBy( 1f );

            List< T > jobsOfType = Jobs.OfType< T >().OrderBy( j => j.Priority ).ToList();

            bool top = jobsOfType.IndexOf( job ) == 0,
                 bottom = jobsOfType.IndexOf( job ) == Jobs.Count - 1;

            if ( !top )
            {
                DrawOrderTooltips( upRect, topRect );
                if ( Widgets.ImageButton( topRect, ArrowTop ) )
                {
                    Manager.Get.JobStack.TopPriority<T>( job );
                    ret = true;
                }

                if ( Widgets.ImageButton( upRect, ArrowUp ) )
                {
                    Manager.Get.JobStack.IncreasePriority<T>( job );
                    ret = true;
                }
            }

            if ( !bottom )
            {
                DrawOrderTooltips( downRect, bottomRect, false );
                if ( Widgets.ImageButton( downRect, ArrowDown ) )
                {
                    Manager.Get.JobStack.DecreasePriority<T>( job );
                    ret = true;
                }

                if ( Widgets.ImageButton( bottomRect, ArrowBottom ) )
                {
                    Manager.Get.JobStack.BottomPriority<T>( job );
                    ret = true;
                }
            }
            return ret;
        }

        public static void DrawOrderTooltips(Rect step, Rect max, bool up = true )
        {
            if ( up )
            {
                TooltipHandler.TipRegion( step, "FM.OrderUp".Translate() );
                TooltipHandler.TipRegion( max, "FM.OrderTop".Translate() );
            }
            else
            {
                TooltipHandler.TipRegion( step, "FM.OrderDown".Translate() );
                TooltipHandler.TipRegion( max, "FM.OrderBottom".Translate() );
            }
        }
    }
}