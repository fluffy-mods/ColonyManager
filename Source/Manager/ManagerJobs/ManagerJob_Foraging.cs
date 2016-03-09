using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    internal class ManagerJob_Foraging : ManagerJob
    {
        #region Properties

        public override bool Completed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Label
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ManagerTab Tab
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string[] Targets
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override WorkTypeDef WorkTypeDef
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion Properties

        #region Methods

        public override void CleanUp()
        {
            throw new NotImplementedException();
        }

        public override void DrawListEntry( Rect rect, bool overview = true, bool active = true )
        {
            throw new NotImplementedException();
        }

        public override void DrawOverviewDetails( Rect rect )
        {
            throw new NotImplementedException();
        }

        public override bool TryDoJob()
        {
            throw new NotImplementedException();
        }

        #endregion Methods
    }
}