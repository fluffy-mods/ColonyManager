// Karel Kroeze
// Trigger.cs
// 2016-12-09

using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static System.String;

namespace FluffyManager
{
    public abstract class Trigger : IExposable
    {
        public Manager manager;

        public Trigger( Manager manager )
        {
            this.manager = manager;
        }

        public abstract bool   State         { get; }
        public virtual  string StatusTooltip { get; } = Empty;

        public virtual void ExposeData()
        {
            Scribe_References.Look( ref manager, "manager" );
        }

        public virtual void DrawProgressBar( Rect progressRect, bool active )
        {
        }

        public abstract void DrawTriggerConfig( ref Vector2 cur, float width, float entryHeight,
                                                string label = null, string tooltip = null,
                                                List<Designation> targets = null, Action onOpenFilterDetails = null,
                                                Func<Designation, string> designationLabelGetter = null );
    }
}