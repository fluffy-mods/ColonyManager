// Manager/ManagerJob.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:29

using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FM
{
    public abstract class ManagerJob : IManagerJob, IExposable
    {
        public float LastUpdateRectWidth = 50f,
                     ProgressRectWidth = 10f;

        public int ActionInterval = 3600; // should be 1 minute.
        public int LastAction;
        public int Priority;

        public virtual bool Assigned { get; set; }

        public virtual bool IsValid
        {
            get { return true; }
        }

        public abstract string Label { get; }
        public bool ShouldDoNow => Assigned && !Suspended && !Completed && LastAction + ActionInterval < Find.TickManager.TicksGame;
        public virtual bool Suspended { get; set; } = false;

        public abstract bool Completed { get; }

        public abstract ManagerTab Tab { get; }

        public abstract string[] Targets { get; }
        public Trigger Trigger;

        public virtual void ExposeData()
        {
            Scribe_Values.LookValue( ref ActionInterval, "ActionInterval" );
            Scribe_Values.LookValue( ref LastAction, "LastAction" );
            Scribe_Values.LookValue( ref Priority, "Priority" );
        }

        public abstract bool TryDoJob();

        public abstract void CleanUp();

        public virtual void Delete( bool cleanup = true )
        {
            if ( cleanup )
            {
                CleanUp();
            }
            Manager.Get.JobStack.Delete( this );
        }

        public abstract void DrawListEntry( Rect rect, bool overview = true, bool active = true );

        public abstract void DrawOverviewDetails( Rect rect );

        public virtual SkillDef SkillDef { get; } = null;

        public abstract WorkTypeDef WorkTypeDef { get; }

        public virtual void Tick() {}

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
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
    }

    public interface IThreshold
    {
        
    }

    internal interface IManagerJob
    {
        bool TryDoJob();
    }
}