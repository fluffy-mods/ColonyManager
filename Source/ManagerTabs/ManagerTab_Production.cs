//// Karel Kroeze
//// ManagerTab_Production.cs
//// 2016-12-09

//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using Verse;

//namespace FluffyManager
//{
//    public class ManagerTab_Production : ManagerTab
//    {
//        #region Fields

//        public Vector2 IngredientsScrollPosition = new Vector2( 0f, 0f );

//        public Vector2 LeftRowScrollPosition = new Vector2( 0f, 0f );

//        public SourceOptions Source = SourceOptions.Available;

//        public string SourceFilter = "";

//        public List<ManagerJob_Production> SourceList;

//        public float SourceListHeight;

//        private static float _leftRowEntryHeight = Utilities.LargeListEntryHeight;

//        private static float _topAreaHeight = 30f;

//        private Vector2 _button = new Vector2( 200f, 40f );

//        private float _entryHeight = 30f;

//        private Vector2 _infoScrollPosition = Vector2.zero;

//        private float _margin = Utilities.Margin;

//        private bool _postOpenFocus;

//        private ManagerJob_Production _selected;

//        private ThingFilterUI filterUI = new ThingFilterUI();

//        #endregion Fields

//        #region Constructors

//        public ManagerTab_Production( Manager manager ) : base( manager )
//        {
//        }

//        #endregion Constructors


//        #region Enums

//        public enum SourceOptions
//        {
//            Available,
//            Current
//        }

//        #endregion Enums

//        public override Texture2D Icon
//        {
//            get { return Resources.IconHammer; }
//        }

//        public override IconAreas IconArea
//        {
//            get { return IconAreas.Middle; }
//        }

//        public override string Label { get; } = "FMP.Production".Translate();

//        public override ManagerJob Selected
//        {
//            get
//            {
//                return _selected;
//            }
//            set
//            {
//                _selected = (ManagerJob_Production)value;
//                _selected?.ForceRecacheOtherRecipe();
//                if ( _selected != null && _selected.Managed && Source != SourceOptions.Current )
//                {
//                    Source = SourceOptions.Current;
//                    Refresh();
//                }
//            }
//        }

//        public void DoContent( Rect canvas )
//        {
//            Widgets.DrawMenuSection( canvas );
//            GUI.BeginGroup( canvas );
//            canvas = canvas.AtZero();

//            if ( _selected != null )
//            {
//                // bottom buttons
//                var buttonRect = new Rect( canvas.xMax - _button.x, canvas.yMax - _button.y, _button.x - _margin,
//                                           _button.y - _margin );
//                var ingredientCheck = new Rect( buttonRect.xMin - 300f - _margin, buttonRect.yMin, 300f,
//                                                buttonRect.height );

//                // add / remove to the stack
//                if ( Source == SourceOptions.Current )
//                {
//                    if ( Widgets.ButtonText( buttonRect, "FM.Delete".Translate() ) )
//                    {
//                        _selected.Delete();
//                        _selected = null;
//                        Refresh();
//                        return; // just skip to the next tick to avoid null reference errors.
//                    }

//                    TooltipHandler.TipRegion( buttonRect, "FMP.DeleteBillTooltip".Translate() );
//                }
//                else
//                {
//                    if ( _selected.Trigger.IsValid )
//                    {
//                        Widgets.CheckboxLabeled( ingredientCheck, "FMP.IngredientDialogTitle".Translate(),
//                                                 ref _selected._createIngredientBills,
//                                                 !_selected._hasMeaningfulIngredientChoices );

//                        if ( Widgets.ButtonText( buttonRect, "FM.Manage".Translate() ) )
//                        {
//                            _selected.Managed = true;
//                            manager.JobStack.Add( _selected );

//                            // refresh source list so that the next added job is not an exact copy.
//                            Refresh();

//                            if ( _selected._hasMeaningfulIngredientChoices &&
//                                 _selected._createIngredientBills )
//                            {
//                                Find.WindowStack.Add( new Dialog_CreateJobsForIngredients( manager,
//                                                                                           _selected.Bill.recipe,
//                                                                                           _selected.Trigger.Count ) );
//                            }

//                            Source = SourceOptions.Current;
//                            Refresh();
//                            SourceFilter = "";
//                        }
//                        TooltipHandler.TipRegion( buttonRect, "FMP.ManageBillTooltip".Translate() );
//                    }
//                    else
//                    {
//                        GUI.color = new Color( .6f, .6f, .6f );
//                        Widgets.DrawBox( buttonRect );
//                        Utilities.Label( buttonRect, "FMP.NoThreshold".Translate(), "FMP.NoThresholdTooltip".Translate(),
//                                         TextAnchor.MiddleCenter );
//                        GUI.color = Color.white;
//                    }
//                }

