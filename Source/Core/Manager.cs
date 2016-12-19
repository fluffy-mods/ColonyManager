// // Karel Kroeze
// // Manager.cs
// // 2016-12-09

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class Manager : MapComponent, ILoadReferenceable
    {
        public enum Modes
        {
            ImportExport,
            Normal
        }

        public static Modes LoadSaveMode = Modes.Normal;
        public static bool HelpShown;
        private List<ManagerTab> _managerTabsLeft;
        private List<ManagerTab> _managerTabsMiddle;
        private List<ManagerTab> _managerTabsRight;
        private bool _powerTabAdded = false;

        internal static bool _powerUnlocked = false;
        private JobStack _stack;

        public List<ManagerTab> ManagerTabs;

        public Manager( Map map ) : base( map )
        {
            _stack = new JobStack();
            ManagerTabs = new List<ManagerTab>
        {
            new ManagerTab_Overview( this ),
            new ManagerTab_Production( this ),
            new ManagerTab_ImportExport( this ),
            new ManagerTab_Hunting( this ),
            new ManagerTab_Forestry( this ),
            new ManagerTab_Livestock( this ),
            new ManagerTab_Foraging( this )
            // Power is added by Manager.UnlockPowerTab() after the appropriate research is done.
        };
        }

        public List<ManagerTab> ManagerTabsLeft
        {
            get
            {
                if ( _managerTabsLeft == null )
                {
                    _managerTabsLeft =
                        ManagerTabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Left && tab.Visible ).ToList();
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
                        ManagerTabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Middle && tab.Visible ).ToList();
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
                    _managerTabsRight =
                        ManagerTabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Right && tab.Visible ).ToList();
                }
                return _managerTabsRight;
            }
        }

        public JobStack JobStack => _stack ?? ( _stack = new JobStack() );

        public void RefreshTabs()
        {
            _managerTabsLeft = null;
            _managerTabsMiddle = null;
            _managerTabsRight = null;
        }

        public void AddPowerTabIfUnlocked()
        {
            if ( _powerUnlocked &&
                 !_powerTabAdded )
            {
                ManagerTabs.Add( new ManagerTab_Power( this ) );
                _powerTabAdded = true;
            }
        }

        public static implicit operator Map( Manager manager ) { return manager.map; }

        // copypasta from AutoEquip.
        public static Manager For( Map map )
        {
            var instance = map.GetComponent<Manager>();
            if ( instance != null )
                return instance;

            instance = new Manager( map );
            map.components.Add( instance );
            return instance;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // TODO: migrate HelpShown to HugsLib invisible setting.
            Scribe_Values.LookValue( ref HelpShown, "HelpShown", false );
            Scribe_Deep.LookDeep( ref _stack, "JobStack" );

            foreach ( ManagerTab tab in ManagerTabs )
            {
                var exposableTab = tab as IExposable;
                if ( exposableTab != null )
                {
                    Scribe_Deep.LookDeep( ref exposableTab, tab.Label );
                }
            }

            if ( _stack == null )
            {
                _stack = new JobStack();
            }
        }

        public bool DoWork() { return JobStack.TryDoNextJob(); }

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
            // TODO: Fix global work for production jobs.
            // ManagerJob_Production.GlobalWork();
            DeepProfiler.End();

            // clear turbine cells.
            DeepProfiler.Start( "Global work for forestry manager" );
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

            // touch new jobs in inappropriate places (reset timing so they are properly performed)
            foreach ( ManagerJob job in _stack.FullStack() )
            {
                job.Touch();
            }
        }

        public string GetUniqueLoadID() { return "Manager_" + map.GetUniqueLoadID(); }
    }
}
