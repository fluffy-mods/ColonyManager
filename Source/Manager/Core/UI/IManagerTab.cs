using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace FM
{
    public interface IManagerTab
    {
        void DoWindowContents(Rect canvas);
    }
}
