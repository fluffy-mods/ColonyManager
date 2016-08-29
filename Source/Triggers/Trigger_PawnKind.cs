// Manager/Trigger_PawnKind.cs
//
// Copyright Karel Kroeze, 2015.
//
// Created 2015-11-27 16:55

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class Trigger_PawnKind : Trigger
    {
        #region Fields

        public Dictionary<Utilities_Livestock.AgeAndSex, int> CountTargets;
        public PawnKindDef pawnKind;

        private Utilities.CachedValue<bool> _state = new Utilities.CachedValue<bool>();

        #endregion Fields

        #region Constructors

        public Trigger_PawnKind()
        {
            CountTargets = Utilities_Livestock.AgeSexArray.ToDictionary( k => k, v => 5 );
        }

        #endregion Constructors

        #region Properties

        public int[] Counts
        {
            get { return Utilities_Livestock.AgeSexArray.Select( ageSex => pawnKind.GetTame( ageSex ).Count ).ToArray(); }
        }

        public ManagerJob_Livestock Job
        {
            get
            {
                return Manager.Get.JobStack.FullStack<ManagerJob_Livestock>()
                              .FirstOrDefault( job => job.Trigger == this );
            }
        }

        public override bool State
        {
            get
            {
                bool state;
                if ( !_state.TryGetValue( out state ) )
                {
                    state = Utilities_Livestock.AgeSexArray.All( ageSex => CountTargets[ageSex] == pawnKind.GetTame( ageSex ).Count ) && AllTrainingWantedSet();
                    _state.Update( state );
                }
                return state;
            }
        }

        public override string StatusTooltip
        {
            get
            {
                List<string> tooltipArgs = new List<string>();
                tooltipArgs.Add( pawnKind.LabelCap );
                tooltipArgs.AddRange( Counts.Select( x => x.ToString() ) );
                tooltipArgs.AddRange( CountTargets.Values.Select( v => v.ToString() ) );
                return "FML.ListEntryTooltip".Translate( tooltipArgs.ToArray() );
            }
        }

        #endregion Properties

        #region Methods

        public override void DrawTriggerConfig( ref Vector2 cur, float width, float entryHeight, bool alt = false, string label = null, string tooltip = null )
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary( ref CountTargets, "Targets", LookMode.Value, LookMode.Value );
            Scribe_Defs.LookDef( ref pawnKind, "PawnKind" );
        }

        private bool AllTrainingWantedSet()
        {
            // do a dry run of the training assignment (no assignments are set).
            // this is rediculously expensive, and should never be called on tick.
            bool actionTaken = false;
            Job.DoTrainingJobs( ref actionTaken, false );
            return actionTaken;
        }

        #endregion Methods
    }
}