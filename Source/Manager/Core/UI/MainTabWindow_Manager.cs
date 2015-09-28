using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace FM
{
    class MainTabWindow_Manager : MainTabWindow
    {
        public MainTabWindow_Manager()
        {
            if (currentTab == null) currentTab = defaultTab;
        }

        public ManagerTab defaultTab = Manager.ManagerTabs[0];

        public ManagerTab currentTab;

        public override void PostOpen()
        {
            base.PostOpen();
            currentTab.PostOpen();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            currentTab.PreOpen();
        }

        public override void PreClose()
        {
            base.PreClose();
            currentTab.PreClose();
        }

        public override void PostClose()
        {
            base.PostClose();
            currentTab.PostClose();
        }

        public override void DoWindowContents(Rect canvas)
        {
            Rect buttonRect = new Rect(0f, 0f, 200f, 30f);
            if (Widgets.TextButton(buttonRect, currentTab.Label))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                for (int i = 0; i < Manager.ManagerTabs.Count(); i++)
                {
                    ManagerTab current = Manager.ManagerTabs[i];
                    list.Add(new FloatMenuOption(current.Label, delegate
                    {
                        currentTab = current;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 0f, canvas.width, 55f), "FM.Manager".Translate() + " - " + currentTab.Label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // delegate actual content to the specific manager.
            Rect contentCanvas = new Rect(0f, 55f, canvas.width, canvas.height - 55f);
            GUI.BeginGroup(contentCanvas);
            currentTab.DoWindowContents(contentCanvas);
            GUI.EndGroup();
        }
    }
}
