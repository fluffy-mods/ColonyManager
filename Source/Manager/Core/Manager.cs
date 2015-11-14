// Manager/Manager.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-05 22:59

using System.Collections.Generic;
using System.Linq;
using Verse;

// todo: implement reservations for managerjobs.

namespace FM
{
    public class Manager : MapComponent
    {
        public enum Modes
        {
            ImportExport,
            Normal
        }

        public static Modes Mode = Modes.Normal;

        private List< ManagerTab > _managerTabsLeft;
        private List< ManagerTab > _managerTabsMiddle;
        private List< ManagerTab > _managerTabsRight;

        private JobStack _stack;

        public List< ManagerTab > ManagerTabs = new List< ManagerTab >
        {
            new ManagerTab_Overview(),
            new ManagerTab_Production(),
            new ManagerTab_ImportExport(),
            new ManagerTab_Hunting(),
            new ManagerTab_Forestry()

            // TODO: new ManagerTabLifestock(),
        };

        public List< ManagerTab > ManagerTabsLeft
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

        public List< ManagerTab > ManagerTabsMiddle
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

        public List< ManagerTab > ManagerTabsRight
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
                    Find.Map.components.OfType< Manager >().FirstOrDefault();
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

            foreach ( ManagerJob job in JobStack.FullStack() )
            {
                job.Tick();
            }
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