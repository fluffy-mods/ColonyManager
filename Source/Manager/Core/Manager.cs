using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using UnityEngine;

namespace FM
{
    public static class Manager
    {
        public static ManagerTab[] ManagerTabs =  {
            new ManagerTab_Production()
        };

        private static JobStack stack = null;

        public static JobStack JobStack
        {
            get
            {
                if (stack == null) stack = new JobStack();
                return stack;
            }
        }

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
            this.stack = new List<ManagerJob>();
        }

        private List<ManagerJob> stack;

        public List<ManagerJob> FullStack
        {
            get
            {
                return stack.OrderBy(mj => mj.priority).ToList();
            }
        } 

        public List<ManagerJob> CurStack
        {
            get
            {
                return stack.Where(mj => mj.ShouldDoNow).OrderBy(mj => mj.priority).ToList();
            }
        }

        public ManagerJob NextJob
        {
            get
            {
                return CurStack.DefaultIfEmpty(null).FirstOrDefault();
            }
        }

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
            job.priority = stack.Count + 1;
            stack.Add(job);
        }

        public void Delete(ManagerJob job)
        {
            job.CleanUp();
            stack.Remove(job);
        }
    }
}
