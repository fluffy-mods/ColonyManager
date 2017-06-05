// Karel Kroeze
// ManagerJob.cs
// 2016-12-09

using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    internal interface IManagerJob
    {
        #region Methods

        bool TryDoJob();

        #endregion Methods
    }

    public abstract class ManagerJob : IManagerJob, IExposable
    {
        #region Fields

        public static float LastUpdateRectWidth = 50f,
                            ProgressRectWidth = 10f,
                            StatusRectWidth = LastUpdateRectWidth + ProgressRectWidth;

        private int _actionInterval = Settings.DefaultUpdateInterval;

        public virtual int ActionInterval
        {
            get { return _actionInterval; }
            set { _actionInterval = value; }
        }

        // should be 1 minute.
        public int LastAction;

        public Manager manager;

        public int Priority;

        public Trigger Trigger;

        #endregion Fields

        #region Constructors

        public ManagerJob( Manager manager )
        {
            this.manager = manager;
        }

        #endregion Constructors



        #region Properties

        public abstract bool Completed { get; }
        public virtual bool IsValid => true;
        public abstract string Label { get; }
        public virtual bool Managed { get; set; }

        public virtual bool ShouldDoNow
            => Managed && !Suspended && !Completed && LastAction + ActionInterval < Find.TickManager.TicksGame;

        public virtual SkillDef SkillDef { get; } = null;
        public virtual bool Suspended { get; set; } = false;
        public abstract ManagerTab Tab { get; }
        public abstract string[] Targets { get; }
        public abstract WorkTypeDef WorkTypeDef { get; }

        #endregion Properties



        #region Methods

        public abstract void CleanUp();

        public virtual void Delete( bool cleanup = true )
        {
            if ( cleanup )
                CleanUp();
            Manager.For( manager ).JobStack.Delete( this, false );
        }

        public abstract void DrawListEntry( Rect rect, bool overview = true, bool active = true );

        public abstract void DrawOverviewDetails( Rect rect );

        public virtual void ExposeData()
        {
            Scribe_References.Look( ref manager, "manager" );
            Scribe_Values.Look( ref _actionInterval, "ActionInterval" );
            Scribe_Values.Look( ref LastAction, "LastAction" );
            Scribe_Values.Look( ref Priority, "Priority" );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit || Manager.LoadSaveMode == Manager.Modes.ImportExport )
            {
                // must be true if it was saved.
                Managed = true;
            }
        }

        public virtual void Tick()
        {
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            s.AppendLine( "Priority: " + Priority );
            s.AppendLine( "Active: " + Suspended );
            s.AppendLine( "LastAction: " + LastAction );
            s.AppendLine( "Interval: " + ActionInterval );
            s.AppendLine( "GameTick: " + Find.TickManager.TicksGame );
            return s.ToString();
        }

        public void Touch()
        {
            LastAction = Find.TickManager.TicksGame;
        }

        public abstract bool TryDoJob();

        #endregion Methods
    }
}
