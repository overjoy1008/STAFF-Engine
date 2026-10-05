using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Staff.MathSpace.Editor;

namespace Staff.Characters.Editor
{
    public static class HumanoidSetup
    {
        const string Source = "Assets/ThirdParty/UnityStarterAssets/";
        public static void ReplaceBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var player = UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();
            if (!player || player.Animator.isHuman) throw new Exception("Expected the previous robot player; refusing unrelated replacement.");
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Source + "Models/Armature.fbx").OfType<Avatar>().First();
            if (!avatar.isHuman || !avatar.isValid) throw new Exception("Starter Assets humanoid Avatar invalid.");
            Directory.CreateDirectory("Assets/Characters/Animation/Humanoid"); AssetDatabase.Refresh();
            AnimationClip Clip(string name)
            {
                var original = AssetDatabase.LoadAllAssetsAtPath(Source + "Animations/" + name + ".anim.fbx")
                    .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
                var copy = UnityEngine.Object.Instantiate(original); copy.name = name;
                // Starter Assets audio callbacks belong to its own controller, not ours.
                AnimationUtility.SetAnimationEvents(copy, Array.Empty<AnimationEvent>());
                AssetDatabase.CreateAsset(copy, "Assets/Characters/Animation/Humanoid/" + name + ".anim");
                return copy;
            }
            var idle = Clip("Stand--Idle"); var walk = Clip("Locomotion--Walk_N");
            var run = Clip("Locomotion--Run_N"); var jump = Clip("Jump--Jump"); var fall = Clip("Jump--InAir");
            string controllerPath = "Assets/Characters/Animation/HumanoidAdventure.controller";
            AssetDatabase.CopyAsset("Assets/Characters/Animation/RobotAdventure.controller", controllerPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            foreach (var entry in controller.layers[0].stateMachine.states)
            {
                var state = entry.state;
                if (state.motion is BlendTree tree)
                {
                    var children = tree.children;
                    children[0].motion = idle; children[1].motion = walk;
                    children[2].motion = run; children[3].motion = run;
                    children[2].threshold = 8; children[2].timeScale = 1.55f;
                    children[3].threshold = 18; children[3].timeScale = 1.9f;
                    tree.children = children; EditorUtility.SetDirty(tree);
                }
                else if (state.name == "Jump") state.motion = jump;
                else if (state.name == "Fall") { state.motion = fall; state.speed = 1; state.cycleOffset = 0; }
                EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(controller);
            var visual = player.Visual;
            var old = player.Animator.gameObject;
            if (old.GetComponentsInChildren<Camera>(true).Length > 0 || old.GetComponentsInChildren<Light>(true).Length > 0)
                throw new Exception("Unexpected camera/light in old model; replacement cancelled.");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Models/Armature.fbx"), visual);
            model.name = "Starter Assets Silver Humanoid";
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.avatar = avatar; animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0);
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = 1.8f / bounds.size.y;
            model.transform.localScale *= scale;
            model.transform.localPosition = new Vector3(0, -(bounds.min.y - visual.position.y) * scale, 0);
            var silver = new Material(Shader.Find("Standard")) { name = "Starter Armature Silver" };
            silver.SetColor("_Color", new Color(.72f,.75f,.79f));
            silver.SetFloat("_Metallic", .35f); silver.SetFloat("_Smoothness", .45f);
            AssetDatabase.CreateAsset(silver,"Assets/Characters/Materials/Starter Armature Silver.mat");
            foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => silver).ToArray();
            player.Configure(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Characters/Input/Adventure.inputactions"), animator, visual, Camera.main.GetComponent<AdventureCamera>());
            UnityEngine.Object.DestroyImmediate(old);
            player.name = "Player Humanoid";
            PrefabUtility.SaveAsPrefabAsset(player.gameObject,"Assets/Characters/Prefabs/Player Humanoid.prefab");
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            CartesianSpaceSetup.Validate(); CartesianSpaceSetup.Capture();
            File.Copy("Library/StaffMathSpace/camera-preview-infinite.png","Library/StaffMathSpace/humanoid-preview.png",true);
            Debug.Log("HUMANOID REPLACEMENT PASS: valid Human Avatar; silver armature normalized to 1.8m; controller and camera preserved.");
        }
    }
}
