//// ManagerJob_Mining.cs
//// Copyright Karel Kroeze, 2017-2017

//using RimWorld;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using Verse;
//using static FluffyManager.Constants;

//namespace FluffyManager
//{
//    public class ManagerJob_Mining: ManagerJob
//    {
//        public Dictionary<ThingDef, bool> AllowedMinerals = new Dictionary<ThingDef, bool>();
//        public List<Designation> Designations = new List<Designation>();
//        public History History;
//        public Area MiningArea;
//        public new Trigger_Threshold Trigger;
//        private Utilities.CachedValue<int> _designatedCachedValue = new Utilities.CachedValue<int>();
//        private Utilities.CachedValue<int> _chunksCachedValue = new Utilities.CachedValue<int>();

//        public bool DeconstructChunks = true;
//        public bool DeconstructBuildings = false;
//        public bool CheckRoofSupport = true;
//        public bool CheckRoofSupportAdvanced = false;
//        public bool CheckRoomDivision = true;

//        public ManagerJob_Mining( Manager manager ) : base( manager )
//        {
//            // populate the trigger field, set the root category to meats and allow all but human & insect meat.
//            Trigger = new Trigger_Threshold( this );

//            // start the history tracker;
//            History = new History(new[] { "stock", "chunks", "designated" },
//                new[] { Color.white, new Color(.7f, .7f, .7f), new Color(.4f, .4f, .4f) });

//            // init stuff if we're not loading
//            if (Scribe.mode == LoadSaveMode.Inactive)
//                RefreshAllowedMinerals();
//        }

//        public override string Label => "FM.Mining".Translate();
//        public override ManagerTab Tab => manager.Tabs.Find(tab => tab is ManagerTab_Mining );

//        public override string[] Targets => AllowedMinerals.Keys
//            .Where( key => AllowedMinerals[key] )
//            .Select( pk => pk.LabelCap ).ToArray();

//        public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Mining;
//        public override void CleanUp()
//        {
//            RemoveObsoleteDesignations();
//            foreach( var designation in Designations )
//                designation.Delete();

//            Designations.Clear();
//        }

//        private void RemoveObsoleteDesignations()
//        {
//            // get the intersection of bills in the game and bills in our list.
//            var designations = manager.map.designationManager.allDesignations.Where( d =>
//                ( d.def == DesignationDefOf.Mine || d.def == DesignationDefOf.Deconstruct ) &&
//                ( !d.target.HasThing || d.target.Thing.Map == manager.map ) ); // equates to SpawnedDesignationsOfDef, with two defs.
//            Designations = Designations.Intersect( designations ).ToList();
//        }

//        public override void DrawListEntry(Rect rect, bool overview = true, bool active = true)
//        {
//            // (detailButton) | name | (bar | last update)/(stamp) -> handled in Utilities.DrawStatusForListEntry
//            int shownTargets = overview ? 4 : 3; // there's more space on the overview

//            // set up rects
//            Rect labelRect = new Rect(Margin, Margin, rect.width -
//                                                      (active ? StatusRectWidth + 4 * Margin : 2 * Margin),
//                    rect.height - 2 * Margin),
//                statusRect = new Rect(labelRect.xMax + Margin, Margin, StatusRectWidth, rect.height - 2 * Margin);

//            // create label string
//            string text = Label + "\n";
//            string subtext = string.Join(", ", Targets);
//            if (subtext.Fits(labelRect))
//                text += subtext.Italic();
//            else
//                text += "multiple".Translate().Italic();

//            // do the drawing
//            GUI.BeginGroup(rect);

//            // draw label
//            Widgets_Labels.Label(labelRect, text, subtext, TextAnchor.MiddleLeft, margin: Margin);

//            // if the bill has a manager job, give some more info.
//            if (active)
//            {
//                this.DrawStatusForListEntry(statusRect, Trigger);
//            }
//            GUI.EndGroup();
//        }

//        public override void DrawOverviewDetails( Rect rect )
//        {
//            History.DrawPlot( rect, Trigger.TargetCount );
//        }

//        public override void ExposeData()
//        {
//            base.ExposeData();

//            Scribe_References.Look( ref MiningArea, "MiningArea" );
//            Scribe_Deep.Look( ref Trigger, "Trigger", manager );
//            Scribe_Collections.Look( ref AllowedMinerals, "AllowedMinerals", LookMode.Def, LookMode.Value );
//            Scribe_Values.Look( ref DeconstructChunks, "DeconstructChunks", true );
//            Scribe_Values.Look( ref DeconstructBuildings, "DeconstructBuildings", false );
//            Scribe_Values.Look( ref CheckRoofSupport, "CheckRoofSupport", true );
//            Scribe_Values.Look( ref CheckRoofSupportAdvanced, "CheckRoofSupportAdvanced", false );
//            Scribe_Values.Look( ref CheckRoomDivision, "CheckRoomDivision", true );

