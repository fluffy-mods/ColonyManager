using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FM
{
    public enum AssignedBillGiverOptions
    {
        All,
        Count,
        Specific
    }

    public class BillGiverTracker : IExposable
    {
        private bool _assignedBillGiversInitialized = true;

        /// <summary>
        ///     Current list of assigned bill/worksations
        /// </summary>
        private Dictionary< Bill_Production, Building_WorkTable > _assignedBills;

        private List< string > _assignedBillsScribeID;

        private List< string > _assignedWorkersScribeID;

        private readonly ManagerJobProduction _job;

        /// <summary>
        ///     Area restriction
        /// </summary>
        public Area AreaRestriction;

        /// <summary>
        ///     Assignment mode for billgivers
        /// </summary>
        public AssignedBillGiverOptions BillGiverSelection = AssignedBillGiverOptions.All;

        /// <summary>
        ///     Specific billgivers set by user
        /// </summary>
        public List< Building_WorkTable > SpecificBillGivers;

        /// <summary>
        ///     User requested billgiver count, when using count assignment mode.
        /// </summary>
        public int UserBillGiverCount;

        public List< string > AssignedBillsScribe
        {
            get { return _assignedBills.Keys.Select( b => b.GetUniqueLoadID() ).ToList(); }
            set { _assignedBillsScribeID = value; }
        }

        public List< string > AssignedWorkersScribe
        {
            get { return _assignedBills.Values.Select( b => b.GetUniqueLoadID() ).ToList(); }
            set { _assignedWorkersScribeID = value; }
        }

        public RecipeDef Recipe { get; }

        /// <summary>
        ///     All potential billgivers count
        /// </summary>
        public int AllBillGiverCount => GetBillGiverDefs.Count;

        /// <summary>
        ///     Currently allowed billgivers count (these do not necessarily actually have the bill)
        /// </summary>
        public int CurBillGiverCount => GetSelectedBillGivers.Count;

        /// <summary>
        ///     All billgiver defs (by recipe).
        /// </summary>
        public List< ThingDef > GetBillGiverDefs => Recipe.GetRecipeUsers();

        /// <summary>
        ///     Get workstations that can perform the current bill/recipe (nothwithstanding area/count restrictions etc).
        /// </summary>
        public List< Building_WorkTable > GetPotentialBillGivers => Recipe.GetCurrentRecipeUsers();

        /// <summary>
        ///     Get workstations that can perform the current bill/recipe, and meet selection criteria set by player.
        /// </summary>
        /// <returns></returns>
        public List< Building_WorkTable > GetSelectedBillGivers
        {
            get
            {
                List< Building_WorkTable > list = Recipe.GetCurrentRecipeUsers();

                switch ( BillGiverSelection )
                {
                    case AssignedBillGiverOptions.Count:
                        if ( AreaRestriction != null )
                        {
                            list = list.Where( bw => AreaRestriction.ActiveCells.Contains( bw.Position ) ).ToList();
                        }
                        list = list.Take( UserBillGiverCount ).ToList();
                        break;
                    case AssignedBillGiverOptions.Specific:
                        list = SpecificBillGivers;
                        break;
                    case AssignedBillGiverOptions.All:
                    default:
                        break;
                }

                return list;
            }
        }

        public Dictionary< Bill_Production, Building_WorkTable > GetAssignedBillGiversAndBillsDictionary
        {
            get
            {
                if ( !_assignedBillGiversInitialized )
                {
                    bool error = false;
                    _assignedBills = new Dictionary< Bill_Production, Building_WorkTable >();
                    for ( int i = 0; i < _assignedBillsScribeID.Count; i++ )
                    {
#if DEBUG_SCRIBE
                        Log.Message( "Trying to find " + _assignedWorkersScribeID[i] + " | " + _assignedBillsScribeID[i] );
                        if ( Recipe != null )
                        {
                            Log.Message( "Recipe: " + Recipe.label );
                        }
                        Log.Message( Recipe.GetCurrentRecipeUsers().Count.ToString() );
#endif
                        try
                        {
                            Building_WorkTable worker = Recipe.GetCurrentRecipeUsers().DefaultIfEmpty( null )
                                                              .FirstOrDefault(
                                                                  b =>
                                                                      b.GetUniqueLoadID() == _assignedWorkersScribeID[i] );
                            Bill_Production bill = null;
                            if ( worker == null )
                            {
                                throw new Exception( "worker not found" );
                            }
                            if ( worker.billStack == null )
                            {
                                throw new Exception( "Billstack not initialized" );
                            }
                            for ( int j = 0; j < worker.billStack.Count; j++ )
                            {
                                if ( worker.billStack[j].GetUniqueLoadID() == _assignedBillsScribeID[i] )
                                {
                                    bill = (Bill_Production) worker.billStack[j];
                                }
                            }
                            if ( bill == null )
                            {
                                throw new Exception( "Bill not found" );
                            }
                            _assignedBills.Add( bill, worker );
                        }
                        catch ( Exception e )
                        {
                            error = true;
#if DEBUG_SCRIBE
                            Log.Warning( e.ToString() );
#endif
                        }
                    }

                    if ( !error )
                    {
                        _assignedBillGiversInitialized = true;
                    }
                }

                return _assignedBills;
            }
        }

        /// <summary>
        ///     Get workstations to which a bill was actually assigned
        /// </summary>
        public List< Building_WorkTable > GetAssignedBillGivers
        {
            get { return GetAssignedBillGiversAndBillsDictionary.Values.ToList(); }
        }

        public WindowBillGiverDetails DetailsWindow
        {
            get
            {
                WindowBillGiverDetails window = new WindowBillGiverDetails
                {
                    Job = _job,
                    closeOnClickedOutside = true,
                    draggable = true
                };
                return window;
            }
        }

        public BillGiverTracker( ManagerJobProduction job )
        {
            Recipe = job.Bill.recipe;
            _job = job;
            _assignedBills = new Dictionary< Bill_Production, Building_WorkTable >();
            SpecificBillGivers = new List< Building_WorkTable >();
        }

        public void ExposeData()
        {
            if ( Scribe.mode == LoadSaveMode.Saving )
            {
                _assignedWorkersScribeID = _assignedBills.Values.Select( b => b.GetUniqueLoadID() ).ToList();
                _assignedBillsScribeID = _assignedBills.Keys.Select( b => b.GetUniqueLoadID() ).ToList();
            }

            Scribe_Values.LookValue( ref BillGiverSelection, "BillGiverSelection" );
            Scribe_Values.LookValue( ref UserBillGiverCount, "UserBillGiverCount" );
            Scribe_References.LookReference( ref AreaRestriction, "AreaRestriction" );
            Scribe_Collections.LookList( ref _assignedBillsScribeID, "AssignedBills", LookMode.Value );
            Scribe_Collections.LookList( ref _assignedWorkersScribeID, "AssignedWorkers", LookMode.Value );
            Scribe_Collections.LookList( ref SpecificBillGivers, "SpecificBillGivers", LookMode.MapReference );

            // rather complicated post-load workaround to find buildings by unique ID, since the scribe won't do things the simple way.
            // i.e. scribing dictionary with reference keys and values does not appear to work.
            // since buildings dont appear in the standard finding methods at this point, set a flag to initialize assignedbillgivers the next time Assigned bill givers is called.
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                _assignedBillGiversInitialized = false;
            }
        }

        /// <summary>
        ///     Draw billgivers info + details button
        /// </summary>
        /// <param name="listing"></param>
        public void DrawBillGiverConfig( ref Listing_Standard listing )
        {
            listing.DoGap( 24f );

            // workstation info
            listing.DoLabel( "FMP.BillGivers".Translate() );
            listing.DoLabel( "FMP.BillGiversCount".Translate( GetPotentialBillGivers.Count, GetSelectedBillGivers.Count,
                                                              GetAssignedBillGivers.Count ) );

            string potentialString = string.Join( "\n", GetPotentialBillGivers.Select( b => b.LabelCap ).ToArray() );
            string assignedString = string.Join( "\n", GetSelectedBillGivers.Select( b => b.LabelCap ).ToArray() );
            string stationsTooltip = "FMP.BillGiversTooltip".Translate( potentialString, assignedString );

            // todo, fix that tooltip. Possible?
            // TooltipHandler.TipRegion(stations, stationsTooltip);

            // workstation selector
            if ( listing.DoTextButton( "FMP.BillGiversDetails".Translate() ) )
            {
                Find.WindowStack.Add( DetailsWindow );
            }
        }
    }
}