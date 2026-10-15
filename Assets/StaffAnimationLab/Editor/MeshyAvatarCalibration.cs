using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEditor;using UnityEngine;
namespace Staff.AnimationLab.Editor {
public static class MeshyAvatarCalibration {
 const string Out="/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/RetargetFix";
 public const string Source="Assets/ThirdParty/MeshyAnimations/KafkaMeshyControl.fbx";
 [MenuItem("Tools/Animation Lab/Calibrate Meshy Avatar")]
 public static void Run(){
  Configure(Source);
  var target=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset").characters.First(c=>c.name=="Kafka_NoCoat");
  var clip=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().First(c=>c.name=="Running");
  MeshyRetargetDiagnostic.Capture("SourceCalibrated",AssetDatabase.LoadAssetAtPath<GameObject>(Source),clip);
  MeshyRetargetDiagnostic.Capture("TargetCalibrated",target,clip);
  var motions=(ModelImporter)AssetImporter.GetAtPath(MeshyMotionSetup.Path);
  motions.preserveHierarchy=true;motions.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;motions.sourceAvatar=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Avatar>().First();motions.SaveAndReimport();
  var fixedClip=AssetDatabase.LoadAllAssetsAtPath(MeshyMotionSetup.Path).OfType<AnimationClip>().First(c=>c.name=="Running");
  MeshyRetargetDiagnostic.Capture("TargetFixed",target,fixedClip);
  File.WriteAllText(Out+"/calibration_done.txt","PASS: motion FBX now uses calibrated source Avatar");
 }
 public static void Configure(string path){
  var importer=(ModelImporter)AssetImporter.GetAtPath(path);var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=AssetDatabase.LoadAssetAtPath<GameObject>(path).name;go.hideFlags=HideFlags.HideAndDontSave;
  try{
   var map=new Dictionary<string,string>{{"Hips","Hips"},{"Spine","Spine"},{"Chest","Spine1"},{"UpperChest","Spine2"},{"Neck","Neck"},{"Head","Head"}};
   foreach(var side in new[]{"Left","Right"}){
    foreach(var pair in new[]{("Shoulder","Shoulder"),("UpperArm","Arm"),("LowerArm","ForeArm"),("Hand","Hand"),("UpperLeg","UpLeg"),("LowerLeg","Leg"),("Foot","Foot"),("Toes","ToeBase")})map[side+pair.Item1]=side+pair.Item2;
    foreach(var finger in new[]{("Thumb","Thumb"),("Index","Index"),("Middle","Middle"),("Ring","Ring"),("Little","Pinky")})for(int i=0;i<3;i++)map[side+" "+finger.Item1+" "+new[]{"Proximal","Intermediate","Distal"}[i]]=side+"Hand"+finger.Item2+(i+1);
   }
   var transforms=go.GetComponentsInChildren<Transform>();var actual=new Dictionary<string,string>();
   foreach(var kv in map){var t=transforms.FirstOrDefault(t=>t.name=="mixamorig:"+kv.Value);if(t)actual[kv.Key]=t.name;}
   var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AvatarSetupTool");var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
   type.GetMethod("SampleBindPose",flags).Invoke(null,new object[]{go});
   var method=type.GetMethods(flags).Single(m=>m.Name=="GetHumanBones"&&m.GetParameters()[0].ParameterType==typeof(Dictionary<string,string>));
   var wrappers=method.Invoke(null,new object[]{actual,transforms.ToDictionary(t=>t,t=>true)});
   float before=(float)type.GetMethod("GetPoseError",flags).Invoke(null,new[]{wrappers});
   type.GetMethod("MakePoseValid",flags).Invoke(null,new[]{wrappers});
   float after=(float)type.GetMethod("GetPoseError",flags).Invoke(null,new[]{wrappers});
   var desc=importer.humanDescription;desc.human=actual.Select(kv=>new HumanBone{humanName=kv.Key,boneName=kv.Value,limit=new HumanLimit{useDefaultValues=true}}).ToArray();
   desc.skeleton=(SkeletonBone[])type.GetMethod("GetSkeletonBones",flags).Invoke(null,new object[]{go.transform});
   importer.humanDescription=desc;importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();
   File.WriteAllText(Out+"/calibration_pose_error.txt",before+" -> "+after+"; explicit mapped bones="+actual.Count);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
}
