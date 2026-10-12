using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Staff.MathSpace.Editor;

namespace Staff.Characters.Editor
{
    public static class AdventureSetup
    {
        const string ModelPath = "Assets/ThirdParty/Quaternius/RobotExpressive/RobotExpressive.fbx";
        const string Root = "Assets/Characters";
        public static void BuildBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            if (UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>()) throw new Exception("Player already exists; refusing duplicate setup.");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Animation");
            Directory.CreateDirectory(Root + "/Input");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importBlendShapes = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            var definitions = importer.defaultClipAnimations;
            foreach (var clip in definitions)
            {
                clip.loopTime = clip.name.Contains("Idle") || clip.name.Contains("Walking") || clip.name.Contains("Running") || clip.name.Contains("Dance");
                clip.loopPose = clip.loopTime;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = definitions;
            importer.SaveAndReimport();
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            Debug.Log("ROBOT CLIPS: " + string.Join(", ", clips.Select(c => c.name + "=" + c.length)));
            AnimationClip Clip(string name) => clips.First(c => c.name == name || c.name.EndsWith("|" + name));
            if (clips.Length < 14) throw new Exception("Expected all 14 source animation tracks.");
            var idle = Clip("Idle"); var walking = Clip("Walking"); var running = Clip("Running"); var jumping = Clip("Jump");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Animation/RobotAdventure.controller");
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            parameters.First(p => p.name == "Grounded").defaultBool = true;
            controller.parameters = parameters;
            var machine = controller.layers[0].stateMachine;
            var locomotion = machine.AddState("Locomotion");
            var tree = new BlendTree { name = "Idle Walk Run Sprint", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(idle, 0); tree.AddChild(walking, 1.8f); tree.AddChild(running, 4.2f); tree.AddChild(running, 6.5f);
            var children = tree.children; children[3].timeScale = 1.45f; tree.children = children;
            locomotion.motion = tree; machine.defaultState = locomotion;
            var jumpState = machine.AddState("Jump"); jumpState.motion = jumping;
            var fall = machine.AddState("Fall"); fall.motion = jumping; fall.speed = 0; fall.cycleOffset = .55f;
            var toJump = Transition(locomotion, jumpState, .08f);
            toJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            toJump.AddCondition(AnimatorConditionMode.Greater, 0, "VerticalSpeed");
            var toFall = Transition(locomotion, fall, .12f);
            toFall.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            toFall.AddCondition(AnimatorConditionMode.Less, .01f, "VerticalSpeed");
            Transition(jumpState, fall, .12f).AddCondition(AnimatorConditionMode.Less, 0, "VerticalSpeed");
            Transition(fall, locomotion, .12f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            Transition(jumpState, locomotion, .1f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");

            var group = new GameObject("Characters");
            var player = new GameObject("Player Robot"); player.transform.SetParent(group.transform, false);
            player.transform.position = new Vector3(0, .03f, -3);
            var visual = new GameObject("Visual"); visual.transform.SetParent(player.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), visual.transform);
            model.name = "Quaternius Robot (Animated)";
            var animator = model.GetComponent<Animator>();
            if (!animator) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().First();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            idle.SampleAnimation(model, 0);
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = 1.8f / bounds.size.y;
            model.transform.localScale = Vector3.one * scale;
            model.transform.localPosition = new Vector3(-bounds.center.x * scale,
                -(bounds.min.y - player.transform.position.y) * scale, -(bounds.center.z - player.transform.position.z) * scale);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    string name = source.name;
                    string path = Root + "/Materials/Robot " + name + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!material)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Robot " + name };
                        material.SetColor("_BaseColor", name.Contains("Main") ? new Color(.96f,.53f,.12f) : name.Contains("Black") ? new Color(.10f,.11f,.13f) : new Color(.40f,.44f,.48f));
                        material.SetFloat("_Metallic", .15f);
                        material.SetFloat("_Smoothness", .3f);
                        AssetDatabase.CreateAsset(material, path);
                    }
                    return material;
                }).ToArray();
            }
            var capsule = player.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = .28f; capsule.center = Vector3.up * .9f;
            capsule.stepOffset = .3f; capsule.slopeLimit = 50; capsule.skinWidth = .03f; capsule.minMoveDistance = 0;
            var camera = Camera.main;
            var follow = camera.gameObject.AddComponent<AdventureCamera>();
            follow.Configure(player.transform, visual.transform);
            var input = CreateInput();
            player.AddComponent<AdventurePlayer>().Configure(input, animator, visual.transform, follow);
            PrefabUtility.SaveAsPrefabAsset(player, Root + "/Prefabs/Player Robot.prefab");
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveScene(player.scene);
            CartesianSpaceSetup.Validate();
            CartesianSpaceSetup.Capture();
            File.Copy("Library/StaffMathSpace/camera-preview-infinite.png", "Library/StaffMathSpace/player-preview.png", true);
            File.WriteAllText("Library/StaffMathSpace/player-import.txt", "14 CC0 source clips imported\n" + string.Join("\n", clips.Select(c => c.name + " " + c.length)) + "\nHeight scale: " + scale);
            Debug.Log("PLAYER BUILD PASS");
        }
        public static void RefreshAvatarAndVerifyBatch()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var player = UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();
            if (player.Animator.isHuman) throw new Exception("Active player is the Starter Assets humanoid; do not assign the legacy robot Avatar.");
            player.Animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().First();
            PrefabUtility.SaveAsPrefabAsset(player.gameObject, Root + "/Prefabs/Player Robot.prefab");
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            AssetDatabase.SaveAssets();
            AdventureVerification.RunBatch();
        }
        static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, float duration)
        {
            var transition = from.AddTransition(to); transition.hasExitTime = false;
            transition.hasFixedDuration = true; transition.duration = duration; return transition;
        }
        static InputActionAsset CreateInput()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Adventure"); asset.AddActionMap(map);
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            map.AddAction("LookMouse", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            map.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space").AddBinding("<Gamepad>/buttonSouth");
            map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift").AddBinding("<Gamepad>/leftStickPress");
            map.FindAction("Sprint").AddBinding("<Mouse>/rightButton");
            map.AddAction("Walk", InputActionType.Button, "<Keyboard>/leftCtrl");
            map.AddAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y", expectedControlLayout: "Axis");
            map.AddAction("Recenter", InputActionType.Button, "<Mouse>/middleButton").AddBinding("<Gamepad>/rightStickPress");
            map.AddAction("ReleaseCursor", InputActionType.Button, "<Keyboard>/escape");
            map.AddAction("CaptureCursor", InputActionType.Button, "<Mouse>/leftButton");
            string path = Root + "/Input/Adventure.inputactions";
            File.WriteAllText(path, asset.ToJson());
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
    }
}
