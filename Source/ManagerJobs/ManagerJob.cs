// Karel Kroeze
// ManagerJob.cs
// 2016-12-09

using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FluffyManager
{
    internal interface IManagerJob
    {
        bool TryDoJob();
    }

    public abstract class ManagerJob : IManagerJob, IExposable
    {
        public static float SuspendStampWidth   = Constants.MediumIconSize,
                            LastUpdateRectWidth = 50f,
                            ProgressRectWidth   = 10f,
                            StatusRectWidth     = SuspendStampWidth + LastUpdateRectWidth + ProgressRectWidth;

        private int  _actionInterval = Settings.DefaultUpdateInterval;
        private bool _suspended;

        public bool CheckReachable = true;

        public int lastAction;

        public Manager manager;
        public bool    PathBasedDistance;

        public int priority;

        public Trigger Trigger;

        public ManagerJob( Manager manager )
        {
            this.manager = manager;
            Touch(); // set last updated to current time.
        }

        public virtual int ActionInterval
        {
            get => _actionInterval;
            set => _actionInterval = value;
        }

        public abstract bool   Completed { get; }
        public virtual  bool   IsValid   => true;
        public abstract string Label     { get; }
        public virtual  bool   Managed   { get; set; }


        public virtual bool ShouldDoNow => Managed && !Suspended && !Completed &&
                                           lastAction + ActionInterval < Find.TickManager.TicksGame;

        public virtual SkillDef SkillDef { get; } = null;

        public virtual bool Suspended
        {
            get => _suspended;
            set => _suspended = value;
        }

        public abstract ManagerTab  Tab         { get; }
        public abstract string[]    Targets     { get; }
        public abstract WorkTypeDef WorkTypeDef { get; }

        public virtual void ExposeData()
        {
            Scribe_References.Look( ref manager, "manager" );
            Scribe_Values.Look( ref _actionInterval, "ActionInterval" );
            Scribe_Values.Look( ref lastAction, "lastAction" );
            Scribe_Values.Look( ref priority, "priority" );
            Scribe_Values.Look( ref CheckReachable, "CheckReachable", true );
            Scribe_Values.Look( ref PathBasedDistance, "PathBasedDistance" );
            Scribe_Values.Look( ref _suspended, "Suspended" );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit || Manager.LoadSaveMode == Manager.Modes.ImportExport )
                // must be true if it was saved.
                Managed = true;
        }

        public abstract bool TryDoJob();

        public virtual bool IsReachable( Thing target )
        {
            return !target.Position.Fogged( manager.map )
                && ( !CheckReachable ||
                     manager.map.mapPawns.FreeColonistsSpawned.Any(
                         p => p.CanReach( target, PathEndMode.Touch, Danger.Some ) ) );
        }

        public virtual float Distance( Thing target, IntVec3 source )
        {
            if ( PathBasedDistance )
            {
                var path = target.Map.pathFinder.FindPath( source, target,
                                                           TraverseParms.For( TraverseMode.PassDoors, Danger.Some ),
                                                           PathEndMode.Touch );
                var cost = path.Found ? path.TotalCost : int.MaxValue;
                path.ReleaseToPool();
                return cost * 2;
            }

            return Mathf.Sqrt( source.DistanceToSquared( target.Position ) ) * 2;
        }

        public abstract void CleanUp();

        public virtual void Delete( bool cleanup = true )
        {
            if ( cleanup )
                CleanUp();
            Manager.For( manager ).JobStack.Delete( this, false );
        }

        public abstract void DrawListEntry( Rect rect, bool overview = true, bool active = true );

        public abstract void DrawOverviewDetails( Rect rect );

        public virtual void Tick()
        {
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            s.AppendLine( "Priority: "   + priority );
            s.AppendLine( "Active: "     + Suspended );
            s.AppendLine( "LastAction: " + lastAction );
            s.AppendLine( "Interval: "   + ActionInterval );
            s.AppendLine( "GameTick: "   + Find.TickManager.TicksGame );
            return s.ToString();
        }

        public void Touch()
        {
            lastAction = Find.TickManager.TicksGame;
        }
    }
}