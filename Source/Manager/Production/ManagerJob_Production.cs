using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;


namespace FM
{
    public class ManagerJob_Production : ManagerJob, IManagerJob
    {
        public ManagerJob_Production(RecipeDef recipe)
        {
            if (recipe.UsesUnfinishedThing) bill = new Bill_ProductionWithUft(recipe);
            else bill = new Bill_Production(recipe);
            mainProduct = new MainProduct_Tracker(bill.recipe);
            trigger = new Trigger_Threshold(this);
            billGivers = new BillGiver_Tracker(bill.recipe);
        }

        public BillGiver_Tracker billGivers;

        public MainProduct_Tracker mainProduct;

        public Bill_Production bill;

        public new Trigger_Threshold trigger;

        public override bool active
        {
            get
            {
                return !bill.suspended;
            }

            set
            {
                bill.suspended = !value;
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

            if (trigger.state)
            {
#if DEBUG
                Log.Message("Checking workers for presence of bills");
#endif
                List<Building_WorkTable> workers = billGivers.GetAssignedBillGivers;

                // If trigger met, check if there's places we need to add the bill.
                for (int workerIndex = 0; workerIndex < workers.Count; workerIndex++)
                {
                    Building_WorkTable worker = workers[workerIndex];
#if DEBUG
                    Log.Message("Checking worker " + worker.LabelCap);
#endif
                    bool billPresent = false;
                    if (worker.BillStack != null && worker.BillStack.Count > 0)
                    {
                        for (int i = 0; i < worker.BillStack.Count; i++)
                        {
                            // TODO: the check for manager bills was removed with the removal from managed_bill class, we will now touch manually assigned jobs, this may not be desired.
                            Bill_Production thatBill = worker.BillStack[i] as Bill_Production;
                            if (thatBill != null && thatBill.recipe == bill.recipe)
                            {
                                billPresent = true;
                                if (thatBill.suspended || thatBill.repeatCount == 0)
                                {
#if DEBUG
                                    Log.Message("Trying to unsuspend and/or bump targetCount");
#endif
                                    thatBill.suspended = false;
                                    thatBill.repeatCount = Utilities_Production.CountPerWorker(this, workerIndex);
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
                        Bill_Production copy = bill.Copy();
                        copy.repeatMode = BillRepeatMode.RepeatCount;
                        copy.repeatCount = Utilities_Production.CountPerWorker(this, workerIndex);
                        worker.BillStack.AddBill(copy);
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
            foreach (Building_WorkTable worker in billGivers.GetAssignedBillGivers)
            {
#if DEBUG
                Log.Message("Checking worker " + worker.LabelCap);
#endif
                if (worker.BillStack != null && worker.BillStack.Count > 0)
                {
                    for (int i = 0; i < worker.BillStack.Count; i++)
                    {
                        // TODO: Again, check was removed.
                        Bill_Production thatBill = worker.BillStack[i] as Bill_Production;
                        if (thatBill != null && thatBill.recipe == this.bill.recipe)
                        {
#if DEBUG
                            Log.Message("Trying to delete obsolete bill");
#endif
                            worker.BillStack.Delete(thatBill);
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            string strout = base.ToString();
            strout += "\n" + bill.ToString();
            return strout;
        }
    }
}
