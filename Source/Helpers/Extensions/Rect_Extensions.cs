// Rect_Extensions.cs
// Copyright Karel Kroeze, 2018-2018

using UnityEngine;
using Verse;

namespace FluffyManager
{
    public static class Rect_Extensions
    {
        public static Rect RoundToInt( this Rect rect )
        {
            return new Rect(
                Mathf.RoundToInt( rect.xMin ),
                Mathf.RoundToInt( rect.yMin ),
                Mathf.RoundToInt( rect.width ),
                Mathf.RoundToInt( rect.height ) );
        }

        public static Rect CenteredIn( this Rect inner, Rect outer, float x = 0f, float y = 0f )
        {
            inner   =  inner.CenteredOnXIn( outer ).CenteredOnYIn( outer );
            inner.x += x;
            inner.y += y;
            return inner;
        }
    }
}