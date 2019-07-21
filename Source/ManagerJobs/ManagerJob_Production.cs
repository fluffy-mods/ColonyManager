//// Karel Kroeze
//// ManagerJob_Production.cs
//// 2016-12-09

//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using Verse;

//#if DEBUG_JOBS

//using System.Text;

//#endif

//namespace FluffyManager
//{
//    public class ManagerJob_Production : ManagerJob
//    {
//        public static bool prioritizeManual = true;
//        internal bool _createIngredientBills;
//        internal bool _hasMeaningfulIngredientChoices;
//        private readonly float _margin = Utilities.Margin;
//        private bool _otherRecipeAvailable;
//        private int _recacheThreshold = 1000;
//        private string[] _targets;
//        private int _timeSinceLastOtherRecipeCheck;
//        private WorkTypeDef _workTypeDef;
//        public Bill_Production Bill;
//        public BillGiverTracker BillGivers;
//        public History History;
//        public MainProductTracker MainProduct;
//        public bool restrictToMaxSkill;
//        public List<RecipeDef> OtherRecipeDefs = new List<RecipeDef>();
//        public new Trigger_Threshold Trigger;

//#if DEBUG_JOBS
//        public static StringBuilder  debug                                  = new StringBuilder();
//#endif

//        public override bool Completed
//        {
//            get { return !Trigger.State; }
//        }

//        public override ManagerTab Tab
//        {
//            get { return manager.Tabs.Find( tab => tab is ManagerTab_Production ); }
//        }

//        public override bool IsValid
//        {
//            get
//            {
//                if ( Bill?.recipe == null )
//                    return false;

//                if ( !Bill.recipe.HasBuildingRecipeUser( manager, true ) || !Bill.recipe.AvailableNow )
//                    return false;

//                return true;
//            }
//        }

//        public override string Label
//        {
//            get { return Bill.recipe.LabelCap; }
//        }

//        public override string[] Targets
//        {
//            get
//            {
//                if ( _targets == null )
//                {
//                    _targets = Bill.recipe.GetRecipeUsers().Select( td => td.LabelCap ).ToArray();
//                }
//                return _targets;
//            }
//        }

//        public override WorkTypeDef WorkTypeDef
//        {
//            get
//            {
//                if ( _workTypeDef == null )
//                {
//                    // fetch the worktype def in the most convoluted way possible.
//                    // first get some examples of worktables our bill is on.
//                    List<Building_WorkTable> workTables = Bill.recipe.CurrentRecipeUsers( manager );

//                    // if none exist (yet), create a phony copy.
//                    if ( workTables.Count == 0 )
//                    {
//                        workTables.Add(
//                                       ThingMaker.MakeThing( Bill.recipe.GetRecipeUsers().First() ) as
//                                       Building_WorkTable );
//                    }

//                    // then loop through workgivers until we find one that matches.
//                    foreach ( WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading )
//                    {
//                        foreach ( WorkGiverDef workGiver in def.workGiversByPriority )
//                        {
//                            // we're only interested in the doBill scanner?
//                            var scanner = workGiver.Worker as WorkGiver_DoBill;
//                            if ( scanner == null )
//                            {
//                                continue;
//                            }
//#if DEBUG_OVERVIEW
//                            Log.Message( Bill.recipe.LabelCap + " > " + workGiver.defName + " > " + workGiver.Worker.GetType().ToString() );
//#endif

//                            // skip workgiver if it applies only to pawns (cooks are not repairers!)
//                            if ( workGiver.billGiversAllAnimals ||
//                                 workGiver.billGiversAllAnimalsCorpses ||
//                                 workGiver.billGiversAllHumanlikes ||
//                                 workGiver.billGiversAllHumanlikesCorpses ||
//                                 workGiver.billGiversAllMechanoids ||
//                                 workGiver.billGiversAllMechanoidsCorpses )
//                            {
//                                continue;
//                            }

//                            // skip workgiver if it doesn't assign work to our tables
//                            if ( !workTables.Any( workTable => scanner.PotentialWorkThingRequest.Accepts( workTable ) ) )
//                            {
//                                continue;
//                            }

