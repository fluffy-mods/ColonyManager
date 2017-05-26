// Karel Kroeze
// Controller.cs
// 2017-05-27

using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class Controller : Mod
    {
        public Controller( ModContentPack content ) : base( content ) { GetSettings<Settings>(); }
        public override string SettingsCategory() { return "FM.HelpTitle".Translate(); }
        public override void DoSettingsWindowContents( Rect inRect ) { Settings.DoSettingsWindowContents( inRect ); }
    }
}