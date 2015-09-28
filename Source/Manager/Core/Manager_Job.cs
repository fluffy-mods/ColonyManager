using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;

namespace FM
{
    interface IManagerJob
    {
        bool TryDoJob();
    }

    public abstract class ManagerJob : IManagerJob
    {
        public virtual bool TryDoJob()
        {
            Log.Warning("Tried to perform job, but the dispatch was not correctly implemented");
            return false;
        }

        public abstract void CleanUp();

        public Trigger trigger;

        public int priority;

        public virtual bool active
        {
            get; set;
        }

        public bool ShouldDoNow
        {
            get
            {
#if DEBUG
                Log.Message("Checking job avail: " + active.ToString() + " last" + lastAction + " interval" + actionInterval + " curTick" + Find.TickManager.TicksGame);
#endif
                return active && (lastAction + actionInterval) < Find.TickManager.TicksGame;
            }
        }

        public int lastAction = 0;

        public int actionInterval = 3600; // should be 1 minute.

        public void Touch()
        {
            this.lastAction = Find.TickManager.TicksGame;
        }

        public override string ToString()
        {
            StringBuilder strout = new StringBuilder();
            strout.AppendLine(priority + " " + active + "LastAction" + lastAction + "(interval: " + actionInterval + ", gameTick: " + Find.TickManager.TicksGame + ")");
            return strout.ToString();
        }
    }
}