//                            // if we got here that should hopefully mean we got a valid scanner for this table, and we now know what the worktype def is
//                            _workTypeDef = scanner.def.workType;
//                            return _workTypeDef;
//                        }
//                    }
//                }

//                return _workTypeDef;
//            }
//        }

//        public override SkillDef SkillDef
//        {
//            get { return Bill.recipe.workSkill; }
//        }

//        public ManagerJob_Production( Manager manager ) : base( manager )
//        {
//            // for scribe loading
//            if ( Scribe.mode == LoadSaveMode.Inactive )
//                Log.Error(
//                          "Scribe constructor called on ManagerJob_Production while not scribing. This should never happen!" );
//        }

//        public ManagerJob_Production( Manager manager, RecipeDef recipe ) : base( manager )
//        {
//            Bill = recipe.UsesUnfinishedThing ? new Bill_ProductionWithUft( recipe ) : new Bill_Production( recipe );
//            _hasMeaningfulIngredientChoices = Dialog_CreateJobsForIngredients.HasPrerequisiteChoices( manager, recipe );
//            MainProduct = new MainProductTracker( Bill.recipe );
//            Trigger = new Trigger_Threshold( this );
//            BillGivers = new BillGiverTracker( this );

//            History = new History( new[] { Trigger.ThresholdFilter.Summary } );
//        }

//        public void ForceRecacheOtherRecipe()
//        {
//            _timeSinceLastOtherRecipeCheck = _recacheThreshold;
//        }

//        public bool OtherRecipeAvailable()
//        {
//            // do we have a recently cached value?
//            if ( _timeSinceLastOtherRecipeCheck < _recacheThreshold )
//            {
//                // if so, return that
//                _timeSinceLastOtherRecipeCheck++;
//                return _otherRecipeAvailable;
//            }

//            // otherwise, get a list of recipes with our output that can be done on any of our workstations.
//            List<RecipeDef> recipes = DefDatabase<RecipeDef>
//                .AllDefsListForReading
//                .Where( rd => rd != Bill.recipe &&
//                              rd.products.Any( tc => tc.thingDef == MainProduct.ThingDef ) &&
//                              rd.HasBuildingRecipeUser( manager, true ) )
//                .ToList();

//            Log.Message( "Recipe count: " + recipes.Count );

//            // if there's anything in here, set the vars
//            if ( recipes.Count > 0 )
//            {
//                _otherRecipeAvailable = true;
//                _timeSinceLastOtherRecipeCheck = 0;
//                OtherRecipeDefs = recipes;
//            }
//            else
//            {
//                _otherRecipeAvailable = false;
//                _timeSinceLastOtherRecipeCheck = 0;
//                OtherRecipeDefs.Clear();
//            }
//            return _otherRecipeAvailable;
//        }

//        public void SetNewRecipe( RecipeDef newRecipe )
//        {
//            // clear currently assigned bills.
//            CleanUp();

//            // set the bill on this job
//            Bill = newRecipe.UsesUnfinishedThing
//                       ? new Bill_ProductionWithUft( newRecipe )
//                       : new Bill_Production( newRecipe );
//            _hasMeaningfulIngredientChoices = Dialog_CreateJobsForIngredients.HasPrerequisiteChoices( manager, newRecipe );

//            // mainproduct and trigger do not change.
//            BillGivers = new BillGiverTracker( this );

//            // set the last cache time so it gets updated.
//            ForceRecacheOtherRecipe();

//            // null targets cache so it gets updated
//            _targets = null;
//        }

//        /// <summary>
//        /// Sorting of bills
//        /// </summary>
//        public void GlobalWork()
//        {
//#if DEBUG_JOBS
//            var watch = Stopwatch.StartNew();
//            debug = new StringBuilder();
//            debug.AppendLine( "Manager :: Starting global priority check" );
//#endif

//            // get a list of all assigned bills, their worktables, and the priority of the job they belong to.
//            var all = new List<BillTablePriority>();
//            foreach ( ManagerJob_Production job in Manager.For( manager ).JobStack.FullStack<ManagerJob_Production>() )
//            {
//                var dump = false;
//                job.CleanBillgivers( ref dump );
//                all.AddRange(
//                             job.BillGivers.AssignedBillGiversAndBillsDictionary.Select(
//                                                                                        pair =>
//                                                                                        new BillTablePriority(
//                                                                                            pair.Key, pair.Value,
//                                                                                            job.priority ) ) );
//            }

