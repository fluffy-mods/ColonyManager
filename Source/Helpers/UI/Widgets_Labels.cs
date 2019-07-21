// Label.cs
// Copyright Karel Kroeze, 2017-2017

using UnityEngine;
using Verse;

namespace FluffyManager
{
    public static class Widgets_Labels
    {
        public static void Label( Rect rect, string label, TextAnchor anchor = TextAnchor.UpperLeft,
                                  GameFont font = GameFont.Small, Color? color = null, float margin = 0f,
                                  bool wrap = true )
        {
            rect.xMin += margin;
            Begin( anchor, font, color ?? Color.white, wrap );
            Widgets.Label( rect, label );
            End();
        }

        public static void Label( Rect rect, string label, string tooltip, TextAnchor anchor = TextAnchor.UpperLeft,
                                  GameFont font = GameFont.Small, Color? color = null, float margin = 0f,
                                  bool wrap = true )
        {
            if ( !tooltip.NullOrEmpty() )
                TooltipHandler.TipRegion( rect, tooltip );
            else
                Logger.Debug( $"Label({rect}, {label}) called with null tooltip. This should never happen." );
            Label( rect, label, anchor, font, color, margin );
        }


        public static void Label( ref Vector2 position, float width, float height, string label,
                                  TextAnchor anchor = TextAnchor.UpperLeft,
                                  GameFont font = GameFont.Small, Color? color = null, float margin = 0f,
                                  bool wrap = true )
        {
            var labelRect = new Rect( position.x, position.y, width, height );
            position.y += height;
            Label( labelRect, label, anchor, font, color, margin, wrap );
        }


        private static void Begin( TextAnchor anchor, GameFont font, Color color, bool wrap )
        {
            GUI.color     = color;
            Text.Anchor   = anchor;
            Text.Font     = font;
            Text.WordWrap = wrap;
        }

        private static void End()
        {
            GUI.color     = Color.white;
            Text.Font     = GameFont.Small;
            Text.Anchor   = TextAnchor.UpperLeft;
            Text.WordWrap = true;
        }
    }
}