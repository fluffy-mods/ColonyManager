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
        private readonly float _lastUpdateRectWidth = 50f,
                               _progressRectWidth = 10f,
                               _margin = Manager.Margin;
        private static int     _histSize = 100;
        private Texture2D      _cogTex = ContentFinder<Texture2D>.Get("UI/Buttons/Cog");
        public static ManagerTab ManagerTab = new ManagerTab_Production();

        public override Texture2D Icon
        {
            get
            {
                return base.Icon;
            }
        }

        public override ManagerTab Tab
        {
            get
            {
                return ManagerTab;
            }
        }

        /// <summary>
        ///     The managed bill, basically a placeholder bill that gets copied and handed out
        /// </summary>
        public Bill_Production Bill;

        /// <summary>
        ///     BillGiver tracker, keeps track of billgiver settings and current assignments
        /// </summary>
        public BillGiverTracker BillGivers;
        
        // TODO: Hour should not be shown when date != 0 and hour == 0
        // TODO: Collect data on counts ( three sets of data of set max size; day, month, year - each with max 100 datapoints)
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

        public History day = new History(_histSize);
        public History month = new History(_histSize, History.period.month);
        public History year = new History(_histSize, History.period.year);
        public History historyShown;

        public ManagerJobProduction()
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

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true)
        {
            // detailButton | name (current/target) | last update

            Rect labelRect = new Rect( _margin, _margin,
                                       rect.width - (active ? (_lastUpdateRectWidth + _progressRectWidth + 4 * _margin) : 2 * _margin),
                                       rect.height - 2 * _margin ),
                 progressRect = new Rect( labelRect.xMax + _margin, _margin,
                                            _progressRectWidth,
                                            rect.height - 2 * _margin ),
                 lastUpdateRect = new Rect( progressRect.xMax + _margin, _margin,
                                            _lastUpdateRectWidth,
                                            rect.height - 2 * _margin );

            string text = Bill.recipe.LabelCap + "\n<i>" +
                          string.Join( ", ", BillGivers.GetBillGiverDefs.Select( bg => bg.LabelCap ).ToArray() ) + "</i>";

#if DEBUG
            text += Priority;
#endif

            GUI.BeginGroup( rect );
            try
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label( labelRect, text );
                // if the bill has a manager job, give some more info.
                if ( active )
                {
                    // bar always goes a little beyond the actual target
                    int max = Math.Max((int)(Trigger.Count * 1.2f), Trigger.CurCount);

                    // get the bar rect
                    float barHeight = progressRect.height / max * Trigger.CurCount;
                    float markHeight = progressRect.height / max * Trigger.Count;
                    Rect progressBarRect = new Rect(progressRect.xMin + 1f, progressRect.yMax - barHeight, 6f, barHeight);

                    // draw a box for the bar
                    GUI.color = Color.gray;
                    Widgets.DrawBox( progressRect.ContractedBy( 1f ) );
                    GUI.color = Color.white;

                    // draw the bar
                    // if the job is active and pending, make the bar blueish green - otherwise white.
                    // Note; active here means the job is currently being handed out.
                    Color barColour = this.Active ? new Color( 0.2f, 0.8f, 0.85f ) : new Color( 1f, 1f, 1f );
                    Texture2D barTex = SolidColorMaterials.NewSolidColorTexture(barColour);
                    GUI.DrawTexture( progressBarRect, barTex );

                    // draw a mark at the treshold
                    Widgets.DrawLineHorizontal( progressRect.xMin, progressRect.yMax - markHeight, progressRect.width );
                    
                    // draw time since last action
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label( lastUpdateRect, ( Find.TickManager.TicksGame - LastAction ).TimeString() );

                    // set tooltips
                    TooltipHandler.TipRegion( progressRect, "FMP.ThresholdCount".Translate( Trigger.CurCount, Trigger.Count ) );
                    TooltipHandler.TipRegion( lastUpdateRect, "FM.LastUpdateTooltip".Translate( ( Find.TickManager.TicksGame - LastAction ).TimeString() ) );
                }
            }
            finally
            {
                // make sure everything is always properly closed / reset to defaults.
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.EndGroup();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

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
        public override bool TryDoJob()
        {
#if DEBUG_JOBS
            Log.Message( "Starting job for Production Manager." );
            Log.Message( "Job: " + ToString() );
#endif

            // flag to see if anything meaningful was done, if false at end, manager will also do next job.
            bool actionTaken = false;

            if ( Trigger.State )
            {
#if DEBUG_JOBS
                Log.Message( "Checking workers for presence of bills" );
#endif

                // BillGivers that we should work with.
                List< Building_WorkTable > workers = BillGivers.GetSelectedBillGivers;

                // clean up bills on workstations that do not meet selection criteria (area, count, etc) (anymore).
                CleanNoLongerAllowedBillgivers( workers, BillGivers.GetAssignedBillGiversAndBillsDictionary,
                                                ref actionTaken );

                // If Trigger met, check if there's places we need to add the bill.
                for ( int workerIndex = 0; workerIndex < workers.Count; workerIndex++ )
                {
                    Building_WorkTable worker = workers[workerIndex];
#if DEBUG_JOBS
                    Log.Message( "Checking worker " + worker.LabelCap );
#endif
                    bool billPresent = false;

                    // loop over workstations
                    if ( worker.BillStack != null && worker.BillStack.Count > 0 )
                    {
#if DEBUG_JOBS
                        foreach (
                            KeyValuePair< Bill_Production, Building_WorkTable > pair in
                                BillGivers.GetAssignedBillGiversAndBillsDictionary )
                        {
                            Log.Message( "saved" + pair.Key.GetUniqueLoadID() + " | " + pair.Value.GetUniqueLoadID() );
                        }
#endif

                        // loop over billstack to see if our bill is set.
                        foreach ( Bill t in worker.BillStack )
                        {
                            Bill_Production thatBill = t as Bill_Production;
#if DEBUG_JOBS
                            if ( thatBill != null )
                            {
                                Log.Message( "real" + thatBill.GetUniqueLoadID() + " | " + worker.GetUniqueLoadID() );
                            }
#endif

                            // if there is a bill, and it's managed by us, check to see if it's up-to-date.
                            if ( thatBill != null && thatBill.recipe == Bill.recipe &&
                                 BillGivers.GetAssignedBillGiversAndBillsDictionary.Contains(
                                     new KeyValuePair< Bill_Production, Building_WorkTable >( thatBill, worker ) ) )
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
                        Bill_Production copy = Bill.Copy();
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
                CleanUp();
            }
            return actionTaken;
        }

        /// <summary>
        ///     Delete outstanding managed jobs on billgivers that no longer meet criteria
        /// </summary>
        /// <param name="workers">Allowed workstations</param>
        /// <param name="assignedBills">Assigned bills/workstations</param>
        /// <param name="actionTaken">Was anything done?</param>
        private void CleanNoLongerAllowedBillgivers( List< Building_WorkTable > workers,
                                                     Dictionary< Bill_Production, Building_WorkTable > assignedBills,
                                                     ref bool actionTaken )
        {
#if DEBUG_JOBS
            Log.Message( "Cleaning no longer allowed billgivers" );
#endif
            Dictionary< Bill_Production, Building_WorkTable > toBeDeleted =
                new Dictionary< Bill_Production, Building_WorkTable >();
            foreach (
                KeyValuePair< Bill_Production, Building_WorkTable > pair in
                    assignedBills.Where( pair => !workers.Contains( pair.Value ) ) )
            {
#if DEBUG_JOBS
                Log.Message( "Deleting " + pair.Key.LabelCap + " from " + pair.Value.LabelCap );
#endif
                pair.Value.BillStack.Delete( pair.Key );
                toBeDeleted.Add( pair.Key, pair.Value );
                actionTaken = true;
            }
            foreach ( KeyValuePair< Bill_Production, Building_WorkTable > pair in toBeDeleted )
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
        public override void CleanUp()
        {
#if DEBUG_JOBS
            Log.Message( "Cleaning up obsolete bills" );
#endif
            List< Bill_Production > toBeDeleted = new List< Bill_Production >();
            foreach (
                KeyValuePair< Bill_Production, Building_WorkTable > pair in
                    BillGivers.GetAssignedBillGiversAndBillsDictionary )
            {
                pair.Value.BillStack.Delete( pair.Key );
                toBeDeleted.Add( pair.Key );
#if DEBUG_JOBS
                Log.Message( "Checking worker " + pair.Value.LabelCap );
#endif
            }
            foreach ( Bill_Production key in toBeDeleted )
            {
#if DEBUG_JOBS
                Log.Message( "Deleting bill " + key.LabelCap );
#endif
                BillGivers.GetAssignedBillGiversAndBillsDictionary.Remove( key );
            }
        }

        public override string ToString()
        {
            string strout = base.ToString();
            strout += "\n" + Bill;
            return strout;
        }

        public override void DrawOverviewDetails( Rect rect )
        {
            if (historyShown == null )
            {
                historyShown = day;
            }
            historyShown.DrawPlot( rect, Trigger.Count );

            Rect switchRect = new Rect(rect.xMax - 16f - _margin, rect.yMin + _margin, 16f, 16f);
            Widgets.DrawHighlightIfMouseover( switchRect );
            if(Widgets.ImageButton( switchRect, _cogTex ) )
            {
                List<FloatMenuOption> options = new List<FloatMenuOption> {
                    new FloatMenuOption("day", delegate { historyShown = day; } ),
                    new FloatMenuOption("month", delegate { historyShown = month; } ),
                    new FloatMenuOption("year", delegate { historyShown = year; })
                };
                Find.WindowStack.Add( new FloatMenu( options ) );
            }
        }

        public override void Tick()
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
            if ( Find.TickManager.TicksGame % day.Interval == 0 )
            {
                day.Add( Trigger.CurCount );
            }
            if( Find.TickManager.TicksGame % month.Interval == 0 )
            {
                month.Add( Trigger.CurCount );
            }
            if( Find.TickManager.TicksGame % year.Interval == 0 )
            {
                year.Add( Trigger.CurCount );
            }
        }
    }
}