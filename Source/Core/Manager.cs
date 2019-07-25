// Karel Kroeze
// Manager.cs
// 2016-12-09

using System;
using System.Collections.Generic;
using System.Linq;
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
        public static bool  HelpShown;

        private List<ManagerTab> _managerTabsLeft;
        private List<ManagerTab> _managerTabsMiddle;
        private List<ManagerTab> _managerTabsRight;
        private JobStack         _stack;
        private int              id = -1;

        public List<ManagerTab> Tabs;

        public Manager( Map map ) : base( map )
        {
            _stack = new JobStack( this );
            Tabs = new List<ManagerTab>
            {
                new ManagerTab_Overview( this ),
                //new ManagerTab_Production( this ),
                //new ManagerTab_ImportExport( this ),
                new ManagerTab_Hunting( this ),
                new ManagerTab_Forestry( this ),
                new ManagerTab_Livestock( this ),
                new ManagerTab_Foraging( this ),
                new ManagerTab_Mining( this ),
                new ManagerTab_Power( this )
            };

            // if not created in SavingLoading, give yourself the ID of the map you were constructed on.
            if ( Scribe.mode == Verse.LoadSaveMode.Inactive ) id = map.uniqueID;
        }

        public List<ManagerTab> ManagerTabsLeft
        {
            get
            {
                if ( _managerTabsLeft == null )
                    _managerTabsLeft = Tabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Left ).ToList();
                return _managerTabsLeft;
            }
        }

        public List<ManagerTab> ManagerTabsMiddle
        {
            get
            {
                if ( _managerTabsMiddle == null )
                    _managerTabsMiddle =
                        Tabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Middle ).ToList();
                return _managerTabsMiddle;
            }
        }

        public List<ManagerTab> ManagerTabsRight
        {
            get
            {
                if ( _managerTabsRight == null )
                    _managerTabsRight =
                        Tabs.Where( tab => tab.IconArea == ManagerTab.IconAreas.Right ).ToList();
                return _managerTabsRight;
            }
        }

        public JobStack JobStack => _stack ?? ( _stack = new JobStack( this ) );

        public string GetUniqueLoadID()
        {
            return "ColonyManager_" + id;
        }

        public static implicit operator Map( Manager manager )
        {
            return manager.map;
        }

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
            Scribe_Values.Look( ref id, "id", -1, true );
            Scribe_Values.Look( ref HelpShown, "HelpShown" );
            Scribe_Deep.Look( ref _stack, "JobStack", this );

            foreach ( var tab in Tabs )
            {
                var exposableTab = tab as IExposable;
                if ( exposableTab != null ) Scribe_Deep.Look( ref exposableTab, tab.Label, this );
            }

            if ( _stack == null ) _stack = new JobStack( this );
        }

        public bool TryDoWork()
        {
            return JobStack.TryDoNextJob();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // tick jobs
            foreach ( var job in JobStack.FullStack() )
            {
                if ( !job.Suspended )
                {
                    try
                    {
                        job.Tick();
                    } catch ( Exception err ) {
                        Log.Error( $"Suspending manager job because it error-ed on tick: \n{err}" );
                    }
                }
            }

            // tick tabs
            foreach ( var tab in Tabs )
                tab.Tick();
        }


        internal void NewJobStack( JobStack jobstack )
        {
            // clean up old jobs
            foreach ( var job in _stack.FullStack() ) job.CleanUp();

            // replace stack
            _stack = jobstack;

            // touch new jobs in inappropriate places (reset timing so they are properly performed)
            foreach ( var job in _stack.FullStack() )
            {
                job.manager = this;
                job.Touch();
            }
        }
    }
}