//            // no assigned bills, nothing to do.
//            if ( all.Count == 0 )
//            {
//                return;
//            }

//#if DEBUG_JOBS
//            debug.AppendLine( "All currently managed bills:" );
//            foreach ( BillTablePriority entry in all )
//            {
//                debug.AppendLine( " - " + entry.ToString() );
//            }
//#endif

//            // loop through distinct worktables that have more than one bill
//            foreach (
//                Building_WorkTable table in
//                    all.Select( v => v.table ).Distinct().Where( table => table.BillStack.Count > 1 ) )
//            {
//                // get all bills (assigned by us) for this table, pre-ordered.
//                List<Bill> managerBills =
//                    all.Where( v => v.table == table ).OrderBy( v => v.priority ).Select( v => v.bill as Bill ).ToList();

//                // get all actual bills on the table (private field)
//                object rawBillsOnTable;
//                if (
//                    !Utilities.TryGetPrivateField( table.billStack.GetType(), table.billStack, "bills",
//                                                   out rawBillsOnTable ) )
//                {
//                    Log.Warning( "Failed to get real billstack for " + table );
//                    continue;
//                }

//                // give it it's type back.
//                var billsOnTable = rawBillsOnTable as List<Bill>;
//                if ( billsOnTable == null )
//                {
//                    Log.Warning( "Failed to convert real billstack for " + table );
//                    continue;
//                }

//#if DEBUG_JOBS
//                debug.AppendLine();
//                debug.AppendLine( "Bills for table " + table.GetUniqueLoadID() + " (managed):" );
//                foreach ( Bill bill in managerBills )
//                {
//                    debug.AppendLine( " - " + bill.GetUniqueLoadID() );
//                }
//                debug.AppendLine( "Bills for table " + table.GetUniqueLoadID() + " (on table):" );
//                foreach ( Bill bill in billsOnTable )
//                {
//                    debug.AppendLine( " - " + bill.GetUniqueLoadID() );
//                }
//#endif

//                // get the set difference of the two lists - these are external/manual bills
//                List<Bill> manualBills = billsOnTable.Except( managerBills ).ToList();

//                // create a new list of bills, by pasting the two lists together in the right order
//                var result = new List<Bill>();
//                if ( prioritizeManual )
//                {
//                    result.AddRange( manualBills );
//                    result.AddRange( managerBills );
//                }
//                else
//                {
//                    result.AddRange( managerBills );
//                    result.AddRange( manualBills );
//                }

//                // feed it back to the table.
//                if ( !Utilities.TrySetPrivateField( table.billStack.GetType(), table.billStack, "bills", result ) )
//                {
//                    Log.Warning( "Failed to set billstack for " + table );
//                }

//#if DEBUG_JOBS
//    // fetch the list again to see what happened.
//    // get all actual bills on the table (private field)
//                if ( !Utilities.TryGetPrivateField( table.billStack.GetType(), table.billStack, "bills", out rawBillsOnTable ) )
//                {
//                    Log.Warning( "Failed to get real billstack for " + table );
//                    continue;
//                }

//                // give it it's type back.
//                billsOnTable = rawBillsOnTable as List<Bill>;
//                if ( billsOnTable == null )
//                {
//                    Log.Warning( "Failed to convert real billstack for " + table );
//                    continue;
//                }

//                debug.AppendLine( "Bills for table " + table.GetUniqueLoadID() + " (after priority sort):" );
//                foreach ( Bill bill in billsOnTable )
//                {
//                    debug.AppendLine( " - " + bill.GetUniqueLoadID() );
//                }
//#endif
//            }

//#if DEBUG_JOBS
//            watch.Stop();
//            debug.AppendLine( "Execution time: " + watch.ElapsedMilliseconds + "ms" );
//            Log.Message( debug.ToString() );
//#endif
//        }

//        public override void ExposeData()
//        {
//            base.ExposeData();

//            Scribe_Deep.Look( ref Bill, "Bill" );

//            // init main product, required by trigger.
//            if ( MainProduct == null )
//            {
//                MainProduct = new MainProductTracker( Bill.recipe );
//            }

