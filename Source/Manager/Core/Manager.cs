using System.Linq;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using RimWorld;
using System;

// todo: implement reservations for managerjobs.

namespace FM
{
    public class Manager : MapComponent
    {
        public const float Margin = 6f;
        public const float ListEntryHeight = 50f;
        public static Texture2D OddRowBG = SolidColorMaterials.NewSolidColorTexture( 1f, 1f, 1f, .05f );
        public static Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
        public static Mode mode = Mode.normal;
        
        public enum Mode
        {
            importExport,
            normal
        }

        private JobStack _stack;

        public List<ManagerTab> ManagerTabs = new List<ManagerTab>()
        {
            new ManagerTab_Overview(),
            new ManagerTab_Production(),
            new ManagerTab_ImportExport(),
            new ManagerTab_Hunting(),
            new ManagerTab_Forestry()

            // TODO: new ManagerTabLifestock(),
            // TODO: new ManagerTabForestry()
        };

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

            foreach ( ManagerJob job in JobStack.FullStack )
            {
                job.Tick();
            }
        }

        internal void NewJobStack( JobStack jobstack )
        {
            // clean up old jobs
            foreach (ManagerJob job in _stack.FullStack )
            {
                job.CleanUp();
            }

            // replace stack 
            _stack = jobstack;

            // touch new jobs in inappropriate places
            foreach( ManagerJob job in _stack.FullStack )
            {
                job.Touch();
            }
        }
    }
}