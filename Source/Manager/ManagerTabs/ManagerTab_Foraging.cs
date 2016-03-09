using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FluffyManager
{
    internal class ManagerTab_Foraging : ManagerTab
    {
        #region Fields

        private ManagerJob_Foraging _selected;

        #endregion Fields

        #region Properties

        public override ManagerJob Selected
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion Properties

        #region Methods

        public override void DoWindowContents( Rect canvas )
        {
            throw new NotImplementedException();
        }

        #endregion Methods
    }
}