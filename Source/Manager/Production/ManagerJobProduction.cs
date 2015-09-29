using System.Collections.Generic;
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
            BillGivers = new BillGiverTracker(Bill.recipe);
        }

        public BillGiverTracker BillGivers;

        public MainProductTracker MainProduct;

        public Bill_Production Bill;

        public List<Bill_Production> AssignedBills = new List<Bill_Production>(); 

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
                            if (thatBill != null && thatBill.recipe == Bill.recipe && AssignedBills.Contains(thatBill))
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
                        AssignedBills.Add(copy);
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

        public override void CleanUp()
        {

#if DEBUG
            Log.Message("Cleaning up obsolete bills");
#endif
            foreach (Building_WorkTable worker in BillGivers.GetAssignedBillGivers)
            {
#if DEBUG
                Log.Message("Checking worker " + worker.LabelCap);
#endif
                if (worker.BillStack != null && worker.BillStack.Count > 0)
                {
                    for (int i = 0; i < worker.BillStack.Count; i++)
                    {
                        Bill_Production thatBill = worker.BillStack[i] as Bill_Production;
                        if (thatBill != null && thatBill.recipe == Bill.recipe && AssignedBills.Contains(thatBill))
                        {
#if DEBUG
                            Log.Message("Trying to delete obsolete bill");
#endif
                            worker.BillStack.Delete(thatBill);
                            AssignedBills.Remove(thatBill);
                        }
                    }
                }
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
