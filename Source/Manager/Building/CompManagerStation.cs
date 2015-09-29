using Verse;

namespace FM
{
    class CompManagerStation : ThingComp
    {
        public CompPropertiesManagerStation Props;

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(Props.Speed))
            {
                Manager.DoWork();
            }
        }

        public override void Initialize(CompProperties vprops)
        {
            base.Initialize(vprops);
            Props = (vprops as CompPropertiesManagerStation);
            if (Props == null)
            {
                Log.Warning("Props went horribly wrong.");
                Props = new CompPropertiesManagerStation {Speed = 250};
            }
        }
    }
}
