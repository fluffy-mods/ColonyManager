// Karel Kroeze
// CompProperties_ManagerStation.cs
// 2016-12-09

using System;
using Verse;

namespace FluffyManager
{
    public class CompProperties_ManagerStation : CompProperties
    {
        public CompProperties_ManagerStation()
        {
        }

        public CompProperties_ManagerStation( Type compClass ) : base( compClass )
        {
        }

        public int speed = 250;
    }
}