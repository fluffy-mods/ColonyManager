using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace FM
{
    class Manager_Controller : MonoBehaviour
    {
        public readonly string GameObjectName = "Fluffy Manager";

        public void Start()
        {
            Log.Message("Manager Controller loaded.");
            base.enabled = true;
        }

        public void OnLevelWasLoaded()
        {
        }
    }

    public class Bootstrap : ITab
    {
        public static GameObject gameObject;

        public Bootstrap()
        {
            if (Bootstrap.gameObject == null)
            {
                Bootstrap.gameObject = new GameObject("Manager_Controller");
                Bootstrap.gameObject.AddComponent<Manager_Controller>();
                UnityEngine.Object.DontDestroyOnLoad(Bootstrap.gameObject);
            }
        }

        protected override void FillTab()
        {
        }
    }
}
