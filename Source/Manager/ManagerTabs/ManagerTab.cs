using UnityEngine;
using Verse;

namespace FM
{
    public abstract class ManagerTab : IManagerTab
    {
        public static Texture2D _defaultIcon = ContentFinder<Texture2D>.Get("UI/Icons/Hammer");

        public abstract string Label
        {
            get;
        }

        public virtual Texture2D Icon
        {
            get
            {
                return _defaultIcon;
            }
        }

        public virtual ManagerJob Selected
        {
            get; set;
        }

        public virtual void PostOpen()
        {

        }

        public virtual void PreOpen()
        {

        }

        public virtual void PostClose()
        {

        }

        public virtual void PreClose()
        {

        }

        public virtual void DoWindowContents(Rect canvas)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.grey;
            Widgets.Label(canvas, "<not implemented>");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
    }
}
