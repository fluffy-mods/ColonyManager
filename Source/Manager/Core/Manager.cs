using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

// todo: implement reservations for managerjobs.

namespace FM
{
    public class Manager : MapComponent
    {
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
            new ManagerTabProduction()
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
    }

    public class JobStack : IExposable
    {
        /// <summary>
        /// Full jobstack, in order of assignment
        /// </summary>
        public JobStack()
        {
            _stack = new List<ManagerJob>();
        }

        public void ExposeData()
        {
            Scribe_Collections.LookList(ref _stack, "JobStack", LookMode.Deep);
        }

        private List<ManagerJob> _stack;

        /// <summary>
        /// Full jobstack, in order of priority
        /// </summary>
        public List<ManagerJob> FullStack
        {
            get
            {
                return _stack.OrderBy(mj => mj.Priority).ToList();
            }
        } 

        /// <summary>
        /// Jobstack of jobs that are available now
        /// </summary>
        public List<ManagerJob> CurStack
        {
            get
            {
                return _stack.Where(mj => mj.ShouldDoNow).OrderBy(mj => mj.Priority).ToList();
            }
        }

        /// <summary>
        /// Highest priority available job
        /// </summary>
        public ManagerJob NextJob => CurStack.DefaultIfEmpty(null).FirstOrDefault();

        /// <summary>
        /// Call the worker for the next available job
        /// </summary>
        public void TryDoNextJob()
        {
            ManagerJob job = NextJob;
            if (job == null)
            {
#if DEBUG_JOBS
                Log.Message("Tried to do job, but _stack is empty");
#endif
                return;
            }

            // update lastAction
            job.Touch();

            // perform next job if no action was taken
            if (!job.TryDoJob()) TryDoNextJob();
        }
        
        public void Add(ManagerJob job)
        {
            job.Priority = _stack.Count + 1;
            _stack.Add(job);
        }

        public void Delete(ManagerJob job)
        {
            job.CleanUp();
            _stack.Remove(job);
        }
    }
}
