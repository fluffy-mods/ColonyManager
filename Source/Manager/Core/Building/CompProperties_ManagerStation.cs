using System;
using Verse;

namespace FM
{
    internal class CompProperties_ManagerStation : CompProperties
    {
        public int Speed;

        public CompProperties_ManagerStation() {}

        public CompProperties_ManagerStation( Type compClass ) : base( compClass ) {}
    }
}