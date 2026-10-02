using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Staff.MathSpace.Editor
{
    [CustomEditor(typeof(MonochromeMode))]
    public sealed class MonochromeModeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Toggle Black / White")) Change((MonochromeMode)target);
        }

        static void Change(MonochromeMode mode)
        {
            Undo.RecordObjects(new UnityEngine.Object[] { mode, Camera.main }, "Toggle monochrome mode");
            mode.Toggle();
            EditorUtility.SetDirty(mode);
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(mode.gameObject.scene);
            SceneView.RepaintAll();
        }

        [MenuItem("STAFF/Math Space/Toggle Black-White Inversion")]
        static void Toggle()
        {
            var mode = UnityEngine.Object.FindFirstObjectByType<MonochromeMode>();
            if (mode) Change(mode);
        }

        public static void InstallBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var shader = Shader.Find("STAFF/Solid Skybox");
            if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Invalid skybox shader");
            const string whitePath = "Assets/MathSpace/Materials/Skybox Pure White.mat";
            var white = AssetDatabase.LoadAssetAtPath<Material>(whitePath);
            if (!white)
            {
                white = new Material(shader) { name = "Skybox Pure White" };
                white.SetColor("_Color", Color.white);
                AssetDatabase.CreateAsset(white, whitePath);
            }
            var existing = UnityEngine.Object.FindFirstObjectByType<MonochromeMode>();
            var host = existing ? existing.gameObject : new GameObject("Monochrome Mode");
            host.transform.SetParent(GameObject.Find("Environment").transform, false);
            var mode = existing ? existing : host.AddComponent<MonochromeMode>();
            mode.Configure(Camera.main, AssetDatabase.LoadAssetAtPath<Material>("Assets/MathSpace/Materials/Skybox Pure Black.mat"), white,
                new[] { GameObject.Find("Environment/Infinite Floor Grid").GetComponent<Renderer>(),
                    GameObject.Find("Coordinates/X Axis (Infinite)").GetComponent<Renderer>(),
                    GameObject.Find("Coordinates/Z Axis (Infinite)").GetComponent<Renderer>() },
                new[] { UnityEngine.Object.FindFirstObjectByType<LatexSymbol>().GetComponent<Renderer>() });
            Directory.CreateDirectory("Library/StaffMathSpace");
            mode.SetInverted(false);
            CartesianSpaceSetup.Capture();
            File.Copy("Library/StaffMathSpace/camera-preview-infinite.png", "Library/StaffMathSpace/normal.png", true);
            mode.Toggle();
            if (!mode.Inverted || RenderSettings.skybox != white) throw new Exception("Inversion failed");
            CartesianSpaceSetup.Capture();
            File.Copy("Library/StaffMathSpace/camera-preview-infinite.png", "Library/StaffMathSpace/inverted.png", true);
            mode.Toggle();
            if (mode.Inverted || RenderSettings.skybox == white) throw new Exception("Restore failed");
            mode.SetInverted(true);
            CartesianSpaceSetup.Validate();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(host.scene);
            EditorSceneManager.SaveScene(host.scene);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            mode = UnityEngine.Object.FindFirstObjectByType<MonochromeMode>();
            if (!mode || !mode.Inverted || RenderSettings.skybox != white) throw new Exception("Reload failed");
            CartesianSpaceSetup.Validate();
            Debug.Log("MONOCHROME PASS: normal, inverted, restore, saved scene reload and geometry validation");
        }
    }
}
