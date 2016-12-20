// Karel Kroeze
// ManagerTab.cs
// 2016-12-09

using RimWorld;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public abstract class ManagerTab
    {
        #region Fields

        public float DefaultLeftRowSize = 300f;

        public Manager manager;

        #endregion Fields

        #region Constructors

        public ManagerTab( Manager manager )
        {
            this.manager = manager;
        }

        #endregion Constructors



        #region Enums

        public enum IconAreas
        {
            Left = 0,
            Middle = 1,
            Right = 2
        }

        #endregion Enums

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
            get { return true; }
        }

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
    }
}
