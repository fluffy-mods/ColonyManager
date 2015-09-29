using System.Collections.Generic;
using System.Linq;
using Verse;
// TODO: save / load.

namespace FM
{
    public static class Manager
    {
        public static ManagerTab[] ManagerTabs =  {
            new ManagerTabProduction()
        };

        private static JobStack _stack;

        public static JobStack JobStack => _stack ?? (_stack = new JobStack());

        public static void DoWork()
        {
            Log.Message("Trying to do work");
            JobStack.TryDoNextJob();
        }
    }

    public class JobStack
    {
        public JobStack()
        {
            stack = new List<ManagerJob>();
        }

        private List<ManagerJob> stack;

        public List<ManagerJob> FullStack
        {
            get
            {
                return stack.OrderBy(mj => mj.Priority).ToList();
            }
        } 

        public List<ManagerJob> CurStack
        {
            get
            {
                return stack.Where(mj => mj.ShouldDoNow).OrderBy(mj => mj.Priority).ToList();
            }
        }

        public ManagerJob NextJob => CurStack.DefaultIfEmpty(null).FirstOrDefault();

        public void TryDoNextJob()
        {
            ManagerJob job = NextJob;
            if (job == null)
            {
#if DEBUG
                Log.Message("Tried to do job, but stack is empty");
#endif
                return;
            }

            job.Touch();
            if (!job.TryDoJob()) TryDoNextJob();
        }
        
        public void Add(ManagerJob job)
        {
            job.Priority = stack.Count + 1;
            stack.Add(job);
        }

        public void Delete(ManagerJob job)
        {
            job.CleanUp();
            stack.Remove(job);
        }
    }
}
