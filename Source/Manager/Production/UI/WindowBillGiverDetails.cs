using UnityEngine;
using Verse;

namespace FM
{
    public class WindowBillGiverDetails : Window
    {
        public override Vector2 InitialWindowSize => new Vector2(300f, 500);

        public override void PreOpen()
        {
            base.PreOpen();
            Input = BillGivers.UserBillGiverCount.ToString();
        }

        public string Input;

        public override void DoWindowContents(Rect inRect)
        {
            // ReSharper disable once UnusedVariable
            // implement
            Rect filterRect = new Rect(inRect.ContractedBy(6f));
            

            // TODO: string to int, validate
            Color oldColor = GUI.color;
            if (!Input.IsInt())
            {
                GUI.color = new Color(1f, 0f, 0f);
            }
            else
            {
                BillGivers.UserBillGiverCount = int.Parse(Input);
            }
            Input = Widgets.TextField(inRect, Input);
            GUI.color = oldColor;
        }

        public BillGiverTracker BillGivers;

        public Vector2 Scrollposition = new Vector2(0f, 0f);
    }
}
