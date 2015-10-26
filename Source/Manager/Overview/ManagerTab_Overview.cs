using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FM
{
    internal class ManagerTab_Overview : ManagerTab
    {
        public const float Margin = 6f;
        public const float OverviewWidthRatio = .6f;
        public const float RowHeight = 50f;

        private readonly Texture2D oddRowBG = SolidColorMaterials.NewSolidColorTexture( 1f, 1f, 1f, .05f ),
                                   ArrowTop = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowTop" ),
                                   ArrowUp = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowUp" ),
                                   ArrowDown = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowDown" ),
                                   ArrowBottom = ContentFinder<Texture2D>.Get( "UI/Buttons/ArrowBottom" );

        private Vector2 _overviewScrollPosition = Vector2.zero;

        private ManagerJob _selectedJob;
        public float OverviewHeight = 9999f;

        public List<ManagerJob> Jobs
        {
            get { return Manager.Get.GetJobStack.FullStack; }
        }

        public override string Label { get; } = "FM.Overview".Translate( );

        public override void DoWindowContents( Rect canvas )
        {
            var overviewRect = new Rect( 0f, 0f, OverviewWidthRatio * canvas.width, canvas.height );
            var sideRectUpper = new Rect( overviewRect.xMax + Margin, 0f,
                                          ( 1 - OverviewWidthRatio ) * canvas.width - Margin,
                                          ( canvas.height - Margin ) / 2 );
            var sideRectLower = new Rect( overviewRect.xMax + Margin, sideRectUpper.yMax + Margin, sideRectUpper.width,
                                          sideRectUpper.height - 1 );

            Widgets.DrawMenuSection( overviewRect );
            DrawOverview( overviewRect );
            Widgets.DrawMenuSection( sideRectUpper );

            //DrawStats(sideRectUpper.ContractedBy(Margin));
            Widgets.DrawMenuSection( sideRectLower );

            //DrawSaveLoad(sideRectLower.ContractedBy(Margin));
        }

        public void DrawOverview( Rect rect )
        {
            if ( Jobs.NullOrEmpty( ) )
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.grey;

                // TODO: Translation
                Widgets.Label( rect, "FM.NoJobs".Translate( ) );
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                var viewRect = rect;
                var contentRect = rect.AtZero( );
                if ( OverviewHeight > viewRect.height )
                {
                    contentRect.width -= 16f;
                }

                GUI.BeginGroup( viewRect );
                Widgets.BeginScrollView( viewRect, ref _overviewScrollPosition, contentRect );

                var cur = Vector2.zero;

                for ( var i = 0; i < Jobs.Count; i++ )
                {
                    var row = new Rect( cur.x, cur.y, contentRect.width, 50f );
                    if ( i % 2 == 1 )
                    {
                        GUI.DrawTexture( row, oddRowBG );
                    }
                    DrawOrderButtons( new Rect( row.xMax - 50f, row.yMin, 50f, 50f ), Jobs[i] );

                    var jobRect = row;
                    jobRect.width -= 50f;
                    Jobs[i].DrawOverviewSummary( jobRect );

                    if ( _selectedJob == Jobs[i] )
                    {
                        Widgets.DrawHighlightSelected( jobRect );
                    }
                    else
                    {
                        Widgets.DrawHighlightIfMouseover( jobRect );
                        if ( Widgets.InvisibleButton( jobRect ) )
                        {
                            _selectedJob = Jobs[i];
                        }
                    }


                    cur.y += 50f;
                }

                GUI.EndScrollView( );
                GUI.EndGroup( );

                OverviewHeight = cur.y;
            }
        }

        private void DrawOrderButtons( Rect rect, ManagerJob job )
        {
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
                if ( Widgets.ImageButton( topRect, ArrowTop ) )
                {
                    Manager.Get.GetJobStack.TopPriority( job );
                }

                if ( Widgets.ImageButton( upRect, ArrowUp ) )
                {
                    Manager.Get.GetJobStack.IncreasePriority( job );
                }
            }

            if ( !bottom )
            {
                if ( Widgets.ImageButton( downRect, ArrowDown ) )
                {
                    Manager.Get.GetJobStack.DecreasePriority( job );
                }

                if ( Widgets.ImageButton( bottomRect, ArrowBottom ) )
                {
                    Manager.Get.GetJobStack.BottomPriority( job );
                }
            }
        }
    }
}