// Karel Kroeze
// MainTabWindow_Manager.cs
// 2016-12-09

using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    internal class MainTabWindow_Manager : MainTabWindow
    {
        public static ManagerTab CurrentTab;

        private static float _iconSize = 30f;

        private static float _margin = Utilities.Margin;

        public MainTabWindow_Manager()
        {
            if ( CurrentTab == null )
            {
                CurrentTab = DefaultTab;
            }
        }

        public static ManagerTab DefaultTab => Manager.For( Find.VisibleMap ).ManagerTabs[0];

        public static void GoTo( ManagerTab tab, ManagerJob job = null )
        {
            // call pre/post open/close methods
            ManagerTab old = CurrentTab;
            old.PreClose();
            tab.PreOpen();
            CurrentTab = tab;
            old.PostClose();
            tab.PostOpen();

            // if desired, set selected.
            if ( job != null )
            {
                tab.Selected = job;
            }
        }

        public override void DoWindowContents( Rect canvas )
        {
            // zooming in seems to cause Text.Font to start at Tiny, make sure it's set to Small for our panels.
            Text.Font = GameFont.Small;

            // three areas of icons for tabs, left middle and right.
            var leftIcons = new Rect( 0f, 0f,
                                      _margin +
                                      Manager.For( Find.VisibleMap ).ManagerTabsLeft.Count * ( _iconSize + _margin ),
                                      _iconSize );
            var middleIcons = new Rect( 0f, 0f,
                                        _margin +
                                        Manager.For( Find.VisibleMap ).ManagerTabsMiddle.Count * ( _iconSize + _margin ),
                                        _iconSize );
            var rightIcons = new Rect( 0f, 0f,
                                       _margin +
                                       Manager.For( Find.VisibleMap ).ManagerTabsRight.Count * ( _iconSize + _margin ),
                                       _iconSize );

            // finetune rects
            middleIcons = middleIcons.CenteredOnXIn( canvas );
            rightIcons.x += canvas.width - rightIcons.width;

            // left icons (probably only overview, but hey...)
            GUI.BeginGroup( leftIcons );
            var cur = new Vector2( _margin, 0f );
            foreach ( ManagerTab tab in Manager.For( Find.VisibleMap ).ManagerTabsLeft )
            {
                var iconRect = new Rect( cur.x, cur.y, _iconSize, _iconSize );
                DrawTabIcon( iconRect, tab );
                cur.x += _iconSize + _margin;
            }

            GUI.EndGroup();

            // middle icons (the bulk of icons)
            GUI.BeginGroup( middleIcons );
            cur = new Vector2( _margin, 0f );
            foreach ( ManagerTab tab in Manager.For( Find.VisibleMap ).ManagerTabsMiddle )
            {
                var iconRect = new Rect( cur.x, cur.y, _iconSize, _iconSize );
                DrawTabIcon( iconRect, tab );
                cur.x += _iconSize + _margin;
            }

            GUI.EndGroup();

            // right icons (probably only import/export, possbile settings?)
            GUI.BeginGroup( rightIcons );
            cur = new Vector2( _margin, 0f );
            foreach ( ManagerTab tab in Manager.For( Find.VisibleMap ).ManagerTabsRight )
            {
                var iconRect = new Rect( cur.x, cur.y, _iconSize, _iconSize );
                DrawTabIcon( iconRect, tab );
                cur.x += _iconSize + _margin;
            }

            GUI.EndGroup();

            // delegate actual content to the specific manager.
            var contentCanvas = new Rect( 0f, _iconSize + _margin, canvas.width, canvas.height - _iconSize - _margin );
            GUI.BeginGroup( contentCanvas );
            CurrentTab.DoWindowContents( contentCanvas );
            GUI.EndGroup();
        }

        public void DrawTabIcon( Rect rect, ManagerTab tab )
        {
            if ( tab.Enabled )
            {
                if ( tab == CurrentTab )
                {
                    GUI.color = GenUI.MouseoverColor;
                    if ( Widgets.ButtonImage( rect, tab.Icon, GenUI.MouseoverColor ) )
                    {
                        tab.Selected = null;
                    }
                    GUI.color = Color.white;
                }
                else if ( Widgets.ButtonImage( rect, tab.Icon ) )
                {
                    GoTo( tab );
                }
                TooltipHandler.TipRegion( rect, tab.Label );
            }
            else
            {
                GUI.color = Color.grey;
                GUI.DrawTexture( rect, tab.Icon );
                GUI.color = Color.white;
                TooltipHandler.TipRegion( rect, tab.Label + "FM.TabDisabledBecause".Translate( tab.DisabledReason ) );
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            CurrentTab.PostClose();
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CurrentTab.PostOpen();
        }

        public override void PreClose()
        {
            base.PreClose();
            CurrentTab.PreClose();
        }

        public override void PreOpen()
        {
            base.PreOpen();

            // TODO: reimplement help dialog
            //if ( !Manager.For( Find.VisibleMap ).HelpShown )
            //{
            //    Find.WindowStack.Add( new Dialog_Message( "FM.HelpMessage".Translate(), "FM.HelpTitle".Translate() ) );
            //    Manager.For( Find.VisibleMap ).HelpShown = true;
            //}

            // make sure the currently open tab is for this map
            if ( CurrentTab.manager.map != Find.VisibleMap )
                CurrentTab = DefaultTab;
            CurrentTab.PreOpen();
        }
    }
}
