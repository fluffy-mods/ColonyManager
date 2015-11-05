using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FM
{
    public class ManagerTab_Production : ManagerTab
    {
        public enum SourceOptions
        {
            Available,
            Current
        }

        public static SourceOptions Source = SourceOptions.Available;
        public static Vector2 LeftRowScrollPosition = new Vector2( 0f, 0f );
        public static Vector2 BillScrollPosition = new Vector2( 0f, 0f );
        public static List< ManagerJobProduction > SourceList;
        public static string SourceFilter = "";
        public static float SourceListHeight;
        private static ManagerJobProduction _selected;
        public override ManagerJob Selected
        { 
            get
            {
                return _selected;
            }
            set
            {
                _selected = (ManagerJobProduction)value;
            }
        }

        private bool _postOpenFocus;
        public float LeftRowSize = 300f;

        //TODO: Add priority switchers to current tab. (After overview tab is done).
        public override string Label { get; } = "FMP.Production".Translate();

        public static void RefreshSourceList()
        {
            SourceList = new List< ManagerJobProduction >();

            switch ( Source )
            {
                case SourceOptions.Available:
                    SourceList = ( from rd in DefDatabase< RecipeDef >.AllDefsListForReading
                                   where rd.HasBuildingRecipeUser( true )
                                   orderby rd.LabelCap
                                   select ( new ManagerJobProduction( rd ) ) ).ToList();
                    break;
                case SourceOptions.Current:
                    SourceList = Manager.Get.JobStack.FullStack.OfType< ManagerJobProduction >().ToList();
                    break;
            }
        }

        public override void DoWindowContents( Rect canvas )
        {
            Rect leftRow = new Rect( 0f, 35f, LeftRowSize, canvas.height - 35f );
            Rect contentCanvas = new Rect( leftRow.xMax + 10f, 5f, canvas.width - leftRow.width - 10f,
                                           canvas.height - 5f );

            DoLeftRow( leftRow );
            DoContent( contentCanvas );
        }

        public void DoContent( Rect canvas )
        {
            Widgets.DrawMenuSection( canvas );

            if ( _selected != null )
            {
                // leave some space for bottom buttons.
                float bottomButtonsHeight = 30f;
                float bottomButtonsGap = 6f;
                canvas.height = canvas.height - bottomButtonsHeight - bottomButtonsGap;

                // bottom buttons
                Rect bottomButtons = new Rect( canvas.xMin, canvas.height + bottomButtonsGap, canvas.width,
                                               bottomButtonsHeight );
                GUI.BeginGroup( bottomButtons );

                // add / remove to the stack
                Rect add = new Rect( bottomButtons.width * .75f, 0f, bottomButtons.width / 4f - 6f, bottomButtons.height );
                if ( Source == SourceOptions.Current )
                {
                    if ( Widgets.TextButton( add, "FM.Delete".Translate() ) )
                    {
                        Manager.Get.JobStack.Delete( _selected );
                        _selected = null;
                        RefreshSourceList();
                        return; // hopefully that'll just skip to the next tick without any further errors?
                    }
                    TooltipHandler.TipRegion( add, "FMP.DeleteBillTooltip".Translate() );
                }
                else
                {
                    if ( _selected.Trigger.IsValid )
                    {
                        if ( Widgets.TextButton( add, "FM.Manage".Translate() ) )
                        {
                            Manager.Get.JobStack.Add( _selected );

                            // refresh source list so that the next added job is not an exact copy.
                            RefreshSourceList();

                            Source = SourceOptions.Current;
                            RefreshSourceList();
                            SourceFilter = "";
                        }
                        TooltipHandler.TipRegion( add, "FMP.ManageBillTooltip".Translate() );
                    }
                    else
                    {
                        TextAnchor oldAnchor = Text.Anchor;
                        Color oldColor = GUI.color;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        GUI.color = new Color( .6f, .6f, .6f );
                        Widgets.DrawBox( add );
                        GUI.Label( add, "FMP.NoThreshold".Translate() );
                        Text.Anchor = oldAnchor;
                        GUI.color = oldColor;
                        TooltipHandler.TipRegion( add, "FMP.NoThresholdTooltip".Translate() );
                    }
                }

                GUI.EndGroup();

                GUI.BeginGroup( canvas );
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Rect recta = new Rect( 0f, 0f, canvas.width, 50f );
                Widgets.Label( recta, _selected.Bill.LabelCap );
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                Rect rect2 = new Rect( 6f, 50f, canvas.width * .3f, canvas.height - 50f );
                Listing_Standard listingStandard = new Listing_Standard( rect2 );
                if ( _selected.Bill.suspended )
                {
                    if ( listingStandard.DoTextButton( "Suspended".Translate() ) )
                    {
                        _selected.Bill.suspended = false;
                    }
                }
                else if ( listingStandard.DoTextButton( "NotSuspended".Translate() ) )
                {
                    _selected.Bill.suspended = true;
                }
                string billStoreModeLabel = ( "BillStoreMode_" + _selected.Bill.storeMode ).Translate();
                if ( listingStandard.DoTextButton( billStoreModeLabel ) )
                {
                    List< FloatMenuOption > list = ( from BillStoreMode mode in Enum.GetValues( typeof (BillStoreMode) )
                                                     select
                                                         new FloatMenuOption( ( "BillStoreMode_" + mode ).Translate(),
                                                                              delegate { _selected.Bill.storeMode = mode; } ) )
                        .ToList();
                    Find.WindowStack.Add( new FloatMenu( list ) );
                }

                // other stuff
                listingStandard.DoGap();
                listingStandard.DoLabel( "IngredientSearchRadius".Translate() + ": " +
                                         _selected.Bill.ingredientSearchRadius.ToString( "#####0" ) );
                _selected.Bill.ingredientSearchRadius =
                    Mathf.RoundToInt( listingStandard.DoSlider( _selected.Bill.ingredientSearchRadius, 0f, 250f ) );

                if ( _selected.Bill.recipe.workSkill != null )
                {
                    listingStandard.DoLabel(
                        "MinimumSkillLevel".Translate( _selected.Bill.recipe.workSkill.label.ToLower() ) + ": " +
                        _selected.Bill.minSkillLevel.ToString( "#####0" ) );
                    _selected.Bill.minSkillLevel =
                        Mathf.RoundToInt( listingStandard.DoSlider( _selected.Bill.minSkillLevel, 0f, 20f ) );
                    listingStandard.DoLabelCheckbox( "Highest colonist skill", ref _selected.maxSkil );
                }

                // draw threshold config
                _selected.Trigger.DrawThresholdConfig( ref listingStandard );
                _selected.BillGivers.DrawBillGiverConfig( ref listingStandard );
                listingStandard.End();

                // ingredient picker
                Rect rect3 = new Rect( rect2.xMax + 6f, 50f, canvas.width * .4f, canvas.height - 50f );
                ThingFilterUI.DoThingFilterConfigWindow( rect3, ref BillScrollPosition, _selected.Bill.ingredientFilter,
                                                         _selected.Bill.recipe.fixedIngredientFilter, 4 );

                // description
                Rect rect4 = new Rect( rect3.xMax + 6f, rect3.y + 30f, canvas.width - rect3.xMax - 12f,
                                       canvas.height - 50f );
                StringBuilder stringBuilder = new StringBuilder();

                // add mainproduct line
                stringBuilder.AppendLine( "FMP.MainProduct".Translate( _selected.MainProduct.Label, _selected.MainProduct.Count ) );
                stringBuilder.AppendLine();

                if ( _selected.Bill.recipe.description != null )
                {
                    stringBuilder.AppendLine( _selected.Bill.recipe.description );
                    stringBuilder.AppendLine();
                }
                stringBuilder.AppendLine( "WorkAmount".Translate() + ": " +
                                          _selected.Bill.recipe.WorkAmountTotal( null ).ToStringWorkAmount() );
                stringBuilder.AppendLine();
                foreach ( IngredientCount ingredientCount in _selected.Bill.recipe.ingredients )
                {
                    if ( !ingredientCount.filter.Summary.NullOrEmpty() )
                    {
                        stringBuilder.AppendLine(
                            _selected.Bill.recipe.IngredientValueGetter.BillRequirementsDescription( ingredientCount ) );
                    }
                }
                stringBuilder.AppendLine();
                string text4 = _selected.Bill.recipe.IngredientValueGetter.ExtraDescriptionLine();
                if ( text4 != null )
                {
                    stringBuilder.AppendLine( text4 );
                    stringBuilder.AppendLine();
                }
                stringBuilder.AppendLine( "MinimumSkills".Translate() );
                stringBuilder.AppendLine( _selected.Bill.recipe.MinSkillString );
                Text.Font = GameFont.Small;
                string text5 = stringBuilder.ToString();
                if ( Text.CalcHeight( text5, rect4.width ) > rect4.height )
                {
                    Text.Font = GameFont.Tiny;
                }
                Widgets.Label( rect4, text5 );
                Text.Font = GameFont.Small;
                if ( _selected.Bill.recipe.products.Count == 1 )
                {
                    Widgets.InfoCardButton( rect4.x, rect3.y, _selected.Bill.recipe.products[0].thingDef );
                }
            }
            GUI.EndGroup();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            RefreshSourceList();
        }

        public override void PostOpen()
        {
            // focus on the filter on open, flag is checked after the field is actually drawn.
            _postOpenFocus = false;
        }

        public void DoLeftRow( Rect canvas )
        {
            Widgets.DrawMenuSection( canvas, false );

            // filter
            Rect filterRect = new Rect( 10f, canvas.yMin + 5f, canvas.width - 50f, 30f );

            GUI.SetNextControlName( "filterTextfield" );
            SourceFilter = Widgets.TextField( filterRect, SourceFilter );

            if ( !_postOpenFocus )
            {
                GUI.FocusControl( "filterTextfield" );
                _postOpenFocus = true;
            }

            if ( SourceFilter != "" )
            {
                Rect clearFilter = new Rect( filterRect.width + 10f, filterRect.yMin, 30f, 30f );
                if ( Widgets.ImageButton( clearFilter, Widgets.CheckboxOffTex ) )
                {
                    SourceFilter = "";
                }
                TooltipHandler.TipRegion( clearFilter, "FMP.ClearFilterDesc".Translate() );
            }
            TooltipHandler.TipRegion( filterRect, "FMP.FilterDesc".Translate() );

            // tabs
            List< TabRecord > list = new List< TabRecord >();
            TabRecord availableTabRecord = new TabRecord( "FMP.Available".Translate(), delegate
            {
                Source = SourceOptions.Available;
                RefreshSourceList();
            }, Source == SourceOptions.Available );
            list.Add( availableTabRecord );
            TabRecord currentTabRecord = new TabRecord( "FMP.Current".Translate(), delegate
            {
                Source = SourceOptions.Current;
                RefreshSourceList();
            }, Source == SourceOptions.Current );
            list.Add( currentTabRecord );
            TabDrawer.DrawTabs( canvas, list );

            // content
            Rect scrollCanvas = canvas; //.ContractedBy( 10f );
            scrollCanvas.yMin = scrollCanvas.yMin + 40f;
            float height = SourceListHeight + 20f;
            Rect scrollView = new Rect( 0f, 0f, scrollCanvas.width, height );
            if ( height > scrollCanvas.height )
            {
                scrollView.width -= 16f;
            }

            Widgets.BeginScrollView( scrollCanvas, ref LeftRowScrollPosition, scrollView );
            Rect scrollContent = scrollView;

            GUI.BeginGroup( scrollContent );
            float y = 0;
            int i = 0;

            foreach ( ManagerJobProduction current in from job in SourceList
                                                      where
                                                          job.Bill.recipe.label.ToUpper()
                                                             .Contains( SourceFilter.ToUpper() ) ||
                                                          job.MainProduct.Label.ToUpper()
                                                             .Contains( SourceFilter.ToUpper() )
                                                      select job )
            {
                Rect row = new Rect( 0f, y, scrollContent.width, Manager.ListEntryHeight );
                Widgets.DrawHighlightIfMouseover( row );
                if ( _selected == current )
                {
                    Widgets.DrawHighlightSelected( row );
                }

                if ( i++ % 2 == 1 )
                {
                    GUI.DrawTexture( row, ManagerTab_Overview.OddRowBg );
                }

                Rect jobRect = row;

                if ( Source == SourceOptions.Current )
                {
                    if ( ManagerTab_Overview.DrawOrderButtons(
                        new Rect( row.xMax - 50f, row.yMin, 50f, 50f ), current ) )
                    {
                        RefreshSourceList();
                    }
                    jobRect.width -= 50f;
                }

                current.DrawListEntry( jobRect, false, Source == SourceOptions.Current );
                if ( Widgets.InvisibleButton( jobRect ) )
                {
                    _selected = current;
                }

                y += Manager.ListEntryHeight;
            }
            SourceListHeight = y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }
    }
}