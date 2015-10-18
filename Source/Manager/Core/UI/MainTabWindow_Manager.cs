using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FM
{
    class MainTabWindow_Manager : MainTabWindow
    {
        public MainTabWindow_Manager()
        {
            if (CurrentTab == null) CurrentTab = DefaultTab;
        }

        public ManagerTab DefaultTab = Manager.Get.ManagerTabs[0];

        public ManagerTab CurrentTab;

        public override void PostOpen()
        {
            base.PostOpen();
            CurrentTab.PostOpen();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            CurrentTab.PreOpen();
        }

        public override void PreClose()
        {
            base.PreClose();
            CurrentTab.PreClose();
        }

        public override void PostClose()
        {
            base.PostClose();
            CurrentTab.PostClose();
        }

        public override void DoWindowContents(Rect canvas)
        {
            Rect buttonRect = new Rect(0f, 0f, 200f, 30f);
            if (Widgets.TextButton(buttonRect, CurrentTab.Label))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                for (int i = 0; i < Manager.Get.ManagerTabs.Length; i++)
                {
                    ManagerTab current = Manager.Get.ManagerTabs[i];
                    list.Add(new FloatMenuOption(current.Label, delegate
                    {
                        ManagerTab old = CurrentTab;
                        old.PreClose();
                        current.PreOpen();
                        CurrentTab = current;
                        old.PostClose();
                        current.PostOpen();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 0f, canvas.width, 55f), "FM.Manager".Translate() + " - " + CurrentTab.Label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // delegate actual content to the specific manager.
            Rect contentCanvas = new Rect(0f, 55f, canvas.width, canvas.height - 55f);
            GUI.BeginGroup(contentCanvas);
            CurrentTab.DoWindowContents(contentCanvas);
            GUI.EndGroup();
        }
    }
}
