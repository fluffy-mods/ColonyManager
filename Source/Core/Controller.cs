// Karel Kroeze
// Controller.cs
// 2017-05-27

using UnityEngine;
using Verse;
using Harmony;
using System.Reflection;

namespace FluffyManager
{
    public class Controller : Mod
    {
        public Controller( ModContentPack content ) : base( content ) { 
            GetSettings<Settings>();

            // apply fixes
            var harmony = HarmonyInstance.Create( "fluffy.colonymanager" );
            harmony.PatchAll( Assembly.GetExecutingAssembly() );
        }
        public override string SettingsCategory() { return "FM.HelpTitle".Translate(); }
        public override void DoSettingsWindowContents( Rect inRect ) { Settings.DoSettingsWindowContents( inRect ); }
    }
}