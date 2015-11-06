using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace FM
{
    class ManagerTab_Hunting : ManagerTab
    {
        private static Texture2D _icon = ContentFinder<Texture2D>.Get("UI/Icons/Hunting");

        public override Texture2D Icon
        {
            get
            {
                return _icon;
            }
        }

        public override string Label
        {
            get
            {
                return "FMH.Hunting".Translate();
            }
        }
    }
}
