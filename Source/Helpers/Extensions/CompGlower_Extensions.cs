// Karel Kroeze
// CompGlower_Extensions.cs
// 2016-12-09

using RimWorld;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public static class CompGlower_Extensions
    {
        #region Fields

        private static FieldInfo _litFI = typeof( CompGlower ).GetField( "glowOnInt",
                                                                         BindingFlags.Instance | BindingFlags.NonPublic );

        #endregion Fields

        #region Methods

        public static void SetLit( this CompGlower glower, bool lit = true )
        {
            if ( _litFI == null )
                throw new Exception( "Field glowOnInt not found in CompGlower" );

            _litFI.SetValue( glower, lit );
        }

        #endregion Methods
    }
}
