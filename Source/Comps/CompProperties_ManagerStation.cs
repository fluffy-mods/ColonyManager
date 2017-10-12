// Karel Kroeze
// CompProperties_ManagerStation.cs
// 2016-12-09

using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class CompProperties_ManagerStation : CompProperties
    {
        #region Fields

        private int speed = 250;
        public int Speed => speed;

        #endregion Fields

        #region Constructors

        public CompProperties_ManagerStation()
        {
        }

        public CompProperties_ManagerStation( Type compClass ) : base( compClass )
        {
        }

        #endregion Constructors
    }
}