//            Scribe_Deep.Look( ref Trigger, "trigger", manager );
//            Scribe_Values.Look( ref _hasMeaningfulIngredientChoices, "hasMeaningFulIngredientChoices", false );
//            Scribe_Values.Look( ref _createIngredientBills, "createIngredientBills", true );

//            // bill giver tracking is going to error out in cross-map import/export, so create a new one.
//            if ( Manager.LoadSaveMode == Manager.Modes.Normal )
//            {
//                Scribe_Deep.Look( ref BillGivers, "BillGivers", this );
//            }
//            else
//            {
//                BillGivers = new BillGiverTracker( this );
//            }
//            Scribe_Values.Look( ref restrictToMaxSkill, "maxSkill", false );

//            // scribe history in normal load/save only.
//            if ( Manager.LoadSaveMode == Manager.Modes.Normal )
//            {
//                Scribe_Deep.Look( ref History, "History" );
//            }
//        }

//        /// <summary>
//        ///     Try to assign / clean up assignments
//        /// </summary>
//        /// <returns></returns>
//        public override bool TryDoJob()
//        {
//#if DEBUG_JOBS
//            var watch = Stopwatch.StartNew();
//            debug = new StringBuilder();
//            debug.AppendLine( "Manager :: Doing work for " + Bill.GetUniqueLoadID() );
//#endif

//            // flag to see if anything meaningful was done, if false at end, manager will also do next job.
//            var actionTaken = false;

//            if ( Trigger.State )
//            {
//#if DEBUG_JOBS
//                debug.AppendLine( "Job is ACTIVE, commencing work " );
//#endif

//                // BillGivers that we should work with.
//                List<Building_WorkTable> workers = BillGivers.SelectedBillGivers;

//                // Delete assigned bills on billgivers that are invalid (no longer allowed / deleted).
//                CleanBillgivers( ref actionTaken );

//                // check if there's places we need to add the bill.
//                for ( var workerIndex = 0; workerIndex < workers.Count; workerIndex++ )
//                {
//                    Building_WorkTable worker = workers[workerIndex];
//#if DEBUG_JOBS
//                    debug.AppendLine( "Checking workstation " + worker.GetUniqueLoadID() + " for changes to be made." );
//                    debug.AppendLine( "Current bills in billstack:" );
//                    foreach ( Bill bill in worker.BillStack )
//                    {
//                        bool match = BillGivers.AssignedBillGiversAndBillsDictionary.Contains(
//                                new KeyValuePair<Bill_Production, Building_WorkTable>( bill as Bill_Production, worker ) );
//                        debug.AppendLine( " - " + bill.GetUniqueLoadID() + ( match ? "<- match!" : "" ) );
//                    }
//#endif
//                    var billPresent = false;

//                    // loop over billstack to see if our bill is set.
//                    if ( worker.BillStack != null &&
//                         worker.BillStack.Count > 0 )
//                    {
//                        foreach ( Bill t in worker.BillStack )
//                        {
//                            var thatBill = t as Bill_Production;

//                            // if there is a bill, and it's managed by us, check to see if it's up-to-date.
//                            if ( thatBill != null &&
//                                 thatBill.recipe == Bill.recipe &&
//                                 BillGivers.AssignedBillGiversAndBillsDictionary.Contains(
//                                                                                          new KeyValuePair
//                                                                                              <Bill_Production,
//                                                                                              Building_WorkTable>(
//                                                                                              thatBill, worker ) ) )
//                            {
//                                billPresent = true;
//                                if ( thatBill.suspended != Bill.suspended ||
//                                     thatBill.repeatCount == 0 )
//                                {
//#if DEBUG_JOBS
//                                    debug.AppendLine( "Trying to unsuspend and/or bump targetCount" );
//#endif
//                                    thatBill.suspended = Bill.suspended;
//                                    thatBill.repeatCount = this.CountPerWorker( workerIndex );
//                                    actionTaken = true;
//                                }

//                                // update filters, modes, etc.
//                                Update( thatBill, ref actionTaken );
//                            }
//                        }
//                    }
//#if DEBUG_JOBS
//                    debug.AppendLine( "Billstack scanned, bill was " + ( billPresent ? "" : "not " ) + "set" );
//#endif

