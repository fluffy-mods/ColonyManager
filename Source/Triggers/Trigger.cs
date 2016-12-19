// // Karel Kroeze
// // Trigger.cs
// // 2016-12-09

using RimWorld;
using UnityEngine;
using Verse;
using static System.String;

namespace FluffyManager
{
    public abstract class Trigger : IExposable
    {
        public Trigger( Manager manager ) { this.manager = manager; }

        #region Properties

        public abstract bool State { get; }
        public virtual string StatusTooltip { get; } = Empty;
        public Manager manager;

        #endregion Properties

        #region Methods

        public virtual void DrawProgressBar( Rect progressRect, bool active ) { }

        public abstract void DrawTriggerConfig( ref Vector2 cur, float width, float entryHeight, bool alt = false,
                                                string label = null, string tooltip = null );

        public abstract void ExposeData();

        #endregion Methods
    }
}
