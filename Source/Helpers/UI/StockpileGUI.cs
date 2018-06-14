// Karel Kroeze
// StockpileGUI.cs
// 2016-12-09

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FluffyManager
{
    public class StockpileGUI
    {
        #region Fields

        private static List<Texture2D> textures;

        #endregion Fields



        #region Methods

        // RimWorld.AreaAllowedGUI
        public static void DoStockpileSelectors( Rect rect, ref Zone_Stockpile stockpile, Map map )
        {
            // get all stockpiles
            List<Zone_Stockpile> allStockpiles = map.zoneManager.AllZones.OfType<Zone_Stockpile>().ToList();

            // count + 1 for all stockpiles
            int areaCount = allStockpiles.Count + 1;

            // create colour swatch
            if ( textures == null || textures.Count != areaCount - 1 )
                CreateTextures( allStockpiles );

            float widthPerCell = rect.width / areaCount;
            Text.WordWrap = false;
            Text.Font = GameFont.Tiny;
            var nullAreaRect = new Rect( rect.x, rect.y, widthPerCell, rect.height );
            DoZoneSelector( nullAreaRect, ref stockpile, null, BaseContent.GreyTex );
            var areaIndex = 1;
            for ( var j = 0; j < allStockpiles.Count; j++ )
            {
                float xOffset = areaIndex * widthPerCell;
                var stockpileRect = new Rect( rect.x + xOffset, rect.y, widthPerCell, rect.height );
                DoZoneSelector( stockpileRect, ref stockpile, allStockpiles[j], textures[j] );
                areaIndex++;
            }

            Text.WordWrap = true;
            Text.Font = GameFont.Small;
        }

        private static void CreateTextures( List<Zone_Stockpile> zones )
        {
            if ( textures != null )
            {
                foreach ( Texture2D tex in textures )
                {
                    Texture2D.DestroyImmediate( tex );
                }

                textures.Clear();
            }
            else
            {
                textures = new List<Texture2D>();
            }

            foreach ( Zone_Stockpile zone in zones )
            {
                textures.Add( SolidColorMaterials.NewSolidColorTexture( zone.color ) );
            }
        }

        // RimWorld.AreaAllowedGUI
        private static void DoZoneSelector( Rect rect, ref Zone_Stockpile zoneAllowed, Zone_Stockpile zone,
                                            Texture2D tex )
        {
            rect = rect.ContractedBy( 1f );
            GUI.DrawTexture( rect, tex );
            Text.Anchor = TextAnchor.MiddleLeft;
            string label = zone?.label ?? "Any stockpile";
            Rect innerRect = rect;
            innerRect.xMin += 3f;
            innerRect.yMin += 2f;
            Widgets.Label( innerRect, label );
            if ( zoneAllowed == zone )
            {
                Widgets.DrawBox( rect, 2 );
            }
            if ( Mouse.IsOver( rect ) )
            {
                if ( zone != null )
                {
                    if ( zone.AllSlotCellsList() != null && zone.AllSlotCellsList().Count > 0 )
                        CameraJumper.TryJump( zone.Cells.First(), zone.Map );
                }
                if ( Input.GetMouseButton( 0 ) &&
                     zoneAllowed != zone )
                {
                    zoneAllowed = zone;
                    SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
                }
            }
            TooltipHandler.TipRegion( rect, label );
            Text.Anchor = TextAnchor.UpperLeft;
        }

        #endregion Methods
    }
}
