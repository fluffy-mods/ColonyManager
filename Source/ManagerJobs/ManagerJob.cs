// // Karel Kroeze
// // ManagerJob.cs
// // 2016-12-09

using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public abstract class ManagerJob : IManagerJob, IExposable
    {
        public static float LastUpdateRectWidth = 50f,
                            ProgressRectWidth = 10f,
                            StatusRectWidth = LastUpdateRectWidth + ProgressRectWidth;

        public int ActionInterval = 3600; // should be 1 minute.
        public int LastAction;

        public Manager manager;
        public int Priority;
        public Trigger Trigger;
        public ManagerJob() { } // scribe
        public ManagerJob( Manager manager ) { this.manager = manager; }
        public virtual bool Managed { get; set; }
        public virtual bool IsValid => true;
        public abstract string Label { get; }

        public virtual bool ShouldDoNow
            => Managed && !Suspended && !Completed && LastAction + ActionInterval < Find.TickManager.TicksGame;

        public virtual bool Suspended { get; set; } = false;
        public abstract bool Completed { get; }
        public abstract ManagerTab Tab { get; }
        public abstract string[] Targets { get; }
        public virtual SkillDef SkillDef { get; } = null;
        public abstract WorkTypeDef WorkTypeDef { get; }

        public virtual void ExposeData()
        {
            Scribe_References.LookReference( ref manager, "manager" );
            Scribe_Values.LookValue( ref ActionInterval, "ActionInterval" );
            Scribe_Values.LookValue( ref LastAction, "LastAction" );
            Scribe_Values.LookValue( ref Priority, "Priority" );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit || Manager.LoadSaveMode == Manager.Modes.ImportExport )
            {
                // must be true if it was saved.
                Managed = true;
            }
        }

        public abstract bool TryDoJob();
        public abstract void CleanUp();

        public virtual void Delete( bool cleanup = true )
        {
            if ( cleanup )
            {
                CleanUp();
            }
            Manager.For( manager ).JobStack.Delete( this );
        }

        public abstract void DrawListEntry( Rect rect, bool overview = true, bool active = true );
        public abstract void DrawOverviewDetails( Rect rect );
        public virtual void Tick() { }

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

        public void Touch() { LastAction = Find.TickManager.TicksGame; }
    }

    internal interface IManagerJob
    {
        bool TryDoJob();
    }
}
