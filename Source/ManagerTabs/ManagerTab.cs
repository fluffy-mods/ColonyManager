// Manager/ManagerTab.cs
//
// Copyright Karel Kroeze, 2015.
//
// Created 2015-11-04 19:23

using UnityEngine;
using Verse;
using Resources = FluffyManager.Resources;

namespace FluffyManager
{
    public abstract class ManagerTab
    {
        #region Fields

        public float DefaultLeftRowSize = 300f;

        #endregion Fields

        #region Enums

        public enum IconAreas
        {
            Left = 0,
            Middle = 1,
            Right = 2
        }

        #endregion Enums

        #region Properties

        public virtual Texture2D Icon
        {
            get { return Resources.IconHammer; }
        }

        public virtual IconAreas IconArea
        {
            get { return IconAreas.Middle; }
        }

        public virtual string Label
        {
            get { return GetType().ToString(); }
        }

        public abstract ManagerJob Selected { get; set; }

        public virtual bool Visible
        {
            get
            {
                return true;
            }
        }

        #endregion Properties

        #region Methods

        public abstract void DoWindowContents( Rect canvas );

        public virtual void PostClose()
        {
        }

        public virtual void PostOpen()
        {
        }

        public virtual void PreClose()
        {
        }

        public virtual void PreOpen()
        {
        }

        public virtual void Tick()
        {
        }

        #endregion Methods
    }
}