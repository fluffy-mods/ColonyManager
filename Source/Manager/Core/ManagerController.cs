using UnityEngine;
using Verse;

namespace FM
{
    internal class ManagerController : MonoBehaviour
    {
        public readonly string GameObjectName = "Fluffy Manager";

        public void Start()
        {
            Log.Message( "Manager Controller loaded." );
            enabled = true;
        }

        public void OnLevelWasLoaded() {}
    }

    public class Bootstrap : ITab
    {
        public static GameObject GameObject;

        public Bootstrap()
        {
            if ( GameObject != null )
            {
                return;
            }
            GameObject = new GameObject( "Manager_Controller" );
            GameObject.AddComponent< ManagerController >();
            Object.DontDestroyOnLoad( GameObject );
        }

        protected override void FillTab() {}
    }
}