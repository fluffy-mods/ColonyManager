// RadialProgressBar.cs
// Copyright Karel Kroeze, 2019-2019

/**
 * Radial progress bar renderer - doesn't actually work.
 */

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FluffyManager
{

    public class RadialRenderer : MonoBehaviour
    {
        private        Mesh   mesh;

        public RadialRenderer()
        {
            Logger.Debug( "RadialRenderer()" );
        }

        public void Start()
        { 
            Logger.Debug( "RadialRenderer.Start()" );
        }

        public void OnPostRender()
        {
            Logger.Debug( "RadialRenderer.OnPostRender()" );
            SolidColorMaterials.SimpleSolidColorMaterial( Color.white ).SetPass( 0 );
            Graphics.DrawMeshNow( mesh, Vector3.zero, Quaternion.AngleAxis( 90, Vector3.up ) );
        }

        public void Render( Mesh mesh, RenderTexture renderTexture )
        {
            Logger.Debug( $"RadialRenderer.Render({(isActiveAndEnabled ? "active" : "inactive")})" );
            RadialProgressBar.Camera.targetTexture    = renderTexture;
            this.mesh                                 = mesh;
            RadialProgressBar.Camera.orthographicSize = mesh.bounds.size.y / 2f;
            RadialProgressBar.Camera.Render();
            this.mesh                              = null;
            RadialProgressBar.Camera.targetTexture = null;
        }
    }

    public static class RadialProgressBar
    {
        private static readonly Dictionary<RadialBarSettings, Mesh> _meshCache =
            new Dictionary<RadialBarSettings, Mesh>();

        private static Camera _camera;

        private static readonly Dictionary<RadialBarSettings, RenderTexture> _renderTextureCache =
            new Dictionary<RadialBarSettings, RenderTexture>();

        public static Camera Camera
        {
            get
            {
                if ( _camera == null )
                {
                    var gameObject = new GameObject( "RadialBarCamera", typeof( Camera ) );
                    gameObject.SetActive( true );
                    gameObject.AddComponent<RadialRenderer>();
                    Object.DontDestroyOnLoad( gameObject );

                    var renderer = gameObject.GetComponent<RadialRenderer>();
                    renderer.enabled = true;

                    var camera = gameObject.GetComponent<Camera>();
                    camera.enabled = true;
                    camera.transform.position  = new Vector3( 0f, 0f, 0f );
                    camera.transform.rotation  = Quaternion.Euler( 90f, 0f, 0f );
                    camera.orthographic        = true;
                    camera.cullingMask         = 0;
                    camera.aspect              = 1f;
                    camera.backgroundColor     = Color.cyan;
                    camera.clearFlags          = CameraClearFlags.Nothing;
                    camera.useOcclusionCulling = false;
                    camera.renderingPath       = RenderingPath.Forward;
                    camera.depth               = 1f;
                    camera.nearClipPlane       = 0f;
                    camera.farClipPlane        = 100f;
                    _camera                    = camera;
                }

                return _camera;
            }
        }

        public static RadialRenderer RadialRenderer => Camera.GetComponent<RadialRenderer>();

        public static Texture Get( float radius, float width, float progress,
                                   float radiansPerSection = 2 * Mathf.PI / 100 )
        {
            var settings = new RadialBarSettings( radius, width, progress, radiansPerSection );
            if ( _renderTextureCache.TryGetValue( settings, out var texture ) )
                return texture;

            texture = new RenderTexture( settings.Size, settings.Size, 32 );
            _renderTextureCache.Add( settings, texture );

            RadialRenderer.Render( GetMesh( settings ), texture );
            return texture;
        }

        private static Mesh GetMesh( RadialBarSettings settings )
        {
            if ( _meshCache.TryGetValue( settings, out var mesh ) )
                return mesh;

            mesh = CreateMesh( settings );
            _meshCache.Add( settings, mesh );
            Logger.Debug( $"Mesh created: {mesh.name}" );
            return mesh;
        }

        private static Mesh CreateMesh( RadialBarSettings settings )
        {
            // radii
            var innerRadius = settings.Radius - settings.Width;

            // get base points along the circle;
            var nPoints =
                (int) ( settings.Progress * Mathf.PI * 2 / settings.RadiansPerSection );
            var points                                    = new Vector2[nPoints + 1];
            for ( var i = 0; i < nPoints; i++ ) points[i] = GetPoint( i * settings.RadiansPerSection );

            // final point
            points[nPoints] = GetPoint( settings.Progress * Mathf.PI * 2 );

            // vertices
            var nVertices = ( nPoints + 1 ) * 2;
            var vertices  = new Vector3[nVertices];
            var colors    = new Color[nVertices];
            var normals   = new Vector3[nVertices];
            var triangles = new int[nPoints * 6];

            for ( var i = 0; i < nPoints + 1; i++ )
            {
                var j = i * 2;
                vertices[j]     = new Vector3( points[i].x * innerRadius, 0f, points[i].y     * innerRadius );
                vertices[j + 1] = new Vector3( points[i].x * settings.Radius, 0f, points[i].y * settings.Radius );
                colors[j]       = Color.white;
                colors[j + 1]   = Color.white;
                normals[j]      = Vector3.up;
                normals[j + 1]  = Vector3.up;

                if ( i < nPoints )
                {
                    var k = i * 6;
                    triangles[k]     = j;
                    triangles[k + 1] = j + 1;
                    triangles[k + 2] = j + 2;
                    triangles[k + 3] = j + 2;
                    triangles[k + 4] = j + 1;
                    triangles[k + 5] = j + 3;
                }
            }

            return new Mesh
            {
                name = $"radialMesh({settings.Radius}, {settings.Width}, {settings.Progress}, {settings.RadiansPerSection})",
                vertices  = vertices,
                triangles = triangles,
                colors    = colors,
                normals   = normals
            };
        }

        public static Vector2 GetPoint( float radians )
        {
            return new Vector2( Mathf.Cos( radians ), Mathf.Sin( radians ) );
        }

        private struct RadialBarSettings : IEqualityComparer<RadialBarSettings>
        {
            public RadialBarSettings( float radius, float width, float progress, float radiansPerSection )
            {
                Radius            = radius;
                Width             = width;
                Progress          = Mathf.Clamp01( (int) ( progress * 100 ) / 100f );
                RadiansPerSection = radiansPerSection;
            }

            public float Radius { get; }

            public int Size => Mathf.CeilToInt( Radius * 2 );

            public float Width { get; }

            public float Progress { get; }

            public float RadiansPerSection { get; }

            public bool Equals( RadialBarSettings x, RadialBarSettings y )
            {
                return x == y;
            }

            public int GetHashCode( RadialBarSettings obj )
            {
                // https://stackoverflow.com/a/263416/2604271
                unchecked // Overflow is fine, just wrap
                {
                    var hash = 17;
                    // Suitable nullity checks etc, of course :)
                    hash = hash * 23 + Radius.GetHashCode();
                    hash = hash * 23 + Width.GetHashCode();
                    hash = hash * 23 + Progress.GetHashCode();
                    hash = hash * 23 + RadiansPerSection.GetHashCode();
                    return hash;
                }
            }

            public static bool operator ==( RadialBarSettings self, RadialBarSettings other )
            {
                return self.Radius            - other.Radius            < float.Epsilon &&
                       self.Width             - other.Width             < float.Epsilon &&
                       self.Progress          - other.Progress          < float.Epsilon &&
                       self.RadiansPerSection - other.RadiansPerSection < float.Epsilon;
            }

            public static bool operator !=( RadialBarSettings self, RadialBarSettings other )
            {
                return !( self == other );
            }
        }
    }
}