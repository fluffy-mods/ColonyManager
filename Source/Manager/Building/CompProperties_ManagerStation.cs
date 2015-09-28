using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace FM
{
    class CompProperties_ManagerStation : CompProperties
    {
        public int speed;

        public CompProperties_ManagerStation()
        {
        }

        public CompProperties_ManagerStation(Type compClass) : base(compClass)
        {
        }
    }
}