//            // don't store history in import/export mode.
//            if (Manager.LoadSaveMode == Manager.Modes.Normal)
//            {
//                Scribe_Deep.Look(ref History, "History");
//            }
//        }

//        public override void Tick()
//        {
//            History.Update( Trigger.CurCount, GetMineralsInChunks(), GetMineralsInDesignations() );
//        }

//        public override bool TryDoJob()
//        {
//            var workDone = false;

//            RemoveObsoleteDesignations();
//            AddRelevantGameDesignations();

//            int count = Trigger.CurCount + GetMineralsInChunks() + GetMineralsInDesignations();

//            if ( DeconstructChunks )
//            {
//                var chunks = GetDeconstructableChunks();
//                for (int i = 0; i < chunks.Length && count < Trigger.TargetCount; i++)
//                {
//                    AddDesignation( chunks[i], DesignationDefOf.Deconstruct );
//                    count += chunks[i].Yield;
//                }
//            }

//            if ( DeconstructBuildings )
//            {
//                var buildings = GetDeconstructableBuildings();
//                for ( int i = 0; i < buildings.Length && count < Trigger.TargetCount; i++ )
//                {
//                    AddDesignation( buildings[i], DesignationDefOf.Deconstruct );
//                    count += buildings[i].Yield;
//                }
//            }

//            var minerals = GetMinableMineralsSorted();
//            for ( int i = 0; i < minerals.Length && count < Trigger.TargetCount; i++ )
//            {
//                AddDesignation( minerals[i], DesignationDefOf.Mine );
//                count += minerals[i].Yield;
//            }

//            return workDone;
//        }

//        public void AddRelevantGameDesignations()
//        {
//            foreach ( Designation des in manager.map.designationManager
//                .SpawnedDesignationsOfDef( DesignationDefOf.Mine )
//                .Except( Designations )
//                .Where( des => IsValidMiningTarget( des.target ) ) )
//            {
//                AddDesignation( des );
//            }
//            foreach ( Designation des in manager.map.designationManager
//                .SpawnedDesignationsOfDef( DesignationDefOf.Deconstruct )
//                .Except( Designations )
//                .Where( des => IsValidDeconstructionTarget( des.target ) ) )
//            {
//                AddDesignation( des );
//            }
//        }

//        public bool IsValidMiningTarget( LocalTargetInfo target )
//        {
//            return target.HasThing
//                   && target.IsValid
//                   && IsValidMiningTarget( target.Thing );
//        }

//        public bool IsValidMiningTarget( Thing mineral )
//        {
//            // mineable
//            return mineral.def.mineable

//                   // allowed
//                   && AllowedMinerals.ContainsKey( mineral.def )
//                   && AllowedMinerals[mineral.def]

//                   // not yet designated
//                   && manager.map.designationManager.DesignationOn( mineral ) == null

//                   // matches settings
//                   && IsInAllowedArea( mineral)
//                   && IsNotARoomDivider(mineral)
//                   && IsNotARoofSupport( mineral )

//                   // can be reached
//                   && manager.map.reachability.CanReachColony( mineral.Position );
//        }

//        public bool IsValidDeconstructionTarget( Building target )
//        {
//            return target.Spawned

//                   // not ours
//                   && target.Faction != Faction.OfPlayer

//                   // allowed
//                   && !target.IsForbidden( Faction.OfPlayer )
//                   && AllowedBuildings.ContainsKey( target.def )
//                   && AllowedBuildings[target.def]

//                   // in allowed area & reachable
//                   && IsInAllowedArea( target )
//                   && manager.map.reachability.CanReachColony( target.Position );
//            manager.map.reachability.CanReachUnfogged(  )
//        }

//        public bool IsInAllowedArea( Thing target )
//        {
//            return MiningArea == null || MiningArea.ActiveCells.Contains( target.Position );
//        }

//        public bool IsNotARoofSupport( Thing target )
//        {
//            if ( !CheckRoofSupport )
//                return true;

//            if ( CheckRoofSupportAdvanced )
//            {
//                //
//            }
//            else
//            {
//                //
//            }
//        }

//        public bool IsNotARoomDivider( Thing target )
//        {
//            if ( !CheckRoomDivision )
//                return true;

//            //
//        }

//        public bool IsValidDeconstructionTarget(LocalTargetInfo target)
//        {
//            return target.HasThing
//                   && target.IsValid
//                   && target.Thing is Building
//                   && IsValidDeconstructionTarget( target.Thing );
//        }

//        public bool IsValidDeconstructionTarget( Building target )
//        {
            
//        }

//        public void AddDesignation( Designation designation )
//        {
//            manager.map.designationManager.AddDesignation(designation);
//            Designations.Add(designation);
//        }


//        private void AddDesignation( Thing target, DesignationDef designationDef )
//        {
//            AddDesignation( new Designation( target, designationDef ) );
//        }

//        public void RefreshAllowedMinerals()
//        {
//            TODO_IMPLEMENT_ME();
//        }
//    }
//}