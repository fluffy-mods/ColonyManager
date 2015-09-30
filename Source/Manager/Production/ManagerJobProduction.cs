using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RimWorld;
using Verse;

namespace FM
{
    public class ManagerJobProduction : ManagerJob
    {
        public ManagerJobProduction(RecipeDef recipe)
        {
            Bill = recipe.UsesUnfinishedThing ? new Bill_ProductionWithUft(recipe) : new Bill_Production(recipe);
            MainProduct = new MainProductTracker(Bill.recipe);
            Trigger = new TriggerThreshold(this);
            BillGivers = new BillGiverTracker(this);
        }

        public BillGiverTracker BillGivers;

        public MainProductTracker MainProduct;

        public Bill_Production Bill;

        // todo; move AssignedBills into BillGiverTracker
        public List<Pair<Bill_Production, Building_WorkTable>> AssignedBills = new List<Pair<Bill_Production, Building_WorkTable>>();

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
#if DEBUG
            Log.Message("Starting job for Production Manager.");
            Log.Message("Job: " + this.ToString());
#endif

            // flag to see if anything had to be done.
            bool actionTaken = false;

            if (Trigger.State)
            {
#if DEBUG
                Log.Message("Checking workers for presence of bills");
#endif
                List<Building_WorkTable> workers = BillGivers.GetAssignedBillGivers;
                CleanNoLongerAllowedBillgivers(workers, AssignedBills, ref actionTaken);

                // If Trigger met, check if there's places we need to add the bill.
                for (int workerIndex = 0; workerIndex < workers.Count; workerIndex++)
                {
                    Building_WorkTable worker = workers[workerIndex];
#if DEBUG
                    Log.Message("Checking worker " + worker.LabelCap);
#endif
                    bool billPresent = false;
                    if (worker.BillStack != null && worker.BillStack.Count > 0)
                    {
                        foreach (Bill t in worker.BillStack)
                        {
                            Bill_Production thatBill = t as Bill_Production;
                            if (thatBill != null && thatBill.recipe == Bill.recipe && AssignedBills.Contains(new Pair<Bill_Production, Building_WorkTable>(thatBill, worker)))
                            {
                                billPresent = true;
                                if (thatBill.suspended != Bill.suspended || thatBill.repeatCount == 0)
                                {
#if DEBUG
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
#if DEBUG
                    Log.Message("Billstack scanned, bill was " + (billPresent ? "" : "not ") + "set");
#endif
                    if (!billPresent)
                    {
#if DEBUG
                        Log.Message("Trying to add bill");
#endif
                        Bill_Production copy = Bill.Copy();
                        copy.repeatMode = BillRepeatMode.RepeatCount;
                        copy.repeatCount = this.CountPerWorker(workerIndex);
                        worker.BillStack?.AddBill(copy);
                        AssignedBills.Add(new Pair<Bill_Production, Building_WorkTable>(copy, worker));
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

        private void CleanNoLongerAllowedBillgivers(List<Building_WorkTable> workers, List<Pair<Bill_Production, Building_WorkTable>> assignedBills, ref bool actionTaken)
        {
            List<Pair<Bill_Production, Building_WorkTable>> toBeDeleted = new List<Pair<Bill_Production, Building_WorkTable>>();
            foreach (Pair<Bill_Production, Building_WorkTable> pair in assignedBills.Where(pair => !workers.Contains(pair.Second)))
            {
                pair.Second.BillStack.Delete(pair.First);
                toBeDeleted.Add(pair);
                actionTaken = true;
            }
            assignedBills.RemoveAll(pair => toBeDeleted.Contains(pair));
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

#if DEBUG
            Log.Message("Cleaning up obsolete bills");
#endif
            foreach (Pair<Bill_Production, Building_WorkTable> pair in AssignedBills)
            {
#if DEBUG
                Log.Message("Checking worker " + pair.First.LabelCap);
#endif

#if DEBUG
                Log.Message("Trying to delete obsolete bill");
#endif
                pair.Second.BillStack.Delete(pair.First);
                AssignedBills.Remove(pair);

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
