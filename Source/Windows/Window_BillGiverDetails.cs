//// Karel Kroeze
//// Window_BillGiverDetails.cs
//// 2016-12-09

//using RimWorld;
//using UnityEngine;
//using Verse;

//namespace FluffyManager
//{
//    public class WindowBillGiverDetails : Window
//    {
//        #region Fields

//        public ManagerJob_Production Job;
//        public Vector2 Scrollposition = new Vector2( 0f, 0f );
//        private string _input;

//        #endregion Fields


//        #region Properties

//        public override Vector2 InitialSize => new Vector2( 300f, 500 );

//        #endregion Properties


//        #region Methods

//        public override void DoWindowContents( Rect inRect )
//        {
//            var contentRect = new Rect( inRect );
//            GUI.BeginGroup( contentRect );

//            //TextAnchor oldAnchor = Text.Anchor;
//            Text.Anchor = TextAnchor.MiddleLeft;
//            Text.Font = GameFont.Small;
//            float x = 0;
//            float y = 6;

//            // All workstations
//            var all = new Rect( x, y, contentRect.width, 30f );
//            var allLabel = new Rect( 30f, y + 3f, contentRect.width - 30f, 27f );
//            y += 30;

//            if ( Job.BillGivers.BillGiverSelection == AssignedBillGiverOptions.All )
//            {
//                Widgets.DrawMenuSection( all );
//            }
//            else
//            {
//                TooltipHandler.TipRegion( all, "FMP.AllWorkstationTooltip" );
//                if ( Mouse.IsOver( all ) )
//                {
//                    GUI.DrawTexture( all, TexUI.HighlightTex );
//                }
//                if ( Widgets.ButtonInvisible( all ) )
//                {
//                    Job.BillGivers.BillGiverSelection = AssignedBillGiverOptions.All;
//                }
//            }
//            Widgets.RadioButton( new Vector2( all.xMin + 3f, all.yMin + 3f ),
//                                 Job.BillGivers.BillGiverSelection == AssignedBillGiverOptions.All );
//            Widgets.Label( allLabel, "FMP.AllWorkstations".Translate() );
//            y += 6;

//            // By area / count
//            var area = new Rect( x, y, contentRect.width, 30f );
//            var areaLabel = new Rect( 30f, y + 3f, contentRect.width - 30f, 27f );
//            y += 30f;

//            if ( Job.BillGivers.BillGiverSelection == AssignedBillGiverOptions.Count )
//            {
//                area.height += 60f;
//                Widgets.DrawMenuSection( area );
//                var areaAreaLabel = new Rect( 6f, y, 50f, 30f );
//                var areaAreaSelector = new Rect( 56f, y, contentRect.width - 56f, 30f );
//                y += 30;
//                var areaCountLabel = new Rect( 6f, y, 50f, 30f );
//                var areaCountSelector = new Rect( 56f, y, contentRect.width - 56f, 30f );
//                y += 30;

//                Widgets.Label( areaAreaLabel, "FMP.AllowedAreas".Translate() );

//                AreaAllowedGUI.DoAllowedAreaSelectors( areaAreaSelector, ref Job.BillGivers.AreaRestriction, Job.manager );

//                Color oldColor = GUI.color;
//                if ( _input.IsInt() )
//                {
//                    Job.BillGivers.UserBillGiverCount = int.Parse( _input );
//                }
//                else
//                {
//                    GUI.color = new Color( 1f, 0f, 0f );
//                }
//                Widgets.Label( areaCountLabel, "FMP.AllowedWorkstationCount".Translate() );
//                _input = Widgets.TextField( areaCountSelector, _input );
//                GUI.color = oldColor;
//            }
//            else
//            {
//                TooltipHandler.TipRegion( area, "FMP.ByAreaAndCountTooltip" );
//                if ( Mouse.IsOver( area ) )
//                {
//                    GUI.DrawTexture( area, TexUI.HighlightTex );
//                }
//                if ( Widgets.ButtonInvisible( area ) )
//                {
//                    Job.BillGivers.BillGiverSelection = AssignedBillGiverOptions.Count;
//                }
//            }
//            Widgets.Label( areaLabel, "FMP.ByAreaAndCount".Translate() );
//            Widgets.RadioButton( new Vector2( area.xMin + 3f, area.yMin + 3f ),
//                                 Job.BillGivers.BillGiverSelection == AssignedBillGiverOptions.Count );
//            y += 6f;

//            // Specific billgivers
//            var specific = new Rect( x, y, contentRect.width, 30f );
//            var specificLabel = new Rect( 36f, y, contentRect.width - 36f, 30f );
//            y += 30;

//            if ( Job.BillGivers.BillGiverSelection == AssignedBillGiverOptions.Specific )
//            {
//                specific.height += 24f * Job.BillGivers.PotentialBillGivers.Count;

//                Widgets.DrawMenuSection( specific, true );

//                foreach ( Building_WorkTable billgiver in Job.BillGivers.PotentialBillGivers )
//                {
//                    var row = new Rect( x, y, contentRect.width, 24f );
//                    DrawRow( billgiver, row );
//                    y += 24f;
//                }
//            }
//            else
//            {
//                if ( Mouse.IsOver( specific ) )
//                {
//                    GUI.DrawTexture( specific, TexUI.HighlightTex );
//                }
//                TooltipHandler.TipRegion( specific, "FMP.SpecificWorkstationsTooltip" );
//                if ( Widgets.ButtonInvisible( specific ) )
//                {
//                    Job.BillGivers.BillGiverSelection = AssignedBillGiverOptions.Specific;
//                }
//            }

//            Widgets.RadioButton( new Vector2( specific.xMin + 3f, specific.yMin + 3f ),
//                                 Job.BillGivers.BillGiverSelection == AssignedBillGiverOptions.Specific );
//            Widgets.Label( specificLabel, "FMP.SpecificWorkstations".Translate() );

//            Text.Anchor = TextAnchor.UpperLeft;
//            GUI.EndGroup();
//        }

//        public void DrawRow( Building_WorkTable billgiver, Rect row )
//        {
//            var labelRect = new Rect( row );
//            labelRect.width -= 36f;
//            labelRect.xMin += 6f;
//            var iconRect = new Rect( row );
//            iconRect.xMin = iconRect.xMax - 24f;

//            Text.Font = GameFont.Tiny;
//            Widgets.Label( labelRect, billgiver.LabelCap + ", " + billgiver.GetRoom().Role.LabelCap );
//            Text.Font = GameFont.Small;
//            if ( Job.BillGivers.SpecificBillGivers.Contains( billgiver ) )
//            {
//                GUI.DrawTexture( iconRect, Widgets.CheckboxOnTex );
//                if ( Widgets.ButtonInvisible( row ) )
//                {
//                    Job.BillGivers.SpecificBillGivers.Remove( billgiver );
//                }
//            }
//            else
//            {
//                if ( Widgets.ButtonInvisible( row ) )
//                {
//                    Job.BillGivers.SpecificBillGivers.Add( billgiver );
//                }
//            }

//            if ( Mouse.IsOver( row ) )
//            {
//                GUI.DrawTexture( row, TexUI.HighlightTex );
//                Find.CameraDriver.JumpTo( billgiver.PositionHeld );
//            }
//        }

//        public override void PreOpen()
//        {
//            base.PreOpen();
//            _input = Job.BillGivers.UserBillGiverCount.ToString();
//        }

//        #endregion Methods
//    }
//}

