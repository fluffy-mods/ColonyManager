using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FM
{
    public class JobStack : IExposable
    {
        private List< ManagerJob > _stack;

        /// <summary>
        ///     Full jobstack, in order of priority
        /// </summary>
        public List< ManagerJob > FullStack
        {
            get { return _stack.OrderBy( mj => mj.Priority ).ToList(); }
        }

        /// <summary>
        ///     Jobstack of jobs that are available now
        /// </summary>
        public List< ManagerJob > CurStack
        {
            get { return _stack.Where( mj => mj.ShouldDoNow ).OrderBy( mj => mj.Priority ).ToList(); }
        }

        /// <summary>
        ///     Highest priority available job
        /// </summary>
        public ManagerJob NextJob => CurStack.DefaultIfEmpty( null ).FirstOrDefault();

        /// <summary>
        ///     Full jobstack, in order of assignment
        /// </summary>
        public JobStack()
        {
            _stack = new List< ManagerJob >();
        }

        public void ExposeData()
        {
            Scribe_Collections.LookList( ref _stack, "JobStack", LookMode.Deep );
        }

        /// <summary>
        ///     Call the worker for the next available job
        /// </summary>
        public void TryDoNextJob()
        {
            ManagerJob job = NextJob;
            if ( job == null )
            {
#if DEBUG_JOBS
                Log.Message( "Tried to do job, but _stack is empty" );
#endif
                return;
            }

            // update lastAction
            job.Touch();

            // perform next job if no action was taken
            if ( !job.TryDoJob() )
            {
                TryDoNextJob();
            }
        }

        public void Add( ManagerJob job )
        {
            job.Priority = _stack.Count + 1;
            _stack.Add( job );
        }

        public void Delete( ManagerJob job )
        {
            job.CleanUp();
            _stack.Remove( job );
            CleanPriorities();
        }

        private void CleanPriorities()
        {
            List< ManagerJob > orderedStack = _stack.OrderBy( mj => mj.Priority ).ToList();
            for ( int i = 1; i <= _stack.Count; i++ )
            {
                orderedStack[i - 1].Priority = i;
            }
        }

        public void SwitchPriorities( ManagerJob A, ManagerJob B )
        {
            int tmp = A.Priority;
            A.Priority = B.Priority;
            B.Priority = tmp;
        }

        public void IncreasePriority( ManagerJob job )
        {
            ManagerJob jobB = _stack.OrderByDescending( mj => mj.Priority ).First( mj => mj.Priority < job.Priority );
            SwitchPriorities( job, jobB );
        }

        public void DecreasePriority( ManagerJob job )
        {
            ManagerJob jobB = _stack.OrderBy( mj => mj.Priority ).First( mj => mj.Priority > job.Priority );
            SwitchPriorities( job, jobB );
        }

        public void TopPriority( ManagerJob job )
        {
            job.Priority = -1;
            CleanPriorities();
        }

        public void BottomPriority( ManagerJob job )
        {
            job.Priority = _stack.Count + 10;
            CleanPriorities();
        }

        internal void TopPriority< T >( T job ) where T : ManagerJob
        {
            // get list of priorities for this type.
            List< T > jobsOfType = _stack.OfType< T >().OrderBy( j => j.Priority ).ToList();
            List< int > priorities = jobsOfType.Select( j => j.Priority ).ToList();

            // make sure our job is on top.
            job.Priority = -1;

            // re-sort
            jobsOfType = jobsOfType.OrderBy( j => j.Priority ).ToList();

            // fill in priorities, making sure we don't affect other types.
            for ( int i = 0; i < jobsOfType.Count; i++ )
            {
                jobsOfType[i].Priority = priorities[i];
            }
        }

        internal void BottomPriority< T >( T job ) where T : ManagerJob
        {
            // get list of priorities for this type.
            List< T > jobsOfType = _stack.OfType< T >().OrderBy( j => j.Priority ).ToList();
            List< int > priorities = jobsOfType.Select( j => j.Priority ).ToList();

            // make sure our job is on the bottom.
            job.Priority = _stack.Count + 10;

            // re-sort
            jobsOfType = jobsOfType.OrderBy( j => j.Priority ).ToList();

            // fill in priorities, making sure we don't affect other types.
            for ( int i = 0; i < jobsOfType.Count; i++ )
            {
                jobsOfType[i].Priority = priorities[i];
            }
        }

        internal void DecreasePriority< T >( T job ) where T : ManagerJob
        {
            ManagerJob jobB = _stack.OfType< T >()
                                    .OrderBy( mj => mj.Priority )
                                    .First( mj => mj.Priority > job.Priority );
            SwitchPriorities( job, jobB );
        }

        internal void IncreasePriority< T >( T job ) where T : ManagerJob
        {
            ManagerJob jobB =
                _stack.OfType< T >().OrderByDescending( mj => mj.Priority ).First( mj => mj.Priority < job.Priority );
            SwitchPriorities( job, jobB );
        }
    }
}