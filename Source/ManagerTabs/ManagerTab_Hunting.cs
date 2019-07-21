// Karel Kroeze
// ManagerTab_Hunting.cs
// 2016-12-09

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    internal class ManagerTab_Hunting : ManagerTab
    {
        private float              _leftRowHeight  = 9999f;
        private Vector2            _scrollPosition = Vector2.zero;
        private ManagerJob_Hunting _selected;

        public List<ManagerJob_Hunting> Jobs;

        public ManagerTab_Hunting( Manager manager ) : base( manager )
        {
            _selected = new ManagerJob_Hunting( manager );
        }

        public override Texture2D Icon => Resources.IconHunting;

        public override IconAreas IconArea => IconAreas.Middle;

        public override string Label => "FMH.Hunting".Translate();

        public override ManagerJob Selected
        {
            get => _selected;
            set => _selected = (ManagerJob_Hunting) value;
        }

        public void DoContent( Rect rect )
        {
            // layout: settings | animals
            // draw background
            Widgets.DrawMenuSection( rect );

            // rects
            var optionsColumnRect = new Rect(
                rect.xMin,
                rect.yMin,
                rect.width * 3 / 5f,
                rect.height - Margin - ButtonSize.y );
            var animalsColumnRect = new Rect(
                optionsColumnRect.xMax,
                rect.yMin,
                rect.width * 2 / 5f,
                rect.height - Margin - ButtonSize.y );
            var buttonRect = new Rect(
                rect.xMax    - ButtonSize.x,
                rect.yMax    - ButtonSize.y,
                ButtonSize.x - Margin,
                ButtonSize.y - Margin );

            Vector2 position;
            float   width;

            // options
            Widgets_Section.BeginSectionColumn( optionsColumnRect, "Hunting.Options", out position, out width );
            Widgets_Section.Section( ref position, width, DrawThresholdSettings, "FM.Threshold".Translate() );
            Widgets_Section.Section( ref position, width, DrawUnforbidCorpses );
            Widgets_Section.Section( ref position, width, DrawHuntingGrounds,
                                     "FM.Hunting.AreaRestriction".Translate() );
            Widgets_Section.EndSectionColumn( "Hunting.Options", position );

            // animals
            Widgets_Section.BeginSectionColumn( animalsColumnRect, "Hunting.Animals", out position, out width );
            var refreshRect = new Rect(
                position.x + width - SmallIconSize - 2 * Margin,
                position.y                         + Margin,
                SmallIconSize,
                SmallIconSize );
            if ( Widgets.ButtonImage( refreshRect, Resources.Refresh, Color.grey ) )
                _selected.RefreshAllowedAnimals();
            Widgets_Section.Section( ref position, width, DrawAnimalShortcuts, "FMH.Animals".Translate() );
            Widgets_Section.Section( ref position, width, DrawAnimalList );
            Widgets_Section.EndSectionColumn( "Hunting.Animals", position );

            // do the button
            if ( !_selected.Managed )
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Manage".Translate() ) )
                {
                    // activate job, add it to the stack
                    _selected.Managed = true;
                    Manager.For( manager ).JobStack.Add( _selected );

                    // refresh source list
                    Refresh();
                }
            }
            else
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Delete".Translate() ) )
                {
                    // inactivate job, remove from the stack.
                    Manager.For( manager ).JobStack.Delete( _selected );

                    // remove content from UI
                    _selected = null;

                    // refresh source list
                    Refresh();
                }
            }
        }

        public float DrawThresholdSettings( Vector2 pos, float width )
        {
            var start = pos;

            // target count (1)
            var currentCount    = _selected.Trigger.CurrentCount;
            var corpseCount     = _selected.GetMeatInCorpses();
            var designatedCount = _selected.GetMeatInDesignations();
            var targetCount     = _selected.Trigger.TargetCount;

            _selected.Trigger.DrawTriggerConfig( ref pos, width, ListEntryHeight,
                                                 "FMH.TargetCount".Translate(
                                                     currentCount, corpseCount, designatedCount, targetCount ),
                                                 "FMH.TargetCountTooltip".Translate(
                                                     currentCount, corpseCount, designatedCount, targetCount ),
                                                 _selected.Designations, null, _selected.DesignationLabel );

            // allow human & insect meat (2)
            Utilities.DrawToggle( ref pos, width, "FM.PathBasedDistance".Translate(),
                                  "FM.PathBasedDistance.Tip".Translate(), ref _selected.PathBasedDistance, true );
            Utilities.DrawReachabilityToggle( ref pos, width, ref _selected.CheckReachable );
            Utilities.DrawToggle( ref pos, width, "FMH.AllowHumanMeat".Translate(),
                                  "FMH.AllowHumanMeat.Tip".Translate(),
                                  _selected.Trigger.ThresholdFilter.Allows( Utilities_Hunting.HumanMeat ),
                                  () => _selected.AllowHumanLikeMeat = true,
                                  () => _selected.AllowHumanLikeMeat = false );
            Utilities.DrawToggle( ref pos, width, "FMH.AllowInsectMeat".Translate(),
                                  "FMH.AllowInsectMeat.Tip".Translate(),
                                  _selected.Trigger.ThresholdFilter.Allows( Utilities_Hunting.InsectMeat ),
                                  () => _selected.AllowInsectMeat = true,
                                  () => _selected.AllowInsectMeat = false );

            return pos.y - start.y;
        }

        public float DrawUnforbidCorpses( Vector2 pos, float width )
        {
            // unforbid corpses (3)
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            Utilities.DrawToggle( rowRect, "FMH.UnforbidCorpses".Translate(), "FMH.UnforbidCorpses.Tip".Translate(),
                                  ref _selected.UnforbidCorpses );
            return ListEntryHeight;
        }

        public float DrawHuntingGrounds( Vector2 pos, float width )
        {
            var start = pos;
            AreaAllowedGUI.DoAllowedAreaSelectors( ref pos, width, ref _selected.HuntingGrounds, manager );
            return pos.y - start.y;
        }

        public float DrawAnimalShortcuts( Vector2 pos, float width )
        {
            var start = pos;

            // list of keys in allowed animals list (all animals in biome + visible animals on map)
            var allowed = _selected.AllowedAnimals;
            var animals = new List<PawnKindDef>( allowed.Keys );

            // toggle all
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            Utilities.DrawToggle( rowRect, "FM.All".Translate().Italic(),
                                  string.Empty,
                                  allowed.Values.All( v => v ),
                                  allowed.Values.All( v => !v ),
                                  () => animals.ForEach( a => allowed[a] = true ),
                                  () => animals.ForEach( a => allowed[a] = false ) );

            // toggle predators
            rowRect.y += ListEntryHeight;
            var predators = animals.Where( a => a.RaceProps.predator ).ToList();
            Utilities.DrawToggle( rowRect, "FM.Hunting.Predators".Translate().Italic(),
                                  "FM.Hunting.Predators.Tip".Translate(),
                                  predators.All( p => allowed[p] ),
                                  predators.All( p => !allowed[p] ),
                                  () => predators.ForEach( p => allowed[p] = true ),
                                  () => predators.ForEach( p => allowed[p] = false ) );

            // toggle herd animals
            rowRect.y += ListEntryHeight;
            var herders = animals.Where( a => a.RaceProps.herdAnimal ).ToList();
            Utilities.DrawToggle( rowRect, "FM.Hunting.HerdAnimals".Translate().Italic(),
                                  "FM.Hunting.HerdAnimals.Tip".Translate(),
                                  herders.All( h => allowed[h] ),
                                  herders.All( h => !allowed[h] ),
                                  () => herders.ForEach( h => allowed[h] = true ),
                                  () => herders.ForEach( h => allowed[h] = false ) );

            // exploding animals
            rowRect.y += ListEntryHeight;
            var exploding = animals
                           .Where( a => a.RaceProps.deathActionWorkerClass == typeof( DeathActionWorker_SmallExplosion )
                                     || a.RaceProps.deathActionWorkerClass == typeof( DeathActionWorker_BigExplosion ) )
                           .ToList();
            Utilities.DrawToggle( rowRect,
                                  "FM.Hunting.Exploding".Translate().Italic(),
                                  "FM.Hunting.Exploding.Tip".Translate(),
                                  exploding.All( e => allowed[e] ),
                                  exploding.All( e => !allowed[e] ),
                                  () => exploding.ForEach( e => allowed[e] = true ),
                                  () => exploding.ForEach( e => allowed[e] = false ) );

            return rowRect.yMax - start.y;
        }

        public float DrawAnimalList( Vector2 pos, float width )
        {
            var start = pos;
            // list of keys in allowed animals list (all animals in biome + visible animals on map)
            var allowed = _selected.AllowedAnimals;
            var animals = new List<PawnKindDef>( allowed.Keys );

            // toggle for each animal
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            foreach ( var animal in animals )
            {
                // draw the toggle
                Utilities.DrawToggle( rowRect, animal.LabelCap, animal.race.description, allowed[animal],
                                      () => allowed[animal] = !allowed[animal] );
                rowRect.y += ListEntryHeight;
            }

            return rowRect.yMin - start.y;
        }

        public void DoJobList( Rect rect )
        {
            Widgets.DrawMenuSection( rect );

            // content
            var height     = _leftRowHeight;
            var scrollView = new Rect( 0f, 0f, rect.width, height );
            if ( height > rect.height )
                scrollView.width -= ScrollbarWidth;

            Widgets.BeginScrollView( rect, ref _scrollPosition, scrollView );
            var scrollContent = scrollView;

            GUI.BeginGroup( scrollContent );
            var cur = Vector2.zero;
            var i   = 0;

            foreach ( var job in Jobs )
            {
                var row = new Rect( 0f, cur.y, scrollContent.width, LargeListEntryHeight );
                Widgets.DrawHighlightIfMouseover( row );
                if ( _selected == job ) Widgets.DrawHighlightSelected( row );

                if ( i++ % 2 == 1 ) Widgets.DrawAltRect( row );

                var jobRect = row;

                if ( ManagerTab_Overview.DrawOrderButtons( new Rect( row.xMax - 50f, row.yMin, 50f, 50f ), manager,
                                                           job ) ) Refresh();
                jobRect.width -= 50f;

                job.DrawListEntry( jobRect, false );
                if ( Widgets.ButtonInvisible( jobRect ) ) _selected = job;

                cur.y += LargeListEntryHeight;
            }

            // row for new job.
            var newRect = new Rect( 0f, cur.y, scrollContent.width, LargeListEntryHeight );
            Widgets.DrawHighlightIfMouseover( newRect );

            if ( i++ % 2 == 1 ) Widgets.DrawAltRect( newRect );

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( newRect, "<" + "FMH.NewHuntingJob".Translate() + ">" );
            Text.Anchor = TextAnchor.UpperLeft;

            if ( Widgets.ButtonInvisible( newRect ) ) Selected = new ManagerJob_Hunting( manager );

            TooltipHandler.TipRegion( newRect, "FMH.NewHuntingJobTooltip".Translate() );

            cur.y += LargeListEntryHeight;

            _leftRowHeight = cur.y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public override void DoWindowContents( Rect canvas )
        {
            // set up rects
            var leftRow = new Rect( 0f, 0f, DefaultLeftRowSize, canvas.height );
            var contentCanvas = new Rect( leftRow.xMax + Margin, 0f, canvas.width - leftRow.width - Margin,
                                          canvas.height );

            // draw overview row
            DoJobList( leftRow );

            // draw job interface if something is selected.
            if ( Selected != null )
                DoContent( contentCanvas );
        }

        public override void PreOpen()
        {
            Refresh();
        }

        public void Refresh()
        {
            // upate our list of jobs
            Jobs = Manager.For( manager ).JobStack.FullStack<ManagerJob_Hunting>();

            // update pawnkind options
            foreach ( var job in Jobs )
                job.RefreshAllowedAnimals();
            _selected?.RefreshAllowedAnimals();
        }
    }
}