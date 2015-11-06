using System;
using UnityEngine;
using Verse;

namespace FM
{
    public abstract class Trigger : IExposable
    {
        public abstract bool State { get; }
        public abstract string StatusTooltip { get; }
        public abstract void ExposeData();
        public abstract void DrawThresholdConfig( ref Listing_Standard listing );
        public virtual void DrawProgressBar( Rect progressRect, bool active ) {
            return;
        }
    }
}