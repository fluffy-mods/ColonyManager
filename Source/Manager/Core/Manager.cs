// Manager/Manager.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-05 22:59

using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FM
{
    public class Manager : MapComponent
    {
        public enum Modes
        {
            ImportExport,
            Normal
        }
                
        public static Modes      LoadSaveMode           = Modes.Normal;
        private List<ManagerTab> _managerTabsLeft;
        private List<ManagerTab> _managerTabsMiddle;
        private List<ManagerTab> _managerTabsRight;
        private JobStack         _stack;

        public List<ManagerTab> ManagerTabs = new List<ManagerTab>
        {
            new ManagerTab_Overview(),
            new ManagerTab_Production(),
            new ManagerTab_ImportExport(),
            new ManagerTab_Hunting(),
            new ManagerTab_Forestry(),
            new ManagerTab_Livestock(),
            new ManagerTab_Power()
        };

        public List<ManagerTab> ManagerTabsLeft
        {
            get
            {
                if ( _managerTabsLeft == null )
                {
                    _managerTabsLeft = ManagerTabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Left ).ToList();
                }
                return _managerTabsLeft;
            }
        }

        public List<ManagerTab> ManagerTabsMiddle
        {
            get
            {
                if ( _managerTabsMiddle == null )
                {
                    _managerTabsMiddle =
                        ManagerTabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Middle ).ToList();
                }
                return _managerTabsMiddle;
            }
        }

        public List<ManagerTab> ManagerTabsRight
        {
            get
            {
                if ( _managerTabsRight == null )
                {
                    _managerTabsRight = ManagerTabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Right ).ToList();
                }
                return _managerTabsRight;
            }
        }

        public JobStack JobStack => _stack ?? ( _stack = new JobStack() );

        // copypasta from AutoEquip.
        public static Manager Get
        {
            get
            {
                Manager getComponent =
                    Find.Map.components.OfType<Manager>().FirstOrDefault();
                if ( getComponent == null )
                {
                    getComponent = new Manager();
                    Find.Map.components.Add( getComponent );
                }

                return getComponent;
            }
        }

        public Manager()
        {
            _stack = new JobStack();
        }

        public override void ExposeData()
        {
            Scribe_Deep.LookDeep( ref _stack, "JobStack" );
            base.ExposeData();

            if ( _stack == null )
            {
                _stack = new JobStack();
            }
        }

        public void DoWork()
        {
#if DEBUG_JOBS
            Log.Message( "Trying to do work" );
#endif
            JobStack.TryDoNextJob();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // tick jobs
            foreach ( ManagerJob job in JobStack.FullStack() )
            {
                job.Tick();
            }

            // tick tabs
            foreach ( ManagerTab tab in ManagerTabs )
            {
                tab.Tick();
            }
#if DEBUG
            if ( Find.TickManager.TicksGame % 2000 == 0 )
#else
            if ( Find.TickManager.TicksGame % 10000 == 0 )
#endif
            {
                DoGlobalWork();
            }
        }

        private void DoGlobalWork()
        {
            // priority settings on worktables.
            DeepProfiler.Start( "Global work for production manager" );
            ManagerJob_Production.GlobalWork();
            DeepProfiler.End();

            // unforbid corpses.
            DeepProfiler.Start( "Global work for hunting manager" );
            ManagerJob_Hunting.GlobalWork();
            DeepProfiler.End();

            // clear turbine cells.
            DeepProfiler.Start( "Global work for forestry manager" );
            ManagerJob_Forestry.GlobalWork();
            DeepProfiler.End();
        }

        internal void NewJobStack( JobStack jobstack )
        {
            // clean up old jobs
            foreach ( ManagerJob job in _stack.FullStack() )
            {
                job.CleanUp();
            }

            // replace stack
            _stack = jobstack;

            // touch new jobs in inappropriate places
            foreach ( ManagerJob job in _stack.FullStack() )
            {
                job.Touch();
            }
        }
    }
}