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
    class Comp_ManagerStation : ThingComp
    {
        public new CompProperties_ManagerStation props;

        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.IsHashIntervalTick(props.speed))
            {
                Manager.DoWork();
            }
        }

        public override void Initialize(CompProperties vprops)
        {
            base.Initialize(vprops);
            this.props = (vprops as CompProperties_ManagerStation);
            if (this.props == null)
            {
                Log.Warning("Props went horribly wrong.");
                this.props.speed = 250;
            }
        }
    }
}
