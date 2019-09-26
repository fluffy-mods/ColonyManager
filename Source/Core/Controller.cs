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
        private Settings mySettings;
        public Controller( ModContentPack content ) : base( content )
        {
            // apply fixes
            var harmony = HarmonyInstance.Create( "fluffy.colonymanager" );
            harmony.PatchAll( Assembly.GetExecutingAssembly() );

            mySettings = GetSettings<Settings>();
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