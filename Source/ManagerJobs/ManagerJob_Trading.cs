using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluffyManager;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class ManagerJob_Trading : ManagerJob
    {
        private string _label;
        private bool _completed;
        private ManagerTab _tab;
        private string[] _targets;
        private WorkTypeDef _workTypeDef;

        public override string Label => "FMT.Trading".Translate();
        public override bool Completed => false;
        public override ManagerTab Tab => Manager.Get.ManagerTabs.Find( tab => tab is ManagerTab_Trading );
        // TODO: string representation of filters.
        public override string[] Targets => new [] {""};
        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Warden;

        // Trading jobs will never be managed in the sense that it requires a manager to interact.
        // It does require a trader to do things, but that's further down the line.
        public override bool Managed => false;

        public override bool TryDoJob()
        {
            Log.Warning( "Manager tried doing job " + ToString() + ". Trading jobs should never be directly managed." );
            return false;
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
    }
}
