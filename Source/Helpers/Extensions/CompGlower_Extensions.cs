// Karel Kroeze
// CompGlower_Extensions.cs
// 2016-12-09

using System;
using System.Reflection;
using Verse;

namespace FluffyManager
{
    public static class CompGlower_Extensions
    {
        private static readonly FieldInfo _litFI = typeof( CompGlower ).GetField( "glowOnInt",
                                                                                  BindingFlags.Instance |
                                                                                  BindingFlags.NonPublic );

        public static void SetLit( this CompGlower glower, bool lit = true )
        {
            if ( _litFI == null )
                throw new Exception( "Field glowOnInt not found in CompGlower" );

            _litFI.SetValue( glower, lit );
        }
    }
}