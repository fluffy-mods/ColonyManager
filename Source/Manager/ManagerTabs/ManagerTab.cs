// Manager/ManagerTab.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:23

using UnityEngine;
using Verse;

namespace FM
{
    public abstract class ManagerTab
    {
        public enum IconAreas
        {
            Left = 0,
            Middle = 1,
            Right = 2
        }

        public static Texture2D DefaultIcon = ContentFinder< Texture2D >.Get( "UI/Icons/Hammer" );
        public float DefaultLeftRowSize = 300f;

        public virtual Texture2D Icon
        {
            get { return DefaultIcon; }
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

        public abstract void DoWindowContents( Rect canvas );

        public virtual void PostClose() {}

        public virtual void PostOpen() {}

        public virtual void PreClose() {}

        public virtual void PreOpen() {}
    }
}