using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

// todo: implement reservations for managerjobs.

namespace FM
{
    public class Manager : MapComponent
    {
        public const float Margin = 6f;

        public Manager()
        {
            _stack = new JobStack();
        }

        public override void ExposeData()
        {
            Scribe_Deep.LookDeep(ref _stack, "JobStack");
            base.ExposeData();

            if (_stack == null) _stack = new JobStack();
        }

        public ManagerTab[] ManagerTabs =
        {
            new ManagerTab_Overview(),
            new ManagerTab_Production(),
            // TODO: new ManagerTabLifestock(),
            // TODO: new ManagerTabHunting(),
            // TODO: new ManagerTabForestry()
        };

        private JobStack _stack;

        public JobStack GetJobStack => _stack ?? (_stack = new JobStack());

        public void DoWork()
        {
#if DEBUG_JOBS
            Log.Message("Trying to do work");
#endif
            GetJobStack.TryDoNextJob();
        }

        // copypasta from AutoEquip.
        public static Manager Get
        {
            get
            {
                Manager getComponent =
                    Find.Map.components.OfType<Manager>().FirstOrDefault();
                if (getComponent == null)
                {
                    getComponent = new Manager();
                    Find.Map.components.Add(getComponent);
                }

                return getComponent;
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            foreach (ManagerJob job in GetJobStack.FullStack)
            {
                job.Tick();
            }
        }
    }
}
