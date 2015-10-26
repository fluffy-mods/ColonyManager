using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FM
{
    public class ManagerJobProduction : ManagerJob
    {
        private float _margin = Manager.Margin;
        private float _lastUpdateRectWidth = 100f;

        /// <summary>
        ///     The managed bill, basically a placeholder bill that gets copied and handed out
        /// </summary>
        public Bill_Production Bill;

        /// <summary>
        ///     BillGiver tracker, keeps track of billgiver settings and current assignments
        /// </summary>
        public BillGiverTracker BillGivers;

        public override void DrawOverviewSummary( Rect rect )
        {
            // detailButton | name (current/target) | last update

            Rect labelRect = new Rect( _margin, _margin,
                                       rect.width - _lastUpdateRectWidth - 3 * _margin,
                                       rect.height - 2 * _margin ),
                 lastUpdateRect = new Rect( labelRect.xMax + _margin, _margin,
                                            rect.width - labelRect.width - 3 * _margin,
                                            rect.height - 2 * _margin );

            GUI.BeginGroup(rect);
            try
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label( labelRect, MainProduct.Label + " (" + Trigger.CurCount + "/" + Trigger.Count + ")" );
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label( lastUpdateRect, ( Find.TickManager.TicksGame - LastAction ).TimeString() );
                Text.Anchor = TextAnchor.UpperLeft;
            }
            finally
            {
                GUI.EndGroup();
            }
        }
        // TODO: Production jobs that are based on the same bill point to the same reference
        // TODO: Hour should not be shown when date != 0 and hour == 0
        // TODO: Click on overview row links to correct managerTab
        // TODO: Collect data on counts
        // TODO: Draw count graph

        /// <summary>
        ///     Helpers to determine 'main' product of recipe and it's count, etc.
        /// </summary>
        public MainProductTracker MainProduct;

        public bool maxSkil;

        /// <summary>
        ///     Threshold for starting/stopping bill assignments
        /// </summary>
        public new TriggerThreshold Trigger;

        /// <summary>
        ///     Should we be handing out the bill?
        /// </summary>
        public override bool Active
        {
            get { return !Bill.suspended; }

            set { Bill.suspended = !value; }
        }

        public ManagerJobProduction( )
        {
            // for scribe loading
        }

        public ManagerJobProduction( RecipeDef recipe )
        {
            Bill = recipe.UsesUnfinishedThing ? new Bill_ProductionWithUft( recipe ) : new Bill_Production( recipe );
            MainProduct = new MainProductTracker( Bill.recipe );
            Trigger = new TriggerThreshold( this );
            BillGivers = new BillGiverTracker( this );
        }

        public override void ExposeData( )
        {
            base.ExposeData( );

            Scribe_Deep.LookDeep( ref Bill, "Bill" );
            Scribe_Deep.LookDeep( ref BillGivers, "BillGivers", this );
            Scribe_Values.LookValue( ref maxSkil, "maxSkill", false );

            // init main product, required by trigger.
            if ( MainProduct == null )
            {
                MainProduct = new MainProductTracker( Bill.recipe );
            }
            Scribe_Deep.LookDeep( ref Trigger, "Trigger", this );
        }

        /// <summary>
        ///     Try to assign / clean up assignments
        /// </summary>
        /// <returns></returns>
        public override bool TryDoJob( )
        {
#if DEBUG_JOBS
            Log.Message( "Starting job for Production Manager." );
            Log.Message( "Job: " + ToString( ) );
#endif

            // flag to see if anything meaningful was done, if false at end, manager will also do next job.
            var actionTaken = false;

            if ( Trigger.State )
            {
#if DEBUG_JOBS
                Log.Message( "Checking workers for presence of bills" );
#endif

                // BillGivers that we should work with.
                var workers = BillGivers.GetSelectedBillGivers;

                // clean up bills on workstations that do not meet selection criteria (area, count, etc) (anymore).
                CleanNoLongerAllowedBillgivers( workers, BillGivers.GetAssignedBillGiversAndBillsDictionary,
                                                ref actionTaken );

                // If Trigger met, check if there's places we need to add the bill.
                for ( var workerIndex = 0; workerIndex < workers.Count; workerIndex++ )
                {
                    var worker = workers[workerIndex];
#if DEBUG_JOBS
                    Log.Message( "Checking worker " + worker.LabelCap );
#endif
                    var billPresent = false;

                    // loop over workstations
                    if ( worker.BillStack != null && worker.BillStack.Count > 0 )
                    {
#if DEBUG_JOBS
                        foreach ( var pair in BillGivers.GetAssignedBillGiversAndBillsDictionary )
                        {
                            Log.Message( "saved" + pair.Key.GetUniqueLoadID( ) + " | " + pair.Value.GetUniqueLoadID( ) );
                        }
#endif

                        // loop over billstack to see if our bill is set.
                        // todo: targeted bill selection, if/when assignedbills will store properly.
                        foreach ( var t in worker.BillStack )
                        {
                            var thatBill = t as Bill_Production;
#if DEBUG_JOBS
                            if ( thatBill != null )
                            {
                                Log.Message( "real" + thatBill.GetUniqueLoadID( ) + " | " + worker.GetUniqueLoadID( ) );
                            }
#endif

                            // if there is a bill, and it's managed by us, check to see if it's up-to-date.
                            if ( thatBill != null && thatBill.recipe == Bill.recipe &&
                                 BillGivers.GetAssignedBillGiversAndBillsDictionary.Contains(
                                     new KeyValuePair<Bill_Production, Building_WorkTable>( thatBill, worker ) ) )
                            {
                                billPresent = true;
                                if ( thatBill.suspended != Bill.suspended || thatBill.repeatCount == 0 )
                                {
#if DEBUG_JOBS
                                    Log.Message( "Trying to unsuspend and/or bump targetCount" );
#endif
                                    thatBill.suspended = Bill.suspended;
                                    thatBill.repeatCount = this.CountPerWorker( workerIndex );
                                    actionTaken = true;
                                }

                                // update filters, modes, etc.
                                Update( thatBill, ref actionTaken );
                            }
                        }
                    }
#if DEBUG_JOBS
                    Log.Message( "Billstack scanned, bill was " + ( billPresent ? "" : "not " ) + "set" );
#endif

                    // if bill wasn't present, add it.
                    if ( !billPresent )
                    {
#if DEBUG_JOBS
                        Log.Message( "Trying to add bill" );
#endif
                        var copy = Bill.Copy( );
                        copy.repeatMode = BillRepeatMode.RepeatCount;
                        copy.repeatCount = this.CountPerWorker( workerIndex );
                        worker.BillStack?.AddBill( copy );
                        BillGivers.GetAssignedBillGiversAndBillsDictionary.Add( copy, worker );
                        actionTaken = true;
                    }
                }
            }
            else // Trigger false, clean up.
            {
                CleanUp( );
            }
            return actionTaken;
        }

        /// <summary>
        ///     Delete outstanding managed jobs on billgivers that no longer meet criteria
        /// </summary>
        /// <param name="workers">Allowed workstations</param>
        /// <param name="assignedBills">Assigned bills/workstations</param>
        /// <param name="actionTaken">Was anything done?</param>
        private void CleanNoLongerAllowedBillgivers( List<Building_WorkTable> workers,
                                                     Dictionary<Bill_Production, Building_WorkTable> assignedBills,
                                                     ref bool actionTaken )
        {
#if DEBUG_JOBS
            Log.Message( "Cleaning no longer allowed billgivers" );
#endif
            var toBeDeleted = new Dictionary<Bill_Production, Building_WorkTable>( );
            foreach ( var pair in assignedBills.Where( pair => !workers.Contains( pair.Value ) ) )
            {
#if DEBUG_JOBS
                Log.Message( "Deleting " + pair.Key.LabelCap + " from " + pair.Value.LabelCap );
#endif
                pair.Value.BillStack.Delete( pair.Key );
                toBeDeleted.Add( pair.Key, pair.Value );
                actionTaken = true;
            }
            foreach ( var pair in toBeDeleted )
            {
                assignedBills.Remove( pair.Key );
            }
        }

        /// <summary>
        ///     update bill settings
        /// </summary>
        /// <param name="thatBill">Managed bill</param>
        /// <param name="actionTaken">Any changes made?</param>
        private void Update( Bill_Production thatBill, ref bool actionTaken )
        {
            if ( thatBill.storeMode != Bill.storeMode )
            {
                thatBill.storeMode = Bill.storeMode;
                actionTaken = true;
            }

            if ( thatBill.ingredientFilter != Bill.ingredientFilter )
            {
                thatBill.ingredientFilter = Bill.ingredientFilter;
                actionTaken = true;
            }

            if ( Math.Abs( thatBill.ingredientSearchRadius - Bill.ingredientSearchRadius ) > 1 )
            {
                thatBill.ingredientSearchRadius = Bill.ingredientSearchRadius;
                actionTaken = true;
            }

            if ( thatBill.minSkillLevel != Bill.minSkillLevel )
            {
                thatBill.minSkillLevel = Bill.minSkillLevel;
                actionTaken = true;
            }
        }

        /// <summary>
        ///     Delete all outstanding managed bills for this job.
        /// </summary>
        public override void CleanUp( )
        {
#if DEBUG_JOBS
            Log.Message( "Cleaning up obsolete bills" );
#endif
            var toBeDeleted = new List<Bill_Production>( );
            foreach ( var pair in BillGivers.GetAssignedBillGiversAndBillsDictionary )
            {
                pair.Value.BillStack.Delete( pair.Key );
                toBeDeleted.Add( pair.Key );
#if DEBUG_JOBS
                Log.Message( "Checking worker " + pair.Value.LabelCap );
#endif
            }
            foreach ( var key in toBeDeleted )
            {
#if DEBUG_JOBS
                Log.Message( "Deleting bill " + key.LabelCap );
#endif
                BillGivers.GetAssignedBillGiversAndBillsDictionary.Remove( key );
            }
        }

        public override string ToString( )
        {
            var strout = base.ToString( );
            strout += "\n" + Bill;
            return strout;
        }

        public override void Tick( )
        {
            if ( Find.TickManager.TicksGame % 250 == 0 )
            {
                if ( maxSkil )
                {
                    Bill.minSkillLevel =
                        Find.ListerPawns.FreeColonistsSpawned.Max(
                            pawn => pawn.skills.GetSkill( Bill.recipe.workSkill ).level );
                }
            }
        }
    }
}