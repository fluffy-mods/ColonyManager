// Karel Kroeze
// JobStack.cs
// 2016-12-09

using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FluffyManager
{
    public class JobStack : IExposable
    {
        private List<ManagerJob> _stack;

        public Manager manager;

        /// <summary>
        ///     Full jobstack, in order of assignment
        /// </summary>
        public JobStack( Manager manager )
        {
            this.manager = manager;
            _stack       = new List<ManagerJob>();
        }

        /// <summary>
        ///     Jobstack of jobs that are available now
        /// </summary>
        public List<ManagerJob> CurStack
        {
            get { return _stack.Where( mj => mj.ShouldDoNow ).OrderBy( mj => mj.priority ).ToList(); }
        }

        /// <summary>
        ///     Highest priority available job
        /// </summary>
        public ManagerJob NextJob => CurStack.FirstOrDefault();

        public void ExposeData()
        {
            Scribe_Collections.Look( ref _stack, "JobStack", LookMode.Deep, manager );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                if ( _stack.Any( j => !j.IsValid ) )
                {
                    Log.Error( $"Colony Manager :: Removing {_stack.Count( j => !j.IsValid )} invalid manager jobs. If this keeps happening, please report it."  );
                    _stack = _stack.Where( job => job.IsValid ).ToList();
                }
            }
        }

        /// <summary>
        ///     Add job to the stack with bottom priority.
        /// </summary>
        /// <param name="job"></param>
        public void Add( ManagerJob job )
        {
            job.priority = _stack.Count + 1;
            _stack.Add( job );
        }

        public void BottomPriority( ManagerJob job )
        {
            job.priority = _stack.Count + 10;
            CleanPriorities();
        }

        public void BottomPriority<T>( T job ) where T : ManagerJob
        {
            // get list of priorities for this type.
            var jobsOfType = _stack.OfType<T>().OrderBy( j => j.priority ).ToList();
            var priorities = jobsOfType.Select( j => j.priority ).ToList();

            // make sure our job is on the bottom.
            job.priority = _stack.Count + 10;

            // re-sort
            jobsOfType = jobsOfType.OrderBy( j => j.priority ).ToList();

            // fill in priorities, making sure we don't affect other types.
            for ( var i = 0; i < jobsOfType.Count; i++ ) jobsOfType[i].priority = priorities[i];
        }

        public void DecreasePriority( ManagerJob job )
        {
            var jobB = _stack.OrderBy( mj => mj.priority ).First( mj => mj.priority > job.priority );
            SwitchPriorities( job, jobB );
        }

        public void DecreasePriority<T>( T job ) where T : ManagerJob
        {
            ManagerJob jobB = _stack.OfType<T>()
                                    .OrderBy( mj => mj.priority )
                                    .First( mj => mj.priority > job.priority );
            SwitchPriorities( job, jobB );
        }

        /// <summary>
        ///     Cleanup job, delete from stack and update priorities.
        /// </summary>
        /// <param name="job"></param>
        public void Delete( ManagerJob job, bool cleanup = true )
        {
            if ( cleanup )
                job.CleanUp();
            _stack.Remove( job );
            CleanPriorities();
        }

        /// <summary>
        ///     Jobs of type T in jobstack, in order of priority
        /// </summary>
        public List<T> FullStack<T>() where T : ManagerJob
        {
            return _stack.OrderBy( job => job.priority ).OfType<T>().ToList();
        }

        /// <summary>
        ///     Jobs of type T in jobstack, in order of priority
        /// </summary>
        public List<ManagerJob> FullStack()
        {
            return _stack.OrderBy( job => job.priority ).ToList();
        }

        public void IncreasePriority( ManagerJob job )
        {
            var jobB = _stack.OrderByDescending( mj => mj.priority ).First( mj => mj.priority < job.priority );
            SwitchPriorities( job, jobB );
        }

        public void IncreasePriority<T>( T job ) where T : ManagerJob
        {
            ManagerJob jobB =
                _stack.OfType<T>().OrderByDescending( mj => mj.priority ).First( mj => mj.priority < job.priority );
            SwitchPriorities( job, jobB );
        }

        public void SwitchPriorities( ManagerJob a, ManagerJob b )
        {
            var tmp = a.priority;
            a.priority = b.priority;
            b.priority = tmp;
        }

        public void TopPriority( ManagerJob job )
        {
            job.priority = -1;
            CleanPriorities();
        }

        public void TopPriority<T>( T job ) where T : ManagerJob
        {
            // get list of priorities for this type.
            var jobsOfType = _stack.OfType<T>().OrderBy( j => j.priority ).ToList();
            var priorities = jobsOfType.Select( j => j.priority ).ToList();

            // make sure our job is on top.
            job.priority = -1;

            // re-sort
            jobsOfType = jobsOfType.OrderBy( j => j.priority ).ToList();

            // fill in priorities, making sure we don't affect other types.
            for ( var i = 0; i < jobsOfType.Count; i++ ) jobsOfType[i].priority = priorities[i];
        }

        /// <summary>
        ///     Call the worker for the next available job
        /// </summary>
        public bool TryDoNextJob()
        {
            var job = NextJob;
            if ( job == null ) return false;

            // update lastAction
            job.Touch();

            // perform next job if no action was taken
            if ( !job.TryDoJob() ) return TryDoNextJob();

            return true;
        }

        /// <summary>
        ///     Normalize priorities
        /// </summary>
        private void CleanPriorities()
        {
            var orderedStack =
                _stack.OrderBy( mj => mj.priority ).ToList();
            for ( var i = 1; i <= _stack.Count; i++ ) orderedStack[i - 1].priority = i;
        }
    }
}