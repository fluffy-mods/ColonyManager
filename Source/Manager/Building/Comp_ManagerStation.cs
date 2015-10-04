using Verse;

namespace FM
{
    class Comp_ManagerStation : ThingComp
    {
        // todo add automatic work setup.
        // todo add research and more workstations.
        public CompProperties_ManagerStation Props;

        public override void CompTick()
        {
            base.CompTick();
            //if (parent.IsHashIntervalTick(Props.Speed))
            //{
            //    Manager.Get.DoWork();
            //}
        }

        public override void Initialize(CompProperties vprops)
        {
            base.Initialize(vprops);
            Props = (vprops as CompProperties_ManagerStation);
            if (Props == null)
            {
                Log.Warning("Props went horribly wrong.");
                Props = new CompProperties_ManagerStation {Speed = 250};
            }
        }
    }
}
