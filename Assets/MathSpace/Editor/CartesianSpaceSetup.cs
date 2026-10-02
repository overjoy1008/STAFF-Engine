using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Staff.MathSpace.Editor
{
    [InitializeOnLoad]
    public static class CartesianSpaceSetup
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string Output = "Library/StaffMathSpace";
        const string Glyph = "Assets/MathSpace/Mathematics/Origin/symbol.png";

        static CartesianSpaceSetup()
        {
            EditorApplication.delayCall += FocusOnce;
            EditorSceneManager.sceneOpened += (_, __) => FocusOnce();
        }

        static void FocusOnce()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode ||
                SessionState.GetBool("StaffMathSpace.InfiniteAxesFocused", false)) return;
            if (SceneManager.GetActiveScene().path != ScenePath || !GameObject.Find("Coordinates/X Axis (Infinite)")) return;
            Focus();
            SessionState.SetBool("StaffMathSpace.InfiniteAxesFocused", true);
        }

        public static void BuildBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Build();
            Capture();
        }

        [MenuItem("STAFF/Math Space/Apply Infinite Axes and LaTeX Origin")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Save SampleScene and use Edit mode first.");
            if (GameObject.Find("Coordinates/X Axis (Infinite)")) { Validate(); Focus(); return; }
            var camera = Camera.main;
            var shader = Shader.Find("STAFF/Infinite Coordinate Plane");
            if (!camera || !shader || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Main Camera or infinite-plane shader unavailable.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Glyph);
            if (!texture || !File.Exists(Path.ChangeExtension(Glyph, ".tex")))
                throw new InvalidOperationException("Compile the origin O using Tools/MathTypography/render_latex.py first.");
            Directory.CreateDirectory(Output);
            if (!File.Exists(Output + "/SampleScene.before-infinite-axes.unity"))
                File.Copy(ScenePath, Output + "/SampleScene.before-infinite-axes.unity");
            var importer = (TextureImporter)AssetImporter.GetAtPath(Glyph);
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Glyph);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Organize math space and replace finite axes");
            var old3D = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "3D");
            var oldUI = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "UI");
            RemoveOwned(old3D ? old3D.transform.Find("Cartesian Space (3D)") : null);
            RemoveOwned(oldUI ? oldUI.transform.Find("Coordinate Labels (World Space)") : null);
            if (old3D && old3D.transform.childCount == 0) Undo.DestroyObjectImmediate(old3D);
            if (oldUI && oldUI.transform.childCount == 0) Undo.DestroyObjectImmediate(oldUI);
            var cameras = Group("Cameras");
            var lighting = Group("Lighting");
            var environment = Group("Environment");
            var coordinates = Group("Coordinates");
            Undo.SetTransformParent(camera.transform, cameras.transform, "Categorize Main Camera");
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Directional Light" || root.name == "Global Volume")
                    Undo.SetTransformParent(root.transform, lighting.transform, "Categorize " + root.name);

            EnsureFolder("Assets/MathSpace", "Meshes");
            EnsureFolder("Assets/MathSpace", "Prefabs");
            var mesh = new Mesh { name = "Infinite Plane Screen Coverage" };
            mesh.vertices = new[] { new Vector3(-1,-1,0), new Vector3(1,-1,0), new Vector3(1,1,0), new Vector3(-1,1,0) };
            mesh.triangles = new[] { 0,2,1,0,3,2 };
            // The shader raycasts the analytic plane in every camera, including Scene View.
            // These are screen-coverage vertices, not finite line endpoints.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000000f);
            AssetDatabase.CreateAsset(mesh, "Assets/MathSpace/Meshes/Infinite Plane Coverage.asset");
            for (int kind = 0; kind < 3; kind++)
            {
                var name = kind == 0 ? "Infinite Floor Grid" : kind == 1 ? "X Axis (Infinite)" : "Z Axis (Infinite)";
                var material = new Material(shader) { name = name, renderQueue = 2000 + kind };
                material.SetFloat("_Kind", kind);
                material.SetFloat("_AxisWidth", .12f);
                AssetDatabase.CreateAsset(material, "Assets/MathSpace/Materials/" + name + ".mat");
                var obj = Group(name, kind == 0 ? environment.transform : coordinates.transform);
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.allowOcclusionWhenDynamic = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            // Invisible collider is separate from the analytically unbounded visual plane.
            var ground = Group("Ground Collision", environment.transform);
            var collider = ground.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, -.1f, 0);
            collider.size = new Vector3(2000, .2f, 2000);

            Undo.RecordObject(camera.transform, "Center origin in view");
            camera.transform.LookAt(Vector3.zero);
            camera.farClipPlane = 5000;
            var symbols = Group("Mathematical Symbols", coordinates.transform);
            var origin = GameObject.CreatePrimitive(PrimitiveType.Quad);
            origin.name = "Origin O (LaTeX)";
            Undo.RegisterCreatedObjectUndo(origin, "Create LaTeX origin");
            origin.transform.SetParent(symbols.transform, false);
            origin.transform.position = new Vector3(-.5f, .45f, -.45f);
            origin.transform.rotation = camera.transform.rotation;
            origin.transform.localScale = new Vector3(.55f * texture.width / texture.height, .55f, 1);
            UnityEngine.Object.DestroyImmediate(origin.GetComponent<Collider>());
            var glyphMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "LaTeX White Glyph", renderQueue = 3000 };
            glyphMaterial.SetTexture("_BaseMap", texture);
            glyphMaterial.SetColor("_BaseColor", Color.white);
            glyphMaterial.SetFloat("_Surface", 1);
            glyphMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            glyphMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            glyphMaterial.SetFloat("_ZWrite", 0);
            glyphMaterial.SetFloat("_Cull", (float)CullMode.Off);
            glyphMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glyphMaterial.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(glyphMaterial, "Assets/MathSpace/Materials/LaTeX White Glyph.mat");
            var glyphRenderer = origin.GetComponent<MeshRenderer>();
            glyphRenderer.sharedMaterial = glyphMaterial;
            glyphRenderer.shadowCastingMode = ShadowCastingMode.Off;
            glyphRenderer.receiveShadows = false;
            origin.AddComponent<LatexSymbol>().SetSource("O", texture, Path.ChangeExtension(Glyph, ".tex"));
            PrefabUtility.SaveAsPrefabAsset(origin, "Assets/MathSpace/Prefabs/Origin O.prefab");
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Validate();
            Focus();
        }

        static void RemoveOwned(Transform transform)
        {
            if (!transform) return;
            if (transform.GetComponentsInChildren<Camera>(true).Length > 0 || transform.GetComponentsInChildren<Light>(true).Length > 0)
                throw new InvalidOperationException("Preserve Camera/Light inside the generated group before replacing it.");
            Undo.DestroyObjectImmediate(transform.gameObject);
        }

        static GameObject Group(string name, Transform parent = null)
        {
            if (!parent)
            {
                var existing = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(item => item.name == name);
                if (existing) return existing;
            }
            var obj = new GameObject(name);
            if (parent) obj.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            return obj;
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        [MenuItem("STAFF/Math Space/Focus Origin")]
        public static void Focus()
        {
            if (Application.isBatchMode || !Camera.main) return;
            var view = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
            view.in2DMode = false;
            view.orthographic = false;
            view.sceneViewState.showSkybox = true;
            view.LookAtDirect(Vector3.zero, Camera.main.transform.rotation, 16);
            view.showGrid = false;
            SceneView.RepaintAll();
        }

        [MenuItem("STAFF/Math Space/Validate Geometry and LaTeX Notation")]
        public static void Validate()
        {
            var all = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            if (all.Any(t => t.GetComponent<Canvas>() || t.GetComponent<TextMesh>() ||
                t.GetComponents<MonoBehaviour>().Any(b => b && (b.GetType().Namespace == "TMPro" || b.GetType().FullName == "UnityEngine.UI.Text"))))
                throw new InvalidOperationException("This scene must use LaTeX geometry only, with no UI/text-font substitutes.");
            if (all.Any(t => t.name.Contains("Y Axis") || t.name.Contains("Tick") || t.name.Contains("Value") || t.name.Contains("Arrow")))
                throw new InvalidOperationException("Unexpected axis/tick/number/arrow remains.");
            var symbols = all.Select(t => t.GetComponent<LatexSymbol>()).Where(s => s).ToArray();
            if (symbols.Length != 1 || symbols[0].Formula != "O" || !symbols[0].RenderedTexture || !File.Exists(symbols[0].SourceAsset))
                throw new InvalidOperationException("The only visible notation must be actual LaTeX O.");
            foreach (var name in new[] { "X Axis (Infinite)", "Z Axis (Infinite)" })
            {
                var renderer = GameObject.Find("Coordinates/" + name).GetComponent<MeshRenderer>();
                if (renderer.sharedMaterial.shader.name != "STAFF/Infinite Coordinate Plane" || ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader))
                    throw new InvalidOperationException("Infinite axis shader missing or invalid.");
            }
            if (!Camera.main || !GameObject.Find("Lighting/Directional Light").GetComponent<Light>())
                throw new InvalidOperationException("Original Camera/Light missing.");
            if (all.Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0))
                throw new InvalidOperationException("Missing component scripts.");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/validation-infinite.txt", "PASS\nRole-based hierarchy\nInfinite analytic X/Z axes; no Y, arrows, ticks or numeric labels\nOne real LaTeX O with source and texture\nNo Canvas, UI Text, TextMesh or TMP\nExisting Camera/Light preserved\nNo missing scripts\n");
        }

        [MenuItem("STAFF/Math Space/Capture Preview")]
        public static void Capture()
        {
            var camera = Camera.main;
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var aspect = camera.aspect;
            Texture2D image = null;
            try
            {
                camera.aspect = 1600f / 900;
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes(Output + "/camera-preview-infinite.png", image.EncodeToPNG());
            }
            finally
            {
                camera.aspect = aspect;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                if (image) UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
