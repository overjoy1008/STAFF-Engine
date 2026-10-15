using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
namespace Staff.AnimationLab.Editor {
[InitializeOnLoad] public static class MeshyMotionSetup {
 public const string Path="Assets/ThirdParty/MeshyAnimations/KafkaLocomotion20.fbx";
 const string Request="Library/StaffMeshyImport.request";
 static MeshyMotionSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(Request);try{Refresh();}catch(Exception e){File.WriteAllText("Library/StaffMeshyImport.result",e.ToString());Debug.LogException(e);}}
 [MenuItem("Tools/Animation Lab/Refresh Meshy Motions")]
 public static void Refresh(){
  AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);
  var imp=(ModelImporter)AssetImporter.GetAtPath(Path);
  imp.animationType=ModelImporterAnimationType.Human;imp.preserveHierarchy=true;
  imp.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
  imp.sourceAvatar=AssetDatabase.LoadAllAssetsAtPath(MeshyAvatarCalibration.Source).OfType<Avatar>().FirstOrDefault();
  if(!imp.sourceAvatar || !imp.sourceAvatar.isValid || !imp.sourceAvatar.isHuman)throw new Exception("Missing calibrated Meshy source Avatar");
  imp.bakeAxisConversion=true;imp.importAnimation=true;imp.importCameras=false;imp.importLights=false;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;
  imp.SaveAndReimport();
  var takes=imp.defaultClipAnimations;
  if(takes.Length!=20)throw new Exception("Expected 20 takes; found "+takes.Length);
  foreach(var c in takes){c.name=c.name.Split('|').Last();c.loopTime=IsLoop(c.name);c.loopPose=c.loopTime;c.lockRootRotation=!c.name.Contains("Turn");c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;}
  imp.clipAnimations=takes;imp.SaveAndReimport();
  var avatar=imp.sourceAvatar;
  if(!avatar||!avatar.isValid||!avatar.isHuman)throw new Exception("Meshy avatar invalid");
  var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");
  var entries=catalog.entries.Where(e=>e.category!="Meshy").ToList();Append(entries);
  var clips=entries.Where(e=>e.category=="Meshy").Select(e=>e.clip).ToArray();
  if(clips.Length<20)throw new Exception("Expected at least 20 Meshy catalog entries");
  int validated=0;
  foreach(var prefab in catalog.characters){
   var scene=EditorSceneManager.NewPreviewScene();GameObject go=null;PlayableGraph graph=default;
   try{
    go=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(go,scene);
    foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
    var animator=go.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=null;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;animator.Rebind();
    foreach(var clip in clips){
    graph=PlayableGraph.Create("Meshy validation");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Meshy",animator);output.SetSourcePlayable(playable);graph.Play();
    Vector3 first=default;float change=0;
    foreach(var f in new[]{.13f,.47f,.79f}){
     playable.SetTime(clip.length*f);graph.Evaluate(0);
     var foot=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
     if(f==.13f)first=foot;else change=Mathf.Max(change,(foot-first).magnitude);
     foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftFoot,HumanBodyBones.RightHand}){var p=animator.GetBoneTransform(bone).position;if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)||p.magnitude>30)throw new Exception("Invalid pose "+prefab.name);}
    }
    if(change<.001f)throw new Exception("Motion did not move foot: "+prefab.name+" / "+clip.name);
    validated++;graph.Destroy();
    }
   }finally{if(graph.IsValid())graph.Destroy();if(go)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
  }
  catalog.entries=entries.OrderBy(e=>e.category).ThenBy(e=>e.title,StringComparer.Ordinal).ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
  File.WriteAllText("Library/StaffMeshyImport.result","PASS: Meshy "+clips.Length+" clips x "+catalog.characters.Length+" characters; "+validated+" retarget combinations with finite and changing poses; catalog entries="+entries.Count);
  Debug.Log("MESHY MOTION IMPORT PASS");
 }
 static bool IsLoop(string name)=>!name.Contains("Turn") && name!="swimming_to_edge";
 public static void Append(List<AnimationLabCatalog.Entry> entries){
  MeshyExpandedSetup.Append(entries);
  foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(Path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"))){
   if(!clip.humanMotion||clip.length<=0)throw new Exception("Invalid Meshy Running clip");
   entries.Add(new AnimationLabCatalog.Entry{title=clip.name.Replace('_',' '),category="Meshy",subcategory="Walking & Running",source="Meshy / Kafka / Walking & Running",context=clip.name=="swimming_to_edge"?"Swim":"Move",clip=clip});
  }
 }
}
}