//                    // if bill wasn't present, add it.
//                    if ( !billPresent )
//                    {
//#if DEBUG_JOBS
//                        debug.AppendLine( "Trying to add bill" );
//#endif
//                        Bill_Production copy = Bill.Copy();
//                        copy.repeatMode = BillRepeatMode.RepeatCount;
//                        copy.repeatCount = this.CountPerWorker( workerIndex );
//                        worker.BillStack?.AddBill( copy );
//                        BillGivers.AssignedBillGiversAndBillsDictionary.Add( copy, worker );
//                        actionTaken = true;

//#if DEBUG_JOBS
//                        debug.AppendLine( "Worker (" + worker.GetUniqueLoadID() + ") billstack after work is done:" );
//                        foreach ( Bill bill in worker.BillStack )
//                        {
//                            debug.AppendLine( " - " + bill.GetUniqueLoadID() );
//                        }
//#endif
//                    }
//                }
//            }
//            else // Trigger false, clean up.
//            {
//#if DEBUG_JOBS
//                debug.AppendLine( "Job is INACTIVE, cleaning up." );
//#endif
//                CleanUp();
//            }

//#if DEBUG_JOBS
//            debug.AppendLine( "Done. " + ( actionTaken ? "Action" : "NO action" ) + " was taken." );
//            watch.Stop();
//            debug.AppendLine( "Execution time: " + watch.ElapsedMilliseconds + "ms" );
//            Log.Message( debug.ToString() );
//#endif

//            return actionTaken;
//        }

//        /// <summary>
//        ///     Delete outstanding managed jobs on billgivers that no longer meet criteria
//        /// </summary>
//        /// <param name="workers">Allowed workstations</param>
//        /// <param name="assignedBills">Managed bills/workstations</param>
//        /// <param name="actionTaken">Was anything done?</param>
//        private void CleanBillgivers( ref bool actionTaken )
//        {
//#if DEBUG_JOBS
//            debug.AppendLine( "Cleaning no longer allowed billgivers:" );
//#endif

//            // get list of workers, bills to iterate over
//            List<Building_WorkTable> workers = BillGivers.AssignedBillGivers;
//            Dictionary<Bill_Production, Building_WorkTable> assignedBills =
//                BillGivers.AssignedBillGiversAndBillsDictionary;
//            var bills = new List<Bill_Production>( assignedBills.Keys );

//            // check each assigned bill to see if it's in the list of allowed workers.
//            foreach ( Bill_Production bill in bills )
//            {
//                // check for each bill if it's still on the table
//                if ( assignedBills[bill].BillStack.IndexOf( bill ) < 0 )
//                {
//                    assignedBills.Remove( bill );
//                    continue;
//                }

//                if ( workers.Contains( assignedBills[bill] ) )
//                {
//                    continue;
//                }
//#if DEBUG_JOBS
//                debug.AppendLine( " - removing bill: " + bill.GetUniqueLoadID() + ", " +
//                                  assignedBills[bill].GetUniqueLoadID() );
//#endif
//                // remove from workstation billstack
//                assignedBills[bill].BillStack.Delete( bill );

//                // remove from managed list
//                assignedBills.Remove( bill );

//                // this counts as work
//                actionTaken = true;
//            }
//        }

//        /// <summary>
//        ///     update bill settings
//        /// </summary>
//        /// <param name="thatBill">Managed bill</param>
//        /// <param name="actionTaken">Any changes made?</param>
//        private void Update( Bill_Production thatBill, ref bool actionTaken )
//        {
//            if ( thatBill.storeMode != Bill.storeMode )
//            {
//#if DEBUG_JOBS
//                debug.AppendLine( "Updating Bill.storeMode" );
//#endif
//                thatBill.storeMode = Bill.storeMode;
//                actionTaken = true;
//            }

//            if ( thatBill.ingredientFilter != Bill.ingredientFilter )
//            {
//#if DEBUG_JOBS
//                debug.AppendLine( "Updating Bill.ingredientFilter" );
//#endif
//                thatBill.ingredientFilter = Bill.ingredientFilter;
//                actionTaken = true;
//            }

