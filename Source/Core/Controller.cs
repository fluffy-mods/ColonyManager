// Karel Kroeze
// Controller.cs
// 2017-05-27

using System.Reflection;
using Harmony;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class Controller : Mod
    {
        public Controller( ModContentPack content ) : base( content )
        {
            GetSettings<Settings>();

            // apply fixes
            var harmony = HarmonyInstance.Create( "fluffy.colonymanager" );
            harmony.PatchAll( Assembly.GetExecutingAssembly() );
        }

        public override string SettingsCategory()
        {
            return "FM.HelpTitle".Translate();
        }

        public override void DoSettingsWindowContents( Rect inRect )
        {
            Settings.DoSettingsWindowContents( inRect );
        }
    }
}