// Manager/ManagerTab_Production.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:28

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static Vector2                     IngredientsScrollPosition = new Vector2( 0f, 0f );
        public static Vector2                     LeftRowScrollPosition     = new Vector2( 0f, 0f );
        public static SourceOptions               Source                    = SourceOptions.Available;
        public static string                      SourceFilter              = "";
        public static List<ManagerJob_Production> SourceList;
        public static float                       SourceListHeight;
        private static float                      _leftRowEntryHeight       = Utilities.ListEntryHeight;
        private static ManagerJob_Production      _selected;
        private static float                      _topAreaHeight            = 30f;
        private Vector2                           _button                   = new Vector2( 200f, 40f );
        private float                             _entryHeight              = 30f;
        private Vector2                           _infoScrollPosition       = Vector2.zero;
        private float                             _margin                   = Utilities.Margin;
        private bool                              _postOpenFocus;

        public override Texture2D Icon
        {
            get { return Resources.IconHammer; }
        }

        public override IconAreas IconArea
        {
            get { return IconAreas.Middle; }
        }

        public override string Label { get; } = "FMP.Production".Translate();

        public override ManagerJob Selected
        {
            get { return _selected; }
            set { _selected = (ManagerJob_Production)value; }
        }

        public static void Refresh()
        {
            SourceList = new List<ManagerJob_Production>();

            switch ( Source )
            {
                case SourceOptions.Available:
                    SourceList = ( from rd in DefDatabase<RecipeDef>.AllDefsListForReading
                                   where rd.HasBuildingRecipeUser( true )
                                   orderby rd.LabelCap
                                   select new ManagerJob_Production( rd ) ).ToList();
                    break;

                case SourceOptions.Current:
                    SourceList = Manager.Get.JobStack.FullStack<ManagerJob_Production>();
                    break;
            }
        }

        public void DoContent( Rect canvas )
        {
            Widgets.DrawMenuSection( canvas );
            GUI.BeginGroup( canvas );
            canvas = canvas.AtZero();

            if ( _selected != null )
            {
                // bottom buttons
                Rect buttonRect = new Rect( canvas.xMax - _button.x, canvas.yMax - _button.y, _button.x - _margin,
                                            _button.y - _margin );
                Rect ingredientCheck = new Rect( buttonRect.xMin - 300f - _margin, buttonRect.yMin, 300f,
                                                 buttonRect.height );

                // add / remove to the stack
                if ( Source == SourceOptions.Current )
                {
                    if ( Widgets.TextButton( buttonRect, "FM.Delete".Translate() ) )
                    {
                        _selected.Delete();
                        _selected = null;
                        Refresh();
                        return; // just skip to the next tick to avoid null reference errors.
                    }
                    TooltipHandler.TipRegion( buttonRect, "FMP.DeleteBillTooltip".Translate() );
                }
                else
                {
                    if ( _selected.Trigger.IsValid )
                    {
                        Widgets.LabelCheckbox(ingredientCheck, "FMP.CreateBillsForIngredients".Translate(), ref _selected._createIngredientBills, !_selected._hasMeaningfulIngredientChoices);

                        if ( Widgets.TextButton( buttonRect, "FM.Manage".Translate() ) )
                        {
                            _selected.Managed = true;
                            Manager.Get.JobStack.Add( _selected );

                            // refresh source list so that the next added job is not an exact copy.
                            Refresh();

                            if ( _selected._hasMeaningfulIngredientChoices &&
                                 _selected._createIngredientBills )
                            {
                                Find.WindowStack.Add( new Dialog_CreateJobsForIngredients( _selected.Bill.recipe, _selected.Trigger.Count ) );
                            }

                            Source = SourceOptions.Current;
                            Refresh();
                            SourceFilter = "";
                        }
                        TooltipHandler.TipRegion( buttonRect, "FMP.ManageBillTooltip".Translate() );
                    }
                    else
                    {
                        GUI.color = new Color( .6f, .6f, .6f );
                        Widgets.DrawBox( buttonRect );
                        Utilities.Label( buttonRect, "FMP.NoThreshold".Translate(), "FMP.NoThresholdTooltip".Translate(),
                                         TextAnchor.MiddleCenter );
                        GUI.color = Color.white;
                    }
                }

                // options
                Rect optionsColumnRect = new Rect( _margin / 2,
                                                   _topAreaHeight,
                                                   canvas.width / 2 - _margin,
                                                   canvas.height - _topAreaHeight - _margin - _button.y );
                Rect billColumnRect = new Rect( optionsColumnRect.xMax + _margin,
                                                _topAreaHeight,
                                                canvas.width / 2 - _margin,
                                                canvas.height - _topAreaHeight - _margin - _button.y );

                Rect optionsColumnTitle = new Rect( optionsColumnRect.xMin,
                                                    0f,
                                                    optionsColumnRect.width,
                                                    _topAreaHeight );
                Rect ingredientsColumnTitle = new Rect( billColumnRect.xMin,
                                                        0f,
                                                        billColumnRect.width,
                                                        _topAreaHeight );

                // backgrounds
                GUI.DrawTexture( optionsColumnRect, Resources.SlightlyDarkBackground );
                GUI.DrawTexture( billColumnRect, Resources.SlightlyDarkBackground );

                // titles
                Utilities.Label( optionsColumnTitle, "FMP.Options".Translate(), lrMargin: _margin * 2,
                                 anchor: TextAnchor.LowerLeft, font: GameFont.Tiny );
                Utilities.Label( ingredientsColumnTitle, "FMP.Bill".Translate(), lrMargin: _margin * 2,
                                 anchor: TextAnchor.LowerLeft, font: GameFont.Tiny );

                // options
                GUI.BeginGroup( optionsColumnRect );
                Vector2 cur = Vector2.zero;
                float width = optionsColumnRect.width;

                // suspended (1)
                Rect suspendedRect = new Rect( cur.x, cur.y, width, _entryHeight );
                Widgets.DrawAltRect( suspendedRect );
                Utilities.DrawToggle( suspendedRect, "Suspended".Translate(), _selected.Suspended,
                                      delegate { _selected.Suspended = !_selected.Suspended; } );
                cur.y += _entryHeight;

                // store mode (2)
                Rect takeToStockRect = new Rect( cur.x, cur.y, width, _entryHeight );
                Utilities.DrawToggle( takeToStockRect, "BillStoreMode_BestStockpile".Translate(),
                                      _selected.Bill.storeMode == BillStoreMode.BestStockpile,
                                      delegate { _selected.Bill.storeMode = BillStoreMode.BestStockpile; },
                                      delegate { _selected.Bill.storeMode = BillStoreMode.DropOnFloor; } );
                cur.y += _entryHeight;

                // ingredient search radius (3)
                Rect searchRadiusLabelRect = new Rect( cur.x, cur.y, width, _entryHeight );
                Widgets.DrawAltRect( searchRadiusLabelRect );
                Utilities.Label( searchRadiusLabelRect,
                                 "IngredientSearchRadius".Translate() + ": " +
                                 _selected.Bill.ingredientSearchRadius.ToString( " #####0" ),
                                 anchor: TextAnchor.MiddleLeft, lrMargin: _margin );
                cur.y += _entryHeight;

                Rect searchRadiusRect = new Rect( cur.x, cur.y, width, Utilities.SliderHeight );
                Widgets.DrawAltRect( searchRadiusRect );
                _selected.Bill.ingredientSearchRadius =
                    (int)GUI.HorizontalSlider( searchRadiusRect, _selected.Bill.ingredientSearchRadius, 0f, 250f );
                cur.y += Utilities.SliderHeight;

                // prioritize over manually set jobs (4)
                Rect prioritizeRect = new Rect( cur.x, cur.y, width, _entryHeight );
                Utilities.DrawToggle(prioritizeRect, "FMP.PrioritizeManual".Translate(), ref ManagerJob_Production.prioritizeManual);
                cur.y += _entryHeight;
                
                // min skill (5)
                if ( _selected.Bill.recipe.workSkill != null )
                {
                    Rect skillLabelRect = new Rect( cur.x, cur.y, width, _entryHeight );
                    Utilities.Label( skillLabelRect,
                                     "MinimumSkillLevel".Translate( _selected.Bill.recipe.workSkill.label.ToLower() )
                                     + ": " + _selected.Bill.minSkillLevel.ToString( "#####0" ),
                                     anchor: TextAnchor.MiddleLeft, lrMargin: 6f );
                    cur.y += _entryHeight;

                    Rect skillRect = new Rect( cur.x, cur.y, width, Utilities.SliderHeight );
                    _selected.Bill.minSkillLevel =
                        (int)GUI.HorizontalSlider( skillRect, _selected.Bill.minSkillLevel, 0f, 20f );
                    cur.y += Utilities.SliderHeight;

                    Rect snapToHighestRect = new Rect( cur.x, cur.y, width, _entryHeight );
                    Utilities.DrawToggle( snapToHighestRect, "FMP.SnapToHighestSkill".Translate(), ref _selected.maxSkil );
                    cur.y += _entryHeight;

                    Widgets.DrawAltRect(skillLabelRect);
                    Widgets.DrawAltRect(skillRect);
                    Widgets.DrawAltRect(snapToHighestRect);
                }

                // draw threshold and billgiver config (6, 7)
                _selected.Trigger.DrawThresholdConfig( ref cur, optionsColumnRect.width, _entryHeight );
                _selected.BillGivers.DrawBillGiverConfig( ref cur, optionsColumnRect.width, _entryHeight, true );

                GUI.EndGroup(); // options

                // bill
                GUI.BeginGroup( billColumnRect );
                cur = Vector2.zero;
                width = billColumnRect.width;

                // bill information
                Rect infoRect = new Rect( cur.x, cur.y, width, ( billColumnRect.height - cur.y ) / 2 );
                string text = GetInfoText();
                float actualHeight = Text.CalcHeight( text, infoRect.width );

                // if required height is small, cull info area
                if ( infoRect.height > actualHeight )
                {
                    infoRect.height = actualHeight;
                }

                // set up scrolling region
                Rect infoViewRect = infoRect;
                if ( actualHeight > infoRect.height )
                {
                    infoViewRect.width -= 16f; // scrollbar
                    infoViewRect.height = Text.CalcHeight( text, infoViewRect.width );
                }

                Widgets.BeginScrollView( infoRect, ref _infoScrollPosition, infoViewRect );
                Utilities.Label( infoRect, text, lrMargin: _margin );
                Widgets.EndScrollView();

                // if there is one or more products known to us (so not smelting, ugh!) display an infocard button
                if ( _selected.Bill.recipe.products.Count > 0 )
                {
                    Widgets.InfoCardButton( infoRect.xMax - Widgets.InfoCardButtonSize - _margin,
                                            infoRect.yMin + _margin, _selected.Bill.recipe.products[0].thingDef );
                }
                cur.y += infoRect.height;

                // ingredients label
                Rect ingredientsLabelRect = new Rect( cur.x, cur.y, width, _entryHeight );
                Utilities.Label( ingredientsLabelRect, "FMP.AllowedIngredients".Translate(),
                                 anchor: TextAnchor.MiddleLeft, lrMargin: _margin );
                cur.y += _entryHeight;

                // ingredients picker, fill available space
                Rect ingredientsRect = new Rect( cur.x, cur.y, width, billColumnRect.height - cur.y );
                ThingFilterUI filterUI = new ThingFilterUI();
                filterUI.DoThingFilterConfigWindow( ingredientsRect, ref IngredientsScrollPosition,
                                                    _selected.Bill.ingredientFilter,
                                                    _selected.Bill.recipe.fixedIngredientFilter, 4 );

                GUI.EndGroup(); // bill
            }

            GUI.EndGroup(); // window
        }

        private static string GetInfoText()
        {
            StringBuilder stringBuilder = new StringBuilder();

            // add mainproduct line
            stringBuilder.AppendLine( "FMP.MainProduct".Translate( _selected.MainProduct.Label,
                                                                   _selected.MainProduct.Count ) );
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
            string extraDescriptionLine = _selected.Bill.recipe.IngredientValueGetter.ExtraDescriptionLine();
            if ( extraDescriptionLine != null )
            {
                stringBuilder.AppendLine( extraDescriptionLine );
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine( "MinimumSkills".Translate() );
            stringBuilder.AppendLine( _selected.Bill.recipe.MinSkillString );
            Text.Font = GameFont.Small;
            string infoText = stringBuilder.ToString();
            return infoText;
        }

        public void DoLeftRow( Rect canvas )
        {
            Widgets.DrawMenuSection( canvas, false );

            // filter
            Rect filterRect = new Rect( 10f, canvas.yMin + 5f, canvas.width - _leftRowEntryHeight, _entryHeight );

            GUI.SetNextControlName( "filterTextfield" );
            SourceFilter = Widgets.TextField( filterRect, SourceFilter );

            if ( !_postOpenFocus )
            {
                GUI.FocusControl( "filterTextfield" );
                _postOpenFocus = true;
            }

            if ( SourceFilter != "" )
            {
                Rect clearFilter = new Rect( filterRect.width + 10f, filterRect.yMin, _entryHeight, _entryHeight );
                if ( Widgets.ImageButton( clearFilter, Widgets.CheckboxOffTex ) )
                {
                    SourceFilter = "";
                }
                TooltipHandler.TipRegion( clearFilter, "FMP.ClearFilterDesc".Translate() );
            }
            TooltipHandler.TipRegion( filterRect, "FMP.FilterDesc".Translate() );

            // tabs
            List<TabRecord> list = new List<TabRecord>();
            TabRecord availableTabRecord = new TabRecord( "FMP.Available".Translate(), delegate
            {
                Source = SourceOptions.Available;
                Refresh();
            }, Source == SourceOptions.Available );
            list.Add( availableTabRecord );
            TabRecord currentTabRecord = new TabRecord( "FMP.Current".Translate(), delegate
            {
                Source = SourceOptions.Current;
                Refresh();
            }, Source == SourceOptions.Current );
            list.Add( currentTabRecord );
            TabDrawer.DrawTabs( canvas, list );

            // content
            Rect scrollCanvas = canvas;
            scrollCanvas.yMin = scrollCanvas.yMin + _entryHeight + _margin;
            float height = SourceListHeight;
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

            foreach ( ManagerJob_Production current in from job in SourceList
                                                       where
                                                           job.Bill.recipe.label.ToUpper()
                                                              .Contains( SourceFilter.ToUpper() ) ||
                                                           job.MainProduct.Label.ToUpper()
                                                              .Contains( SourceFilter.ToUpper() )
                                                       select job )
            {
                Rect row = new Rect( 0f, y, scrollContent.width, Utilities.ListEntryHeight );
                Widgets.DrawHighlightIfMouseover( row );
                if ( _selected == current )
                {
                    Widgets.DrawHighlightSelected( row );
                }

                if ( i++ % 2 == 1 )
                {
                    Widgets.DrawAltRect( row );
                }

                Rect jobRect = row;

                if ( Source == SourceOptions.Current )
                {
                    if ( ManagerTab_Overview.DrawOrderButtons(
                        new Rect( row.xMax - _leftRowEntryHeight, row.yMin, _leftRowEntryHeight, _leftRowEntryHeight ),
                        current ) )
                    {
                        Refresh();
                    }
                    jobRect.width -= _leftRowEntryHeight;
                }

                current.DrawListEntry( jobRect, false, Source == SourceOptions.Current );
                if ( Widgets.InvisibleButton( jobRect ) )
                {
                    _selected = current;
                }

                y += Utilities.ListEntryHeight;
            }
            SourceListHeight = y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public override void DoWindowContents( Rect canvas )
        {
            Rect leftRow = new Rect( 0f, 31f, DefaultLeftRowSize, canvas.height - 31f );
            Rect contentCanvas = new Rect( leftRow.xMax + Utilities.Margin, 0f,
                                           canvas.width - leftRow.width - Utilities.Margin, canvas.height );

            DoLeftRow( leftRow );
            DoContent( contentCanvas );
        }

        public override void PostOpen()
        {
            // focus on the filter on open, flag is checked after the field is actually drawn.
            _postOpenFocus = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Refresh();
        }
    }
}