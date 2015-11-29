// Manager/ManagerTab_Livestock.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-22 15:52

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FM
{
    public class ManagerTab_Livestock : ManagerTab
    {
        private List<PawnKindDef> _available;
        private List<ManagerJob_Livestock> _current;
        private float _entryHeight = 30f;
        private float _listEntryHeight = Utilities.LargeListEntryHeight;

        // init with 5's if new job.
        private Dictionary<ManagerJob_Livestock.AgeAndSex, string> _newCounts =
            ManagerJob_Livestock.AgeSexArray.ToDictionary( k => k, v => "5" );

        private bool _onCurrentTab;
        private Vector2 _scrollPosition = Vector2.zero;
        private PawnKindDef _selectedAvailable;
        private ManagerJob_Livestock _selectedCurrent;
        private float _topAreaHeight = 30f;

        // public override Texture2D Icon { get; }
        public override IconAreas IconArea => IconAreas.Middle;
        public override string Label => "FML.Livestock".Translate();
        public override Texture2D Icon => Resources.IconLivestock;

        public override ManagerJob Selected
        {
            get { return _selectedCurrent; }
            set
            {
                // set tab to current if we're moving to an actual job.
                // in either case, available selection can be cleared.
                _onCurrentTab = value != null;
                _selectedAvailable = null;
                _selectedCurrent = (ManagerJob_Livestock)value;
                _newCounts = _selectedCurrent?.Trigger?.CountTargets.ToDictionary( k => k.Key, v => v.Value.ToString() );
            }
        }

        public override void PreOpen()
        {
            Refresh();
        }

        private void Refresh()
        {
            // concatenate lists of animals on biome and animals in colony.
            _available = Find.Map.Biome.AllWildAnimals.ToList();
            _available.AddRange(
                Find.ListerPawns.PawnsInFaction( Faction.OfColony )
                    .Where( p => p.RaceProps.Animal )
                    .Select( p => p.kindDef ) );
            _available = _available.Distinct().OrderBy( def => def.LabelCap ).ToList();

            // currently managed
            _current = Manager.Get.JobStack.FullStack<ManagerJob_Livestock>();
        }

        public override void DoWindowContents( Rect canvas )
        {
            Rect leftRow = new Rect( 0f, 31f, DefaultLeftRowSize, canvas.height - 31f );
            Rect contentCanvas = new Rect( leftRow.xMax + Utilities.Margin, 0f,
                                           canvas.width - leftRow.width - Utilities.Margin, canvas.height );

            DoLeftRow( leftRow );
            DoContent( contentCanvas );
        }

        private void DoContent( Rect rect )
        {
            // cop out if nothing is selected.
            if ( _selectedCurrent == null )
            {
                return;
            }

            // background
            Widgets.DrawMenuSection( rect );

            // begin window
            GUI.BeginGroup( rect );
            rect = rect.AtZero();

            // rects
            Rect optionsColumnRect = new Rect( Utilities.Margin / 2,
                                               _topAreaHeight,
                                               rect.width / 2 - Utilities.Margin,
                                               rect.height - _topAreaHeight - Utilities.Margin - Utilities.ButtonSize.y );
            Rect animalsRect = new Rect( optionsColumnRect.xMax + Utilities.Margin,
                                         _topAreaHeight,
                                         rect.width / 2 - Utilities.Margin,
                                         rect.height - _topAreaHeight - Utilities.Margin - Utilities.ButtonSize.y );

            Rect optionsColumnTitle = new Rect( optionsColumnRect.xMin,
                                                0f,
                                                optionsColumnRect.width,
                                                _topAreaHeight );
            Rect animalsColumnTitle = new Rect( animalsRect.xMin,
                                                0f,
                                                animalsRect.width,
                                                _topAreaHeight );

            // backgrounds
            GUI.DrawTexture( optionsColumnRect, Resources.SlightlyDarkBackground );
            GUI.DrawTexture( animalsRect, Resources.SlightlyDarkBackground );

            // titles
            Utilities.Label( optionsColumnTitle, "FMP.Options".Translate(),
                             anchor: TextAnchor.LowerLeft, lrMargin: Utilities.Margin * 2, font: GameFont.Tiny );
            Utilities.Label( animalsColumnTitle, "FML.Animals".Translate(),
                             anchor: TextAnchor.LowerLeft, lrMargin: Utilities.Margin * 2, font: GameFont.Tiny );

            // options
            GUI.BeginGroup( optionsColumnRect );
            Vector2 cur = Vector2.zero;
            int optionIndex = 1;

            // counts header
            Utilities.Label( ref cur, optionsColumnRect.width, _entryHeight, "FML.TargetCounts".Translate(), alt: optionIndex % 2 == 0 );

            // counts table
            int cols = 3;
            float fifth = optionsColumnRect.width / 5;
            float[] widths = { fifth, fifth * 2, fifth * 2 };
            float[] heights = { _entryHeight / 3 * 2, _entryHeight, _entryHeight };

            // set up a 3x3 table of rects
            Rect[,] countRects = new Rect[cols, cols];
            for ( int x = 0; x < cols; x++ )
            {
                for ( int y = 0; y < cols; y++ )
                {
                    // kindof overkill for a 3x3 table, but ok.
                    countRects[x, y] = new Rect( widths.Take( x ).Sum(), cur.y + heights.Take( y ).Sum(), widths[x], heights[y] );
                    if (optionIndex % 2 == 0) Widgets.DrawAltRect(countRects[x,y]);
                }
            }
            optionIndex++;

            // headers
            Utilities.Label( countRects[1, 0], Gender.Female.ToString(), null, TextAnchor.LowerCenter, font: GameFont.Tiny );
            Utilities.Label( countRects[2, 0], Gender.Male.ToString(), null, TextAnchor.LowerCenter, font: GameFont.Tiny );
            Utilities.Label( countRects[0, 1], "FML.Adult".Translate(), null, TextAnchor.MiddleRight, font: GameFont.Tiny );
            Utilities.Label( countRects[0, 2], "FML.Juvenile".Translate(), null, TextAnchor.MiddleRight, font: GameFont.Tiny );

            // fields
            DoCountField( countRects[1, 1], ManagerJob_Livestock.AgeAndSex.AdultFemale );
            DoCountField( countRects[2, 1], ManagerJob_Livestock.AgeAndSex.AdultMale );
            DoCountField( countRects[1, 2], ManagerJob_Livestock.AgeAndSex.JuvenileFemale );
            DoCountField( countRects[2, 2], ManagerJob_Livestock.AgeAndSex.JuvenileMale );
            cur.y += 3 * _entryHeight;

            // restrict to area
            Rect restrictAreaRect = new Rect(cur.x, cur.y, optionsColumnRect.width, _entryHeight);
            if ( optionIndex % 2 == 0 ) Widgets.DrawAltRect( restrictAreaRect );
            Utilities.DrawToggle(restrictAreaRect, "FML.RestrictToArea".Translate(), ref _selectedCurrent.RestrictToArea);
            cur.y += _entryHeight;
            if ( _selectedCurrent.RestrictToArea )
            {            
                // area selectors table
                // set up a 3x3 table of rects
                Rect[,] areaRects = new Rect[cols, cols];
                for( int x = 0; x < cols; x++ )
                {
                    for( int y = 0; y < cols; y++ )
                    {
                        // kindof overkill for a 3x3 table, but ok.
                        areaRects[x, y] = new Rect( widths.Take( x ).Sum(), cur.y + heights.Take( y ).Sum(), widths[x], heights[y] );
                        if( optionIndex % 2 == 0 ) Widgets.DrawAltRect( areaRects[x, y] );
                    }
                }

                // headers
                Utilities.Label( areaRects[1, 0], Gender.Female.ToString(), null, TextAnchor.LowerCenter, font: GameFont.Tiny );
                Utilities.Label( areaRects[2, 0], Gender.Male.ToString(), null, TextAnchor.LowerCenter, font: GameFont.Tiny );
                Utilities.Label( areaRects[0, 1], "FML.Adult".Translate(), null, TextAnchor.MiddleRight, font: GameFont.Tiny );
                Utilities.Label( areaRects[0, 2], "FML.Juvenile".Translate(), null, TextAnchor.MiddleRight, font: GameFont.Tiny );

                // do the selectors
                _selectedCurrent.RestrictArea[0] = AreaAllowedGUI.DoAllowedAreaSelectors( areaRects[1, 1], _selectedCurrent.RestrictArea[0], AllowedAreaMode.Animal, Utilities.Margin );
                _selectedCurrent.RestrictArea[1] = AreaAllowedGUI.DoAllowedAreaSelectors( areaRects[2, 1], _selectedCurrent.RestrictArea[1], AllowedAreaMode.Animal, Utilities.Margin );
                _selectedCurrent.RestrictArea[2] = AreaAllowedGUI.DoAllowedAreaSelectors( areaRects[1, 2], _selectedCurrent.RestrictArea[2], AllowedAreaMode.Animal, Utilities.Margin );
                _selectedCurrent.RestrictArea[3] = AreaAllowedGUI.DoAllowedAreaSelectors( areaRects[2, 2], _selectedCurrent.RestrictArea[3], AllowedAreaMode.Animal, Utilities.Margin );

                cur.y += 3 * _entryHeight;

            }
            optionIndex++;

            // train
            Utilities.Label(ref cur, optionsColumnRect.width, _entryHeight, "FML.Training".Translate(), alt: optionIndex % 2 == 0 );
            Rect trainingRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
            if( optionIndex++ % 2 == 0 ) Widgets.DrawAltRect( trainingRect );
            _selectedCurrent.DrawTrainingSelector( trainingRect, Utilities.Margin );
            cur.y += _entryHeight;
            
            // butchery stuff
            Rect butcherExcessRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
            if( optionIndex++ % 2 == 0 ) Widgets.DrawAltRect( butcherExcessRect );
            cur.y += _entryHeight;
            Rect butcherTrainedRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
            if( optionIndex++ % 2 == 0 ) Widgets.DrawAltRect( butcherTrainedRect );
            cur.y += _entryHeight;

            Utilities.DrawToggle( butcherExcessRect, "FML.ButcherExcess".Translate(), ref _selectedCurrent.ButcherExcess );
            Utilities.DrawToggle( butcherTrainedRect, "FML.ButcherTrained".Translate(),
                                  ref _selectedCurrent.ButcherTrained );

            // try tame more?
            Rect tameMoreRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
            cur.y += _entryHeight;

            Utilities.DrawToggle( tameMoreRect, "FML.TameMore".Translate(), ref _selectedCurrent.TryTameMore );
            if( optionIndex % 2 == 0 ) Widgets.DrawAltRect( tameMoreRect );

            // area to train from (if taming more);
            if ( _selectedCurrent.TryTameMore )
            {
                Rect tameAreaRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
                if( optionIndex % 2 == 0 ) Widgets.DrawAltRect( tameAreaRect );
                cur.y += _entryHeight;
                Rect tameAreaSelectorRect = new Rect(cur.x, cur.y, optionsColumnRect.width, _entryHeight);
                if( optionIndex % 2 == 0 ) Widgets.DrawAltRect( tameAreaSelectorRect );
                cur.y += _entryHeight;

                Utilities.Label( tameAreaRect, "FML.TameArea".Translate() );
                AreaAllowedGUI.DoAllowedAreaSelectors( tameAreaSelectorRect, ref _selectedCurrent.TameArea, AllowedAreaMode.Any, Utilities.Margin );

                // why am I getting an error for not being at upperleft? Oh well, force it.
                Text.Anchor = TextAnchor.UpperLeft;
            }
            optionIndex++;

            GUI.EndGroup(); // options

            // Start animals list
            GUI.BeginGroup(animalsRect);
            cur = Vector2.zero;

            GUI.EndGroup(); // animals

            // bottom button
            Rect buttonRect = new Rect( rect.xMax - Utilities.ButtonSize.x, rect.yMax - Utilities.ButtonSize.y, Utilities.ButtonSize.x - Utilities.Margin,
                                            Utilities.ButtonSize.y - Utilities.Margin );

            // add / remove to the stack
            if( _selectedCurrent.Managed )
            {
                if( Widgets.TextButton( buttonRect, "FM.Delete".Translate() ) )
                {
                    _selectedCurrent.Delete();
                    _selectedCurrent = null;
                    _onCurrentTab = false;
                    Refresh();
                    return; // just skip to the next tick to avoid null reference errors.
                }
                TooltipHandler.TipRegion( buttonRect, "FMP.DeleteBillTooltip".Translate() );
            }
            else
            {
                    if( Widgets.TextButton( buttonRect, "FM.Manage".Translate() ) )
                    {
                        _selectedCurrent.Managed = true;
                        _onCurrentTab = true;
                        Manager.Get.JobStack.Add( _selectedCurrent );
                        Refresh();
                    }
                    TooltipHandler.TipRegion( buttonRect, "FMP.ManageBillTooltip".Translate() );
            }

            GUI.EndGroup(); // window
        }

        private void DoCountField( Rect rect, ManagerJob_Livestock.AgeAndSex ageSex )
        {
            if ( _newCounts == null ||
                 _newCounts[ageSex] == null )
            {
                _newCounts = _selectedCurrent?.Trigger?.CountTargets.ToDictionary( k => k.Key, v => v.Value.ToString() );
            }

            if ( !_newCounts[ageSex].IsInt() )
            {
                GUI.color = Color.red;
            }
            else
            {
                _selectedCurrent.Trigger.CountTargets[ageSex] = int.Parse( _newCounts[ageSex] );
            }
            _newCounts[ageSex] = Widgets.TextField( rect.ContractedBy( 1f ), _newCounts[ageSex] );
            GUI.color = Color.white;
        }

        private void DoLeftRow( Rect rect )
        {
            // background (minus top line so we can draw tabs.)
            Widgets.DrawMenuSection( rect, false );

            // tabs
            List<TabRecord> tabs = new List<TabRecord>();
            TabRecord availableTabRecord = new TabRecord( "FMP.Available".Translate(), delegate
            {
                _onCurrentTab = false;
                Refresh();
            }, !_onCurrentTab );
            tabs.Add( availableTabRecord );
            TabRecord currentTabRecord = new TabRecord( "FMP.Current".Translate(), delegate
            {
                _onCurrentTab = true;
                Refresh();
            }, _onCurrentTab );
            tabs.Add( currentTabRecord );

            TabDrawer.DrawTabs( rect, tabs );

            // start the actual content.
            Rect outRect = rect;
            Rect viewRect = outRect.AtZero();

            if ( _onCurrentTab )
            {
                DrawCurrentJobList( outRect, viewRect );
            }
            else
            {
                DrawAvailableJobList( outRect, viewRect );
            }
        }

        private void DrawAvailableJobList( Rect outRect, Rect viewRect )
        {
            // set sizes
            viewRect.height = _available.Count * _listEntryHeight;
            if ( viewRect.height > outRect.height )
            {
                viewRect.width -= 16f;
            }

            Widgets.BeginScrollView( outRect, ref _scrollPosition, viewRect );
            GUI.BeginGroup( viewRect );

            for ( int i = 0; i < _available.Count; i++ )
            {
                // set up rect
                Rect row = new Rect( 0f, _listEntryHeight * i, viewRect.width, _listEntryHeight );

                // highlights
                Widgets.DrawHighlightIfMouseover( row );
                if ( i % 2 == 0 )
                {
                    Widgets.DrawAltRect( row );
                }
                if ( _available[i] == _selectedAvailable )
                {
                    Widgets.DrawHighlightSelected( row );
                }

                // draw label
                Utilities.Label( row, _available[i].LabelCap, null, TextAnchor.MiddleLeft, Utilities.Margin * 2 );

                // button
                if ( Widgets.InvisibleButton( row ) )
                {
                    _selectedAvailable = _available[i]; // for highlighting to work
                    Selected = new ManagerJob_Livestock( _available[i] ); // for details
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        private void DrawCurrentJobList( Rect outRect, Rect viewRect )
        {
            // set sizes
            viewRect.height = _current.Count * _listEntryHeight;
            if ( viewRect.height > outRect.height )
            {
                viewRect.width -= 16f;
            }

            Widgets.BeginScrollView( outRect, ref _scrollPosition, viewRect );
            GUI.BeginGroup( viewRect );

            for ( int i = 0; i < _current.Count; i++ )
            {
                // set up rect
                Rect row = new Rect( 0f, _listEntryHeight * i, viewRect.width, _listEntryHeight );

                // highlights
                Widgets.DrawHighlightIfMouseover( row );
                if ( i % 2 == 0 )
                {
                    Widgets.DrawAltRect( row );
                }
                if ( _current[i] == _selectedCurrent )
                {
                    Widgets.DrawHighlightSelected( row );
                }

                // draw label
                _current[i].DrawListEntry( row, false, true );

                // button
                if ( Widgets.InvisibleButton( row ) )
                {
                    Selected = _current[i];
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();
        }
    }
}