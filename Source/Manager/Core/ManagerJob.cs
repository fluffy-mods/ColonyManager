using System.Text;
using RimWorld;
using Verse;

namespace FM
{
    interface IManagerJob
    {
        bool TryDoJob();
    }

    public abstract class ManagerJob : IManagerJob, IExposable
    {
        public virtual bool TryDoJob()
        {
            Log.Warning("Tried to perform job, but the dispatch was not correctly implemented");
            return false;
        }

        public abstract void CleanUp();

        public Trigger Trigger;

        public int Priority;

        public virtual bool Active
        {
            get; set;
        }

        public bool ShouldDoNow => Active && (LastAction + ActionInterval) < Find.TickManager.TicksGame;

        public int LastAction;

        public int ActionInterval = 3600; // should be 1 minute.

        public void Touch()
        {
            LastAction = Find.TickManager.TicksGame;
        }

        public override string ToString()
        {
            StringBuilder strout = new StringBuilder();
            strout.AppendLine(Priority + " " + Active + "LastAction" + LastAction + "(interval: " + ActionInterval + ", gameTick: " + Find.TickManager.TicksGame + ")");
            return strout.ToString();
        }

        public virtual void ExposeData()
        {
            Scribe_Values.LookValue(ref ActionInterval, "ActionInterval");
            Scribe_Values.LookValue(ref LastAction, "LastAction");
            Scribe_Values.LookValue(ref Priority, "Priority");
        }
    }
}
