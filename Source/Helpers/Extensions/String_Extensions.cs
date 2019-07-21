using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public static class String_Extensions
    {
        private static readonly Dictionary<Pair<string, Rect>, bool> _fitsCache =
            new Dictionary<Pair<string, Rect>, bool>();

        public static bool Fits( this string text, Rect rect )
        {
            var  key = new Pair<string, Rect>( text, rect );
            bool result;
            if ( _fitsCache.TryGetValue( key, out result ) )
                return result;

            // make sure WW is temporarily turned off.
            var WW = Text.WordWrap;
            Text.WordWrap = false;
            result        = Text.CalcSize( text ).x < rect.width;
            Text.WordWrap = WW;

            _fitsCache.Add( key, result );
            return result;
        }

        public static string Italic( this string text )
        {
            return $"<i>{text}</i>";
        }

        public static string Bold( this string text )
        {
            return $"<b>{text}</b>";
        }
    }
}