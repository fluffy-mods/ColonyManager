using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace FM
{
    class ManagerJob_Forestry : ManagerJob
    {
        public override string Label
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ManagerTab Tab
        {
            get
            {
                return Manager.Get.ManagerTabs.Find(tab => tab is ManagerTab_Forestry);
            }
        }

        public override string[] Targets
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void CleanUp()
        {
            throw new NotImplementedException();
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            throw new NotImplementedException();
        }

        public override void DrawOverviewDetails( Rect rect )
        {
            throw new NotImplementedException();
        }

        public override bool TryDoJob()
        {
            throw new NotImplementedException();
        }
    }
}
