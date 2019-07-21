// Karel Kroeze
// HSV_Helper.cs
// 2016-12-09

using UnityEngine;

namespace FluffyManager
{
    internal class HSV_Helper
    {
        /// <summary>
        ///     From http://answers.unity3d.com/questions/701956/hsv-to-rgb-without-editorguiutilityhsvtorgb.html
        /// </summary>
        /// <param name="H"></param>
        /// <param name="S"></param>
        /// <param name="V"></param>
        /// <param name="A"></param>
        /// <returns>Color</returns>
        public static Color HSVtoRGB( float H, float S, float V, float A = 1f )
        {
            if ( S == 0f ) return new Color( V, V, V, A );
            if ( V == 0f ) return new Color( 0f, 0f, 0f, A );

            var col  = Color.black;
            var Hval = H * 6f;
            var sel  = Mathf.FloorToInt( Hval );
            var mod  = Hval - sel;
            var v1   = V * ( 1f - S );
            var v2   = V * ( 1f - S * mod );
            var v3   = V * ( 1f - S * ( 1f - mod ) );
            switch ( sel + 1 )
            {
                case 0:
                    col.r = V;
                    col.g = v1;
                    col.b = v2;
                    break;

                case 1:
                    col.r = V;
                    col.g = v3;
                    col.b = v1;
                    break;

                case 2:
                    col.r = v2;
                    col.g = V;
                    col.b = v1;
                    break;

                case 3:
                    col.r = v1;
                    col.g = V;
                    col.b = v3;
                    break;

                case 4:
                    col.r = v1;
                    col.g = v2;
                    col.b = V;
                    break;

                case 5:
                    col.r = v3;
                    col.g = v1;
                    col.b = V;
                    break;

                case 6:
                    col.r = V;
                    col.g = v1;
                    col.b = v2;
                    break;

                case 7:
                    col.r = V;
                    col.g = v3;
                    col.b = v1;
                    break;
            }

            col.r = Mathf.Clamp( col.r, 0f, 1f );
            col.g = Mathf.Clamp( col.g, 0f, 1f );
            col.b = Mathf.Clamp( col.b, 0f, 1f );
            col.a = Mathf.Clamp( A, 0f, 1f );
            return col;
        }

        public static Color[] Range( int n )
        {
            var cols                              = new Color[n];
            for ( var i = 0; i < n; i++ ) cols[i] = HSVtoRGB( i / (float) n, 1f, 1f );

            return cols;
        }
    }
}