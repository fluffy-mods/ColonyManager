// CompProperties_ManagerStation.cs
// Copyright Karel Kroeze, 2017-2020

using System;
using Verse;

namespace FluffyManager
{
    public class CompProperties_ManagerStation : CompProperties
    {
        public int speed = 250;

        public CompProperties_ManagerStation()
        {
        }

        public CompProperties_ManagerStation( Type compClass ) : base( compClass )
        {
        }
    }
}