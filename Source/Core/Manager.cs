// Karel Kroeze
// Manager.cs
// 2016-12-09

using RimWorld;
using System.Collections.Generic;
using System.Linq;
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

        internal static bool _powerUnlocked = false;
        private List<ManagerTab> _managerTabsLeft;
        private List<ManagerTab> _managerTabsMiddle;
        private List<ManagerTab> _managerTabsRight;
        private bool _powerTabAdded;
        private JobStack _stack;
        private int id = -1;

        public List<ManagerTab> ManagerTabs;

        public Manager( Map map ) : base( map )
        {
            _stack = new JobStack( this );
            ManagerTabs = new List<ManagerTab>
                          {
                              new ManagerTab_Overview( this ),
                              //new ManagerTab_Production( this ),
                              //new ManagerTab_ImportExport( this ),
                              new ManagerTab_Hunting( this ),
                              new ManagerTab_Forestry( this ),
                              new ManagerTab_Livestock( this ),
                              new ManagerTab_Foraging( this )
                              // Power is added by Manager.UnlockPowerTab() after the appropriate research is done.
                          };

            // if not created in SavingLoading, give yourself the ID of the map you were constructed on.
            if ( Scribe.mode == Verse.LoadSaveMode.Inactive )
            {
                id = map.uniqueID;
            }
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

        public JobStack JobStack => _stack ?? ( _stack = new JobStack( this ) );

        public string GetUniqueLoadID() { return "ColonyManager_" + id; }

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

        public static implicit operator Map( Manager manager )
        {
            return manager.map;
        }

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
            Scribe_Values.Look( ref id, "id", -1, true );
            Scribe_Values.Look( ref HelpShown, "HelpShown", false );
            Scribe_Deep.Look( ref _stack, "JobStack", this );

            foreach ( ManagerTab tab in ManagerTabs )
            {
                var exposableTab = tab as IExposable;
                if ( exposableTab != null )
                {
                    Scribe_Deep.Look( ref exposableTab, tab.Label, this );
                }
            }

            if ( _stack == null )
            {
                _stack = new JobStack( this );
            }
        }

        public bool TryDoWork()
        {
            return JobStack.TryDoNextJob();
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
            // clear turbine cells.
#if DEBUG_GLOBAL_WORK
            DeepProfiler.Start( "Global work for forestry manager" );
#endif
            // todo; move turbine clearing back here.
#if DEBUG_GLOBAL_WORK
            DeepProfiler.End();
#endif
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
                job.manager = this;
                job.Touch();
            }
        }
    }
}
