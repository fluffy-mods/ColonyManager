// Manager/Trigger_PawnKind.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-27 16:55

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FM
{
    public class Trigger_PawnKind : Trigger
    {
        private Dictionary<ManagerJob_Livestock.AgeAndSex, Utilities.CachedValue<IEnumerable<Pawn>>> _allCache =
            ManagerJob_Livestock.AgeSexArray.ToDictionary( k => k, v => new Utilities.CachedValue<IEnumerable<Pawn>>() );

        public Dictionary<ManagerJob_Livestock.AgeAndSex, int> CountTargets;
        public PawnKindDef pawnKind;

        private Utilities.CachedValue<bool> _state = new Utilities.CachedValue<bool>(); 
        public override bool State
        {
            get
            {
                bool state;
                if ( !_state.TryGetValue( out state ) )
                {
                    state = ManagerJob_Livestock.AgeSexArray.All( ageSex => CountTargets[ageSex] == GetTame( ageSex ).Count ) && AllTrainingWantedSet();
                    _state.Update( state );
                }
                return state;
            }
        }

        public ManagerJob_Livestock Job
        {
            get
            {
                return Manager.Get.JobStack.FullStack<ManagerJob_Livestock>()
                              .FirstOrDefault( job => job.Trigger == this );
            }
        }

        private bool AllTrainingWantedSet()
        {   
            // loop through all set training targets, then through all animals to see if they're actually set. For the first that is not set, return false.
            // if the loop is completed, everything is set - return true.
            // This is rediculously expensive, and not meant to be called on tick - but as part of the cached Completed routine. 
            foreach ( TrainableDef def in Job.Training.Defs )
            {
                if ( Job.Training[def] )
                {
                    foreach( ManagerJob_Livestock.AgeAndSex ageSex in ManagerJob_Livestock.AgeSexArray )
                    {
                        foreach ( Pawn p in GetTame( ageSex ) )
                        {
                            if ( !p.training.GetWanted( def ) &&
                                 !p.training.IsCompleted( def ) )
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        public int[] Counts
        {
            get { return ManagerJob_Livestock.AgeSexArray.Select( ageSex => GetTame( ageSex ).Count ).ToArray(); }
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

        public bool PawnIsOfAgeSex( Pawn p, ManagerJob_Livestock.AgeAndSex ageSex )
        {
            // we're making the assumption here that anything with a lifestage index of 2 or greater is adult - so 3 lifestages.
            // this works for vanilla and all modded animals that I know off.
            
            switch ( ageSex )
            {
                case ManagerJob_Livestock.AgeAndSex.AdultFemale:
                    return p.gender == Gender.Female && p.ageTracker.CurLifeStageIndex >= 2;
                case ManagerJob_Livestock.AgeAndSex.AdultMale:
                    return p.gender == Gender.Male && p.ageTracker.CurLifeStageIndex >= 2;
                case ManagerJob_Livestock.AgeAndSex.JuvenileFemale:
                    return p.gender == Gender.Female && p.ageTracker.CurLifeStageIndex < 2;
                case ManagerJob_Livestock.AgeAndSex.JuvenileMale:
                default:
                    return p.gender == Gender.Male && p.ageTracker.CurLifeStageIndex < 2;
            }
        }

        public List<Pawn> GetTame( ManagerJob_Livestock.AgeAndSex ageSex )
        {
#if DEBUG_LIFESTOCK_COUNTS
            List<Pawn> tame = GetAll( ageSex ).Where( p => p.Faction == Faction.OfColony ).ToList();
            Log.Message( "Tamecount " + ageSex + ": " + tame.Count );
            return tame;
#else
            return GetAll( ageSex ).Where( p => p.Faction == Faction.OfColony ).ToList();
#endif
        }

        public IEnumerable<Pawn> GetAll( ManagerJob_Livestock.AgeAndSex ageSex )
        {
            // check if we have a cached version
            IEnumerable<Pawn> cached;
            if ( _allCache[ageSex].TryGetValue( out cached ) &&
                 cached != null )
            {
                return cached;
            }

            // if not, get a new list.
            cached = Find.ListerPawns.AllPawns
                         .Where( p => p.RaceProps.Animal // is animal
                                      && !p.Dead // is alive
                                      && p.kindDef == pawnKind // is our managed pawnkind
                                      && PawnIsOfAgeSex( p, ageSex ) ); // is of age and sex we want
            _allCache[ageSex].Update( cached );
#if DEBUG_LIFESTOCK_COUNTS
            Log.Message( "Allcount " + ageSex + ": " + cached.Count() );
#endif
            return cached;
        }

        public List<Pawn> GetWild( ManagerJob_Livestock.AgeAndSex ageSex )
        {
#if DEBUG_LIFESTOCK_COUNTS
            foreach (Pawn p in GetAll( ageSex )) Log.Message(p.Faction?.GetCallLabel() ?? "NULL" );
            List<Pawn> wild = GetAll( ageSex ).Where( p => p.Faction == null ).ToList();
            Log.Message( "Wildcount " + ageSex + ": " + wild.Count );
            return wild;
#else
            return GetAll( ageSex ).Where( p => p.Faction == null ).ToList();
#endif
        }

        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary( ref CountTargets, "Targets", LookMode.Value, LookMode.Value );
            Scribe_Defs.LookDef( ref pawnKind, "PawnKind" );
        }

        public override void DrawTriggerConfig( ref Vector2 cur, float width, float entryHeight, bool alt = false ) {}
    }
}