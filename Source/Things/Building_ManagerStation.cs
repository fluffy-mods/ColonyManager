// Karel Kroeze
// Building_ManagerStation.cs
// 2016-12-09

using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    // special blinking LED texture/glower logic + automagically doing jobs.
    public class Building_AIManager : Building_ManagerStation
    {
        private readonly Color[] _colors =
        {
            Color.white, Color.green, Color.red, Color.blue, Color.yellow, Color.cyan
        };

        private bool _glowDirty;

        private CompGlower _glower;

        private bool _graphicDirty;

        private Comp_ManagerStation _managerStation;

        private bool _powered;

        private CompPowerTrader _powerTrader;

        private Color _primaryBlinkerColour = Color.black;

        private Color _primaryColor = Color.black;

        private Color _secondaryColor = Color.black;

        private int _secondaryColourIndex;

        public Building_AIManager()
        {
            _powerTrader = PowerComp as CompPowerTrader;
            _glower      = GetComp<CompGlower>();
        }

        public override Color DrawColor => PrimaryColourBlinker;

        public override Color DrawColorTwo => SecondaryColour;

        public CompGlower Glower => _glower ?? ( _glower = GetComp<CompGlower>() );

        public Comp_ManagerStation ManagerStation =>
            _managerStation ?? ( _managerStation = GetComp<Comp_ManagerStation>() );

        public bool Powered
        {
            get => _powered;
            set
            {
                _powered = value;
                Glower.SetLit( value );
                PrimaryColourBlinker = value ? PrimaryColour : Color.black;
                SecondaryColour      = value ? _colors[_secondaryColourIndex] : Color.black;
            }
        }

        public CompPowerTrader PowerTrader => _powerTrader ?? ( _powerTrader = PowerComp as CompPowerTrader );

        public Color PrimaryColour
        {
            get => _primaryColor;
            set
            {
                var newColour = new ColorInt( (int) ( value.r * 255 ), (int) ( value.g * 255 ),
                                              (int) ( value.b * 255 ), 0 );
                Glower.Props.glowColor = newColour;
                _primaryColor          = value;
                _glowDirty             = true;
            }
        }

        public Color PrimaryColourBlinker
        {
            get => _primaryBlinkerColour;
            set
            {
                _primaryBlinkerColour = value;
                _graphicDirty         = true;
            }
        }

        public Color SecondaryColour
        {
            get => _secondaryColor;
            set
            {
                _secondaryColor = value;
                _graphicDirty   = true;
            }
        }

        public int SecondaryColourIndex
        {
            get => _secondaryColourIndex;
            set
            {
                _secondaryColourIndex = value;
                SecondaryColour       = _colors[_secondaryColourIndex];
            }
        }

        public override void Tick()
        {
            base.Tick();

            if ( Powered != PowerTrader.PowerOn ) Powered = PowerTrader.PowerOn;

            if ( Powered )
            {
                var tick = Find.TickManager.TicksGame;

                // turn on glower
                Glower.SetLit();

                // random blinking on secondary
                if ( tick % 30 == Rand.RangeInclusive( 0, 25 ) )
                    SecondaryColourIndex = ( SecondaryColourIndex + 1 ) % _colors.Length;

                // primary colour
                if ( tick % ManagerStation.Props.speed == 0 )
                    PrimaryColour = Manager.For( Map ).TryDoWork() ? Color.green : Color.red;

                // blinking on primary
                if ( tick % 30 == 0 ) PrimaryColourBlinker  = PrimaryColour;
                if ( tick % 30 == 25 ) PrimaryColourBlinker = Color.black;
            }

            // apply changes
            if ( _graphicDirty )
            {
                // update LED colours
                Notify_ColorChanged();
                _graphicDirty = false;
            }

            if ( _glowDirty )
            {
                // Update glow grid
                Map.glowGrid.MarkGlowGridDirty( Position );

                // the following two should not be necesarry, but for some reason do seem to be.
                Map.mapDrawer.MapMeshDirty( Position, MapMeshFlag.GroundGlow );
                Map.mapDrawer.MapMeshDirty( Position, MapMeshFlag.Things );

                _glowDirty = false;
            }
        }
    }

    public class Building_ManagerStation : Building_WorkTable
    {
        // just to give different versions a common interface.
    }
}