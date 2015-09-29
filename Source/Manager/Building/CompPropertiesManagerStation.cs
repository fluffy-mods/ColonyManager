using System;
using Verse;

namespace FM
{
    class CompPropertiesManagerStation : CompProperties
    {
        public int Speed;

        public CompPropertiesManagerStation()
        {
        }

        public CompPropertiesManagerStation(Type compClass) : base(compClass)
        {
        }
    }
}

