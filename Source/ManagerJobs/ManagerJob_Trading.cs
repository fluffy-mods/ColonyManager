// // Karel Kroeze
// // ManagerJob_Trading.cs
// // 2016-12-09

// using RimWorld;
// using System;
// using UnityEngine;
// using Verse;

// namespace FluffyManager
// {
//     public class ManagerJob_Trading : ManagerJob
//     {
//         #region Fields

//         private bool _completed;
//         private string _label;
//         private ManagerTab _tab;
//         private string[] _targets;
//         private WorkTypeDef _workTypeDef;

//         #endregion Fields

//         #region Constructors

//         public ManagerJob_Trading( Manager manager ) : base( manager )
//         {
//         }

//         #endregion Constructors

//         #region Properties

//         public override bool Completed => false;
//         public override string Label => "FMT.Trading".Translate();

//         // Trading jobs will never be managed in the sense that it requires a manager to interact.
//         // It does require a trader to do things, but that's further down the line.
//         public override bool Managed => false;

//         public override ManagerTab Tab => manager.Tabs.Find( tab => tab is ManagerTab_Trading );
//         public override string[] Targets => new[] { "" };
//         public override WorkTypeDef WorkTypeDef => WorkTypeDefOf.Warden;

//         #endregion Properties

//         #region Methods

//         public override void CleanUp()
//         {
//             throw new NotImplementedException();
//         }

//         public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
//         {
//             throw new NotImplementedException();
//         }

//         public override void DrawOverviewDetails( Rect rect )
//         {
//             throw new NotImplementedException();
//         }

//         public override bool TryDoJob()
//         {
//             Log.Warning( "Manager tried doing job " + ToString() + ". Trading jobs should never be directly managed." );
//             return false;
//         }

//         #endregion Methods
//     }
// }