//                // options
//                var optionsColumnRect = new Rect( _margin / 2,
//                                                  _topAreaHeight,
//                                                  canvas.width / 2 - _margin,
//                                                  canvas.height - _topAreaHeight - _margin - _button.y );
//                var recipeColumnRect = new Rect( optionsColumnRect.xMax + _margin,
//                                                 _topAreaHeight,
//                                                 canvas.width / 2 - _margin,
//                                                 canvas.height - _topAreaHeight - _margin - _button.y );

//                var optionsColumnTitle = new Rect( optionsColumnRect.xMin,
//                                                   0f,
//                                                   optionsColumnRect.width,
//                                                   _topAreaHeight );
//                var recipeColumnTitle = new Rect( recipeColumnRect.xMin,
//                                                  0f,
//                                                  recipeColumnRect.width,
//                                                  _topAreaHeight );

//                // backgrounds
//                GUI.DrawTexture( optionsColumnRect, Resources.SlightlyDarkBackground );
//                GUI.DrawTexture( recipeColumnRect, Resources.SlightlyDarkBackground );

//                // titles
//                Utilities.Label( optionsColumnTitle, "FMP.Options".Translate(),
//                                 anchor: TextAnchor.LowerLeft, lrMargin: _margin * 2, font: GameFont.Tiny );
//                Utilities.Label( recipeColumnTitle, "FMP.Recipe".Translate(),
//                                 anchor: TextAnchor.LowerLeft, lrMargin: _margin * 2, font: GameFont.Tiny );

//                // options
//                GUI.BeginGroup( optionsColumnRect );
//                Vector2 cur = Vector2.zero;
//                float width = optionsColumnRect.width;

//                // keep track of optionIndex for shading purposes (lazy way to avoid having to redo this all the damn time).
//                var optionindex = 0;

//                // suspended
//                var suspendedRect = new Rect( cur.x, cur.y, width, _entryHeight );
//                if ( optionindex++ % 2 == 0 )
//                    Widgets.DrawAltRect( suspendedRect );
//                Utilities.DrawToggle( suspendedRect, "Suspended".Translate(), _selected.Suspended,
//                                      delegate
//                                      { _selected.Suspended = !_selected.Suspended; } );
//                cur.y += _entryHeight;

//                // store mode
//                var takeToStockRect = new Rect( cur.x, cur.y, width, _entryHeight );
//                if ( optionindex++ % 2 == 0 )
//                    Widgets.DrawAltRect( takeToStockRect );
//                Utilities.DrawToggle( takeToStockRect, "BillStoreMode_BestStockpile".Translate(),
//                                      _selected.Bill.storeMode == BillStoreMode.BestStockpile,
//                                      delegate
//                                      { _selected.Bill.storeMode = BillStoreMode.BestStockpile; },
//                                      delegate
//                                      { _selected.Bill.storeMode = BillStoreMode.DropOnFloor; } );
//                cur.y += _entryHeight;

//                // ingredient search radius (3)
//                var searchRadiusLabelRect = new Rect( cur.x, cur.y, width, _entryHeight );
//                if ( optionindex % 2 == 0 )
//                    Widgets.DrawAltRect( searchRadiusLabelRect );
//                Utilities.Label( searchRadiusLabelRect,
//                                 "IngredientSearchRadius".Translate() + ": " +
//                                 _selected.Bill.ingredientSearchRadius.ToString( " #####0" ),
//                                 anchor: TextAnchor.MiddleLeft, lrMargin: _margin );
//                cur.y += _entryHeight;

//                var searchRadiusRect = new Rect( cur.x, cur.y, width, Utilities.SliderHeight );
//                if ( optionindex++ % 2 == 0 )
//                    Widgets.DrawAltRect( searchRadiusRect );
//                _selected.Bill.ingredientSearchRadius =
//                    (int)GUI.HorizontalSlider( searchRadiusRect, _selected.Bill.ingredientSearchRadius, 0f, 250f );
//                cur.y += Utilities.SliderHeight;

//                // prioritize over manually set jobs (4)
//                var prioritizeRect = new Rect( cur.x, cur.y, width, _entryHeight );
//                if ( optionindex++ % 2 == 0 )
//                    Widgets.DrawAltRect( prioritizeRect );
//                Utilities.DrawToggle( prioritizeRect, "FMP.PrioritizeManual".Translate(),
//                                      ref ManagerJob_Production.prioritizeManual );
//                cur.y += _entryHeight;

//                // skill range (5)
//                if ( _selected.Bill.recipe.workSkill != null )
//                {
//                    var skillLabelRect = new Rect( cur.x, cur.y, width, _entryHeight );
//                    if ( optionindex % 2 == 0 )
//                        Widgets.DrawAltRect( skillLabelRect );
//                    Utilities.Label( skillLabelRect,
//                                     "FMP.AllowedSkillRange".Translate()
//                                     + ": " + _selected.Bill.allowedSkillRange );
//                    cur.y += _entryHeight;

//                    var skillRect = new Rect( cur.x, cur.y, width, Utilities.SliderHeight );
//                    if ( optionindex % 2 == 0 )
//                        Widgets.DrawAltRect( skillRect );
//                    Widgets.IntRange( skillRect, 2134112311, ref _selected.Bill.allowedSkillRange, 0, 20 );
//                    cur.y += Utilities.SliderHeight;

//                    var snapToHighestRect = new Rect( cur.x, cur.y, width, _entryHeight );
//                    if ( optionindex++ % 2 == 0 )
//                        Widgets.DrawAltRect( snapToHighestRect );
//                    Utilities.DrawToggle( snapToHighestRect, "FMP.SnapToHighestSkill".Translate(),
//                                          ref _selected.restrictToMaxSkill );
//                    cur.y += _entryHeight;
//                }

//                // draw threshold and billgiver config (6, 7)
//                _selected.Trigger.DrawTriggerConfig( ref cur, optionsColumnRect.width, _entryHeight,
//                                                     optionindex++ % 2 == 0 );
//                _selected.BillGivers.DrawBillGiverConfig( ref cur, optionsColumnRect.width, _entryHeight,
//                                                          optionindex++ % 2 == 0 );

//                // add a better recipe available notification with corresponding float menu if other recipe options are available.
//                if ( _selected.Managed && _selected.OtherRecipeAvailable() )
//                {
//                    var otherRecipeAvailableRect = new Rect( cur.x, cur.y, width, _entryHeight );
//                    Utilities.Label( otherRecipeAvailableRect, "FMP.OtherRecipeAvailable".Translate(),
//                                     "FMP.OtherRecipeAvailableTooltip".Translate() );
//                    Widgets.DrawHighlightIfMouseover( otherRecipeAvailableRect );
//                    if ( optionindex++ % 2 == 0 )
//                        Widgets.DrawAltRect( otherRecipeAvailableRect );

//                    // add a little icon to mark interactivity
//                    var searchIconRect = new Rect( otherRecipeAvailableRect.xMax - Utilities.Margin - _entryHeight,
//                                                   cur.y, _entryHeight, _entryHeight );
//                    if ( searchIconRect.height > Utilities.SmallIconSize )
//                    {
//                        // center it.
//                        searchIconRect =
//                            searchIconRect.ContractedBy( ( searchIconRect.height - Utilities.SmallIconSize ) / 2 );
//                    }
//                    GUI.DrawTexture( searchIconRect, Resources.Search );

//                    // draw a floatmenu on click
//                    if ( Widgets.ButtonInvisible( otherRecipeAvailableRect ) )
//                    {
//                        var options = new List<FloatMenuOption>();
//                        string curLabel = "Current: " + _selected.Label +
//                                          " (<i>" + string.Join( ", ", _selected.Targets ) + "</i>)";
//                        options.Add( new FloatMenuOption( curLabel, null ) );

//                        foreach ( RecipeDef recipe in _selected.OtherRecipeDefs )
//                        {
//                            string label = recipe.LabelCap +
//                                           " (<i>" +
//                                           string.Join( ", ",
//                                                        recipe.GetRecipeUsers().Select( td => td.LabelCap ).ToArray() ) +
//                                           "</i>)";
//                            Action action = delegate
//                                                {
//                                                    _selected.SetNewRecipe( recipe );
//                                                    _selected.ForceRecacheOtherRecipe();
//                                                };
//                            options.Add( new FloatMenuOption( label, action ) );
//                        }

//                        Find.WindowStack.Add( new FloatMenu( options ) );
//                    }

//                    cur.y += _entryHeight;
//                }

//                GUI.EndGroup(); // options

//                // bill
//                GUI.BeginGroup( recipeColumnRect );
//                cur = Vector2.zero;
//                width = recipeColumnRect.width;

//                // bill information
//                var infoRect = new Rect( cur.x, cur.y, width, ( recipeColumnRect.height - cur.y ) / 2 );
//                string text = GetInfoText();
//                float actualHeight = Text.CalcHeight( text, infoRect.width );