//            if ( Math.Abs( thatBill.ingredientSearchRadius - Bill.ingredientSearchRadius ) > 1 )
//            {
//#if DEBUG_JOBS
//                debug.AppendLine( "Updating Bill.ingredientSearchRadius" );
//#endif
//                thatBill.ingredientSearchRadius = Bill.ingredientSearchRadius;
//                actionTaken = true;
//            }

//            if ( thatBill.allowedSkillRange != Bill.allowedSkillRange )
//            {
//#if DEBUG_JOBS
//                debug.AppendLine( "Updating Bill.minSkillLevel" );
//#endif
//                thatBill.allowedSkillRange = Bill.allowedSkillRange;
//                actionTaken = true;
//            }
//        }

//        /// <summary>
//        ///     Delete all outstanding managed bills for this job.
//        /// </summary>
//        public override void CleanUp()
//        {
//            // remove managed bills from worktables.
//            var toBeDeleted = new List<Bill_Production>();
//            foreach (
//                KeyValuePair<Bill_Production, Building_WorkTable> pair in
//                    BillGivers.AssignedBillGiversAndBillsDictionary )
//            {
//                pair.Value.BillStack.Delete( pair.Key );
//#if DEBUG_JOBS
//                debug.AppendLine( " - deleting " + pair.Key.GetUniqueLoadID() + " from " + pair.Value.GetUniqueLoadID() );
//#endif
//            }

//            // clear out managed list
//#if DEBUG_JOBS
//            debug.AppendLine( "Clearing list of managed bills." );
//#endif
//            BillGivers.AssignedBillGiversAndBillsDictionary.Clear();
//        }

//        public override string ToString()
//        {
//            string strout = base.ToString();
//            strout += "\n" + Bill;
//            return strout;
//        }

//        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
//        {
//            // (detailButton) | name | (bar | last update)/(stamp) -> handled in Utilities.DrawStatusForListEntry
//            int shownTargets = overview ? 4 : 3; // there's more space on the overview

//            // set up rects
//            Rect labelRect = new Rect( _margin, _margin, rect.width -
//                                                         ( active ? StatusRectWidth + 4 * _margin : 2 * _margin ),
//                                       rect.height - 2 * _margin ),
//                 statusRect = new Rect( labelRect.xMax + _margin, _margin, StatusRectWidth, rect.height - 2 * _margin );

//            // create label string
//            string text = Label + "\n";
//            string subtext = string.Join( ", ", Targets );
//            if ( subtext.Fits( labelRect ) )
//                text += subtext.Italic();
//            else
//                text += "multiple".Translate().Italic();

//            // do the drawing
//            GUI.BeginGroup( rect );

//            // draw label
//            Utilities.Label( labelRect, text, subtext, TextAnchor.MiddleLeft, _margin );

//            // if the bill has a manager job, give some more info.
//            if ( active )
//            {
//                this.DrawStatusForListEntry( statusRect, Trigger );
//            }
//            GUI.EndGroup();
//        }

//        public override void DrawOverviewDetails( Rect rect )
//        {
//            History.DrawPlot( rect, Trigger.Count );
//        }

//        public override void Tick()
//        {
//            if ( Find.TickManager.TicksGame % 250 == 0 )
//            {
//                if ( restrictToMaxSkill )
//                {
//                    int highestSkill = manager.map.mapPawns.FreeColonistsSpawned.Max(
//                                                                                     pawn =>
//                                                                                     pawn.skills.GetSkill(
//                                                                                                          Bill.recipe
//                                                                                                              .workSkill )
//                                                                                         .Level );
//                    Bill.allowedSkillRange = new IntRange( highestSkill, highestSkill );
//                }
//            }
//            History.Update( Trigger.CurrentCount );
//        }

//        public struct BillTablePriority
//        {
//            public Bill_Production bill;
//            public Building_WorkTable table;
//            public int priority;

//            public BillTablePriority( Bill_Production bill, Building_WorkTable table, int priority )
//            {
//                this.bill = bill;
//                this.table = table;
//                this.priority = priority;
//            }

//            public override string ToString()
//            {
//                return "( bill: " + bill.GetUniqueLoadID() + ", table: " + table.GetUniqueLoadID() + ", priority: " +
//                       priority + " )";
//            }
//        }
//    }
//}

