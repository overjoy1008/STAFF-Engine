using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace Staff.Characters.Editor
{
    public static class DanceSetup
    {
        const string Source="Assets/ThirdParty/StaffDances/";
        [Serializable] public class Entry {public string id,title,author,source;public int number;public float duration,audioStart;public bool audio;}
        [Serializable] public class Manifest {public Entry[] dances;}
        [MenuItem("Tools/Characters/Import Dance Library")]
        public static void BuildBatch()
        {
            AssetDatabase.Refresh();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Source+"manifest.json"));
            var avatar=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/StaffCharacters/Nicole/Nicole.fbx").OfType<Avatar>().First(a=>a.isHuman && a.isValid);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Characters/Animation/HumanoidAdventure.controller");
            if(!controller.parameters.Any(p=>p.name=="Dance"))controller.AddParameter("Dance",AnimatorControllerParameterType.Int);
            Directory.CreateDirectory("Assets/Characters/Resources");AssetDatabase.Refresh();
            const string catalogPath="Assets/Characters/Resources/StaffDanceCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<DanceCatalog>(catalogPath);
            if(!catalog){catalog=ScriptableObject.CreateInstance<DanceCatalog>();AssetDatabase.CreateAsset(catalog,catalogPath);}
            var entries=new DanceCatalog.Entry[manifest.dances.Length];
            var machine=controller.layers[0].stateMachine;
            var locomotion=machine.states.First(s=>s.state.name=="Locomotion").state;
            foreach(var item in manifest.dances)
            {
                string path=Source+item.id+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
                importer.animationType=ModelImporterAnimationType.Human;
                importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;importer.sourceAvatar=avatar;
                importer.animationCompression=ModelImporterAnimationCompression.Off;importer.optimizeGameObjects=false;
                importer.preserveHierarchy=true;importer.optimizeBones=false;
                var clips=importer.defaultClipAnimations;
                if(clips.Length!=1)throw new Exception("Expected one baked take: "+item.id);
                clips[0].name="Dance_"+item.id;clips[0].loopTime=true;clips[0].loopPose=false;
                clips[0].lockRootRotation=true;clips[0].keepOriginalOrientation=true;
                clips[0].lockRootHeightY=true;clips[0].keepOriginalPositionY=true;
                clips[0].lockRootPositionXZ=true;clips[0].keepOriginalPositionXZ=true;
                importer.clipAnimations=clips;importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>c.name=="Dance_"+item.id);
                if(!clip || !clip.humanMotion || Mathf.Abs(clip.length-item.duration)>.08f)throw new Exception("Invalid dance clip: "+item.id);
                var state=machine.states.FirstOrDefault(s=>s.state.name==clip.name).state;
                if(!state)state=machine.AddState(clip.name);
                state.motion=clip;state.writeDefaultValues=true;
                if(!state.transitions.Any(t=>t.destinationState==locomotion))
                {var exit=state.AddTransition(locomotion);exit.hasExitTime=false;exit.duration=.08f;exit.hasFixedDuration=true;exit.AddCondition(AnimatorConditionMode.Equals,0,"Dance");}
                entries[item.number-1]=new DanceCatalog.Entry{id=item.id,title=item.title,clip=clip,audioStart=item.audioStart,music=item.audio?AssetDatabase.LoadAssetAtPath<AudioClip>(Source+item.id+".mp3"):null};
                if(item.audio&&!entries[item.number-1].music)throw new Exception("Missing bundled music: "+item.id);
                Debug.Log("DANCE IMPORT PASS: "+item.id+" duration="+clip.length+" Humanoid="+clip.humanMotion);
            }
            catalog.dances=entries;EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Library/StaffDance");File.WriteAllText("Library/StaffDance/import-result.txt","PASS: 8 Humanoid dance clips, number-key catalog, bundled audio, existing locomotion controller preserved.");
        }
    }
}
