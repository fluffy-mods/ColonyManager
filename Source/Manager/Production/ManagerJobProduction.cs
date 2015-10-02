using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FM
{
    public class ManagerJobProduction : ManagerJob
    {
        public ManagerJobProduction()
        {
            // for scribe loading only?
        }

        public ManagerJobProduction(RecipeDef recipe)
        {
            Bill = recipe.UsesUnfinishedThing ? new Bill_ProductionWithUft(recipe) : new Bill_Production(recipe);
            MainProduct = new MainProductTracker(Bill.recipe);
            Trigger = new TriggerThreshold(this);
            BillGivers = new BillGiverTracker(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.LookDeep(ref Bill, "Bill");
            Scribe_Deep.LookDeep(ref BillGivers, "BillGivers", this, true);
            // init main product, required by trigger.
            if (MainProduct == null) MainProduct = new MainProductTracker(Bill.recipe);
            Scribe_Deep.LookDeep(ref Trigger, "Trigger", this);

        }

        public BillGiverTracker BillGivers;

        public MainProductTracker MainProduct;

        public Bill_Production Bill;

        public new TriggerThreshold Trigger;

        public override bool Active
        {
            get
            {
                return !Bill.suspended;
            }

            set
            {
                Bill.suspended = !value;
            }
        }

        public override bool TryDoJob()
        {
#if DEBUG_JOBS
            Log.Message("Starting job for Production Manager.");
            Log.Message("Job: " + this.ToString());
#endif

            // flag to see if anything had to be done.
            bool actionTaken = false;

            if (Trigger.State)
            {
#if DEBUG_JOBS
                Log.Message("Checking workers for presence of bills");
#endif
                List<Building_WorkTable> workers = BillGivers.GetSelectedBillGivers;
                CleanNoLongerAllowedBillgivers(workers, BillGivers.AssignedBills, ref actionTaken);

                // If Trigger met, check if there's places we need to add the bill.
                for (int workerIndex = 0; workerIndex < workers.Count; workerIndex++)
                {
                    Building_WorkTable worker = workers[workerIndex];
#if DEBUG_JOBS
                    Log.Message("Checking worker " + worker.LabelCap);
#endif
                    bool billPresent = false;
                    if (worker.BillStack != null && worker.BillStack.Count > 0)
                    {
                        foreach (KeyValuePair<Bill_Production, Building_WorkTable> pair in BillGivers.AssignedBills)
                        {
                            Log.Message("saved" + pair.Key.GetUniqueLoadID() + " | " + pair.Value.GetUniqueLoadID());
                        }
                        foreach (Bill t in worker.BillStack)
                        {
                            Bill_Production thatBill = t as Bill_Production;
#if DEBUG_JOBS
                            if (thatBill != null)
                            {
                                Log.Message("real" + thatBill.GetUniqueLoadID() + " | " + worker.GetUniqueLoadID());
                            }
#endif
                            if (thatBill != null && thatBill.recipe == Bill.recipe && BillGivers.AssignedBills.Contains(new KeyValuePair<Bill_Production, Building_WorkTable>(thatBill, worker)))
                            {
                                billPresent = true;
                                if (thatBill.suspended != Bill.suspended || thatBill.repeatCount == 0)
                                {
#if DEBUG_JOBS
                                    Log.Message("Trying to unsuspend and/or bump targetCount");
#endif
                                    thatBill.suspended = Bill.suspended;
                                    thatBill.repeatCount = this.CountPerWorker(workerIndex);
                                    actionTaken = true;
                                }

                                Update(thatBill, ref actionTaken);
                            }
                        }
                    }
#if DEBUG_JOBS
                    Log.Message("Billstack scanned, bill was " + (billPresent ? "" : "not ") + "set");
#endif
                    if (!billPresent)
                    {
#if DEBUG_JOBS
                        Log.Message("Trying to add bill");
#endif
                        Bill_Production copy = Bill.Copy();
                        copy.repeatMode = BillRepeatMode.RepeatCount;
                        copy.repeatCount = this.CountPerWorker(workerIndex);
                        worker.BillStack?.AddBill(copy);
                        BillGivers.AssignedBills.Add(copy, worker);
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

        private void CleanNoLongerAllowedBillgivers(List<Building_WorkTable> workers, Dictionary<Bill_Production, Building_WorkTable> assignedBills, ref bool actionTaken)
        {
#if DEBUG_JOBS
            Log.Message("Cleaning no longer allowed billgivers");
#endif
            Dictionary<Bill_Production, Building_WorkTable> toBeDeleted = new Dictionary<Bill_Production, Building_WorkTable>();
            foreach (KeyValuePair<Bill_Production, Building_WorkTable> pair in assignedBills.Where(pair => !workers.Contains(pair.Value)))
            {
#if DEBUG_JOBS
                Log.Message("Deleting " + pair.Key.LabelCap + " from " + pair.Value.LabelCap);
#endif
                pair.Value.BillStack.Delete(pair.Key);
                toBeDeleted.Add(pair.Key, pair.Value);
                actionTaken = true;
            }
            foreach (KeyValuePair<Bill_Production, Building_WorkTable> pair in toBeDeleted)
            {
                assignedBills.Remove(pair.Key);
            }
        }

        private void Update(Bill_Production thatBill, ref bool actionTaken)
        {
            if (thatBill.storeMode != Bill.storeMode)
            {
                thatBill.storeMode = Bill.storeMode;
                actionTaken = true;
            }

            if (thatBill.ingredientFilter != Bill.ingredientFilter)
            {
                thatBill.ingredientFilter = Bill.ingredientFilter;
                actionTaken = true;
            }

            if (Math.Abs(thatBill.ingredientSearchRadius - Bill.ingredientSearchRadius) > 1)
            {
                thatBill.ingredientSearchRadius = Bill.ingredientSearchRadius;
                actionTaken = true;
            }

            if (thatBill.minSkillLevel != Bill.minSkillLevel)
            {
                thatBill.minSkillLevel = Bill.minSkillLevel;
                actionTaken = true;
            }
        }

        public override void CleanUp()
        {

#if DEBUG_JOBS
            Log.Message("Cleaning up obsolete bills");
#endif
            List<Bill_Production> toBeDeleted = new List<Bill_Production>();
            foreach (KeyValuePair<Bill_Production, Building_WorkTable> pair in BillGivers.AssignedBills)
            {
                pair.Value.BillStack.Delete(pair.Key);
                toBeDeleted.Add(pair.Key);
#if DEBUG_JOBS
                Log.Message("Checking worker " + pair.Value.LabelCap);
#endif
            }
            foreach (Bill_Production key in toBeDeleted)
            {
#if DEBUG_JOBS
                Log.Message("Deleting bill " + key.LabelCap);
#endif
                BillGivers.AssignedBills.Remove(key);
            }
        }

        public override string ToString()
        {
            string strout = base.ToString();
            strout += "\n" + Bill;
            return strout;
        }
    }
}