//                // if required height is small, cull info area
//                if ( infoRect.height > actualHeight )
//                {
//                    infoRect.height = actualHeight;
//                }

//                // set up scrolling region
//                Rect infoViewRect = infoRect;
//                if ( actualHeight > infoRect.height )
//                {
//                    infoViewRect.width -= 16f; // scrollbar
//                    infoViewRect.height = Text.CalcHeight( text, infoViewRect.width );
//                }

//                Widgets.BeginScrollView( infoRect, ref _infoScrollPosition, infoViewRect );
//                Utilities.Label( infoRect, text, anchor: TextAnchor.UpperLeft, lrMargin: _margin );
//                Widgets.EndScrollView();

//                // if there is one or more products known to us (so not smelting, ugh!) display an infocard button
//                if ( _selected.Bill.recipe.products.Count > 0 )
//                {
//                    Widgets.InfoCardButton( infoRect.xMax - Widgets.InfoCardButtonSize - _margin,
//                                            infoRect.yMin + _margin, _selected.Bill.recipe.products[0].thingDef );
//                }
//                cur.y += infoRect.height;

//                // ingredients label
//                var ingredientsLabelRect = new Rect( cur.x, cur.y, width, _entryHeight );
//                Utilities.Label( ingredientsLabelRect, "FMP.AllowedIngredients".Translate(),
//                                 anchor: TextAnchor.MiddleLeft, lrMargin: _margin );
//                cur.y += _entryHeight;

//                // ingredients picker, fill available space
//                var ingredientsRect = new Rect( cur.x, cur.y, width, recipeColumnRect.height - cur.y );
//                filterUI.DoThingFilterConfigWindow( ingredientsRect, ref IngredientsScrollPosition,
//                                                    _selected.Bill.ingredientFilter,
//                                                    _selected.Bill.recipe.fixedIngredientFilter, 4 );

//                GUI.EndGroup(); // bill
//            }

//            GUI.EndGroup(); // window
//        }

//        public void DoLeftRow( Rect canvas )
//        {
//            Widgets.DrawMenuSection( canvas, false );

//            // filter
//            var filterRect = new Rect( 10f, canvas.yMin + 5f, canvas.width - _leftRowEntryHeight, _entryHeight );

//            GUI.SetNextControlName( "filterTextfield" );
//            SourceFilter = Widgets.TextField( filterRect, SourceFilter );

//            if ( !_postOpenFocus )
//            {
//                GUI.FocusControl( "filterTextfield" );
//                _postOpenFocus = true;
//            }

//            if ( SourceFilter != "" )
//            {
//                var clearFilter = new Rect( filterRect.width + 10f, filterRect.yMin, _entryHeight, _entryHeight );
//                if ( Widgets.ButtonImage( clearFilter, Widgets.CheckboxOffTex ) )
//                {
//                    SourceFilter = "";
//                }
//                TooltipHandler.TipRegion( clearFilter, "FMP.ClearFilterDesc".Translate() );
//            }
//            TooltipHandler.TipRegion( filterRect, "FMP.FilterDesc".Translate() );

//            // tabs
//            var list = new List<TabRecord>();
//            var availableTabRecord = new TabRecord( "FMP.Available".Translate(), delegate
//                                                                                     {
//                                                                                         Source =
//                                                                                             SourceOptions.Available;
//                                                                                         Refresh();
//                                                                                     },
//                                                    Source == SourceOptions.Available );
//            list.Add( availableTabRecord );
//            var currentTabRecord = new TabRecord( "FMP.Current".Translate(), delegate
//                                                                                 {
//                                                                                     Source = SourceOptions.Current;
//                                                                                     Refresh();
//                                                                                 }, Source == SourceOptions.Current );
//            list.Add( currentTabRecord );
//            TabDrawer.DrawTabs( canvas, list );

//            // content
//            Rect scrollCanvas = canvas;
//            scrollCanvas.yMin = scrollCanvas.yMin + _entryHeight + _margin;
//            float height = SourceListHeight;
//            var scrollView = new Rect( 0f, 0f, scrollCanvas.width, height );
//            if ( height > scrollCanvas.height )
//            {
//                scrollView.width -= 16f;
//            }

//            Widgets.BeginScrollView( scrollCanvas, ref LeftRowScrollPosition, scrollView );
//            Rect scrollContent = scrollView;

//            GUI.BeginGroup( scrollContent );
//            float y = 0;
//            var i = 0;

