using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace FM
{
    public abstract class Trigger
    {
        public abstract bool state
        {
            get;
        }

        public abstract void DrawThresholdConfig(ref Listing_Standard listing);
    }
}
