using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Staff.Characters.Editor
{
    public static class CharacterRosterSetup
    {
        const string Source = "Assets/ThirdParty/StaffCharacters/";
        const string Prefabs = "Assets/Characters/Prefabs/Imported/";
        [Serializable] public class Entry { public string slug, name, label, game; }
        [Serializable] public class Manifest { public Entry[] characters; }
        [Serializable] public class MaterialInfo { public string name, texture; public float[] color; public bool doubleSided; }
        [Serializable] public class Metadata { public string[] humanBones; public MaterialInfo[] materials; public float targetHeight; }
        // Use the installed editor's own Enforce T-Pose implementation. Asset bind
        // poses stay intact; the Avatar reference pose is calibrated like Rig > Configure.
        static SkeletonBone[] CalibratedTPose(GameObject source, string[] mappedNames)
        {
            var instance=UnityEngine.Object.Instantiate(source);instance.name=source.name;
            try
            {
                var tool=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AvatarSetupTool",true);
                var wrapper=tool.GetNestedType("BoneWrapper",BindingFlags.Public|BindingFlags.NonPublic);
                var bones=Array.CreateInstance(wrapper,HumanTrait.BoneCount);
                var transforms=instance.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t.name);
                for(int i=0;i<HumanTrait.BoneCount;i++)
                {
                    string name=HumanTrait.BoneName[i];Transform bone=null;
                    if(mappedNames.Contains(name))transforms.TryGetValue(name,out bone);
                    bones.SetValue(Activator.CreateInstance(wrapper,new object[]{name,bone}),i);
                }
                tool.GetMethod("MakePoseValid",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{bones});
                return instance.GetComponentsInChildren<Transform>(true).Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
            }
            finally {UnityEngine.Object.DestroyImmediate(instance);}
        }
        public static void RefreshCatalogBatch()
        {
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Source+"manifest.json"));
            Directory.CreateDirectory("Assets/Characters/Resources");AssetDatabase.Refresh();
            const string path="Assets/Characters/Resources/StaffCharacterRoster.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<CharacterRosterCatalog>(path);
            if(!catalog){catalog=ScriptableObject.CreateInstance<CharacterRosterCatalog>();AssetDatabase.CreateAsset(catalog,path);}
            catalog.characters=manifest.characters.Select(e=>AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+e.slug+".prefab")).ToArray();
            catalog.easterEggCharacters=manifest.characters.Select(e=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Prefabs/EasterEgg/"+e.slug+".prefab")).ToArray();
            if(catalog.characters.Any(p=>!p)||catalog.easterEggCharacters.Any(p=>!p))throw new Exception("Incomplete character catalog");
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Characters/Build Imported Character Roster")]
        public static void BuildBatch()
        {
            AssetDatabase.Refresh();
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Source + "manifest.json"));
            Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Characters/Animation/HumanoidAdventure.controller");
            if (!controller) throw new Exception("Existing humanoid locomotion controller not found.");
            var shader = Shader.Find("STAFF/Character Toon");
            if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Character toon shader failed.");
            var prefabs = new List<GameObject>();
            foreach (var entry in manifest.characters)
            {
                string folder = Source + entry.slug + "/", path = folder + entry.slug + ".fbx";
                var meta = JsonUtility.FromJson<Metadata>(File.ReadAllText(folder + "character.json"));
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation = false; importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.isReadable = true; importer.optimizeGameObjects = false;
                importer.animationType = ModelImporterAnimationType.Generic; importer.SaveAndReimport();
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var skeleton = CalibratedTPose(source, meta.humanBones);
                var human = meta.humanBones.Select(n => new HumanBone {boneName=n,humanName=n,limit=new HumanLimit {useDefaultValues=true}}).ToArray();
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.humanDescription = new HumanDescription {human=human,skeleton=skeleton,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0,hasTranslationDoF=false};
                importer.SaveAndReimport();
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                if (!avatar || !avatar.isValid || !avatar.isHuman) throw new Exception("Invalid Humanoid: " + entry.slug);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                instance.name = entry.slug;
                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.avatar=avatar;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var materials = new Dictionary<string,Material>();
                foreach (var info in meta.materials)
                {
                    string matPath=folder+info.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (!mat) {mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}mat.shader=shader;
                    mat.SetColor("_BaseColor",new Color(info.color[0],info.color[1],info.color[2],info.color[3]));mat.SetFloat("_Cull",info.doubleSided?0:2);
                    if (!string.IsNullOrEmpty(info.texture))
                    {
                        var texImporter=(TextureImporter)AssetImporter.GetAtPath(folder+info.texture);texImporter.alphaSource=TextureImporterAlphaSource.FromInput;texImporter.alphaIsTransparency=true;texImporter.sRGBTexture=true;texImporter.maxTextureSize=2048;texImporter.SaveAndReimport();
                        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+info.texture));
                    }
                    EditorUtility.SetDirty(mat);materials[info.name]=mat;
                }
                foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>materials.TryGetValue(m.name,out var material)?material:throw new Exception("Unknown material "+m.name)).ToArray();
                    renderer.updateWhenOffscreen=true;
                }
                // Normalize by skeleton, not hats, weapons, hair or transparent effect cards.
                var head=animator.GetBoneTransform(HumanBodyBones.Head);var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                float feet=Mathf.Min(left.position.y,right.position.y);
                float bodyHeight=(head.position.y-feet)*1.14f;
                if (bodyHeight <= .1f) throw new Exception("Invalid body height: "+entry.slug+" "+bodyHeight);
                float scale=meta.targetHeight/bodyHeight;instance.transform.localScale*=scale;
                float ground=instance.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>r.bounds.min.y).Min();
                instance.transform.position-=Vector3.up*ground;
                prefabs.Add(PrefabUtility.SaveAsPrefabAsset(instance,Prefabs+entry.slug+".prefab"));
                UnityEngine.Object.DestroyImmediate(instance);
                Debug.Log("CHARACTER IMPORT PASS: "+entry.slug+" human bones="+human.Length);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var player=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();
            if (!player || !player.Animator || !player.Animator.isHuman) throw new Exception("Existing silver humanoid required.");
            var switcher=player.GetComponent<CharacterSwitcher>() ?? player.gameObject.AddComponent<CharacterSwitcher>();
            switcher.Configure(player.Animator.gameObject,prefabs.ToArray(),manifest.characters.Select(e=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Prefabs/EasterEgg/"+e.slug+".prefab")).ToArray());EditorUtility.SetDirty(switcher);
            PrefabUtility.SaveAsPrefabAsset(player.gameObject,"Assets/Characters/Prefabs/Player Humanoid.prefab");
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Library/StaffCharacters");File.WriteAllText("Library/StaffCharacters/import-complete.txt",string.Join("\n",manifest.characters.Select(e=>e.slug)));
            RefreshCatalogBatch();
            Debug.Log("CHARACTER ROSTER READY: silver robot plus "+prefabs.Count+" characters; press C.");
        }
    }
}