//            foreach ( ManagerJob_Production current in from job in SourceList
//                                                       where
//                                                           job.Bill.recipe.label.ToUpper()
//                                                              .Contains( SourceFilter.ToUpper() ) ||
//                                                           job.MainProduct.Label.ToUpper()
//                                                              .Contains( SourceFilter.ToUpper() )
//                                                       select job )
//            {
//                var row = new Rect( 0f, y, scrollContent.width, Utilities.LargeListEntryHeight );
//                Widgets.DrawHighlightIfMouseover( row );
//                if ( _selected == current )
//                {
//                    Widgets.DrawHighlightSelected( row );
//                }

//                if ( i++ % 2 == 1 )
//                {
//                    Widgets.DrawAltRect( row );
//                }

//                Rect jobRect = row;

//                if ( Source == SourceOptions.Current )
//                {
//                    if ( ManagerTab_Overview.DrawOrderButtons(
//                                                              new Rect( row.xMax - _leftRowEntryHeight, row.yMin,
//                                                                        _leftRowEntryHeight, _leftRowEntryHeight ),
//                                                              manager,
//                                                              current ) )
//                    {
//                        Refresh();
//                    }
//                    jobRect.width -= _leftRowEntryHeight;
//                }

//                current.DrawListEntry( jobRect, false, Source == SourceOptions.Current );
//                if ( Widgets.ButtonInvisible( jobRect ) )
//                {
//                    _selected = current;
//                }

//                y += Utilities.LargeListEntryHeight;
//            }

//            SourceListHeight = y;
//            GUI.EndGroup();
//            Widgets.EndScrollView();
//        }

//        public override void DoWindowContents( Rect canvas )
//        {
//            var leftRow = new Rect( 0f, 31f, DefaultLeftRowSize, canvas.height - 31f );
//            var contentCanvas = new Rect( leftRow.xMax + Utilities.Margin, 0f,
//                                          canvas.width - leftRow.width - Utilities.Margin, canvas.height );

//            DoLeftRow( leftRow );
//            DoContent( contentCanvas );
//        }

//        public override void PostOpen()
//        {
//            // focus on the filter on open, flag is checked after the field is actually drawn.
//            _postOpenFocus = false;
//        }

//        public override void PreOpen()
//        {
//            base.PreOpen();
//            Refresh();
//            if ( _selected != null && _selected.Managed )
//                _selected.ForceRecacheOtherRecipe();
//        }

//        public void Refresh()
//        {
//            SourceList = new List<ManagerJob_Production>();

//            switch ( Source )
//            {
//                case SourceOptions.Available:
//                    SourceList = ( from rd in DefDatabase<RecipeDef>.AllDefsListForReading
//                                   where rd.AvailableNow && rd.HasBuildingRecipeUser( manager, true )
//                                   orderby rd.LabelCap
//                                   select new ManagerJob_Production( manager, rd ) ).ToList();
//                    break;

//                case SourceOptions.Current:
//                    SourceList = manager.JobStack.FullStack<ManagerJob_Production>();
//                    break;
//            }
//        }

//        private string GetInfoText()
//        {
//            var stringBuilder = new StringBuilder();

//            // add mainproduct line
//            stringBuilder.AppendLine( "FMP.MainProduct".Translate( _selected.MainProduct.Label,
//                                                                   _selected.MainProduct.Count ) );
//            stringBuilder.AppendLine();

//            if ( _selected.Bill.recipe.description != null )
//            {
//                stringBuilder.AppendLine( _selected.Bill.recipe.description );
//                stringBuilder.AppendLine();
//            }
//            stringBuilder.AppendLine( "WorkAmount".Translate() + ": " +
//                                      _selected.Bill.recipe.WorkAmountTotal( null ).ToStringWorkAmount() );
//            stringBuilder.AppendLine();
//            foreach ( IngredientCount ingredientCount in _selected.Bill.recipe.ingredients )
//            {
//                if ( !ingredientCount.filter.Summary.NullOrEmpty() )
//                {
//                    stringBuilder.AppendLine(
//                                             _selected.Bill.recipe.IngredientValueGetter.BillRequirementsDescription(
//                                                                                                                     ingredientCount ) );
//                }
//            }

//            stringBuilder.AppendLine();
//            string extraDescriptionLine =
//                _selected.Bill.recipe.IngredientValueGetter.ExtraDescriptionLine( _selected.Bill.recipe );
//            if ( extraDescriptionLine != null )
//            {
//                stringBuilder.AppendLine( extraDescriptionLine );
//                stringBuilder.AppendLine();
//            }
//            stringBuilder.AppendLine( "MinimumSkills".Translate() );
//            stringBuilder.AppendLine( _selected.Bill.recipe.MinSkillString );
//            Text.Font = GameFont.Small;
//            string infoText = stringBuilder.ToString();
//            return infoText;
//        }
//    }
//}

