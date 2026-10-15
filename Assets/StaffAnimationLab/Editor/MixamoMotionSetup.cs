using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Animations;using UnityEngine.Playables;using UnityEngine.SceneManagement;
namespace Staff.AnimationLab.Editor {
public static class MixamoMotionSetup {
 const string Folder="Assets/ThirdParty/MixamoAnimations";
 const string Source=Folder+"/ActionAdventurePack/Ch22_nonPBR.fbx";
 const string Out="/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Mixamo/Import";
 static string[] Paths()=>Directory.GetFiles(Folder,"*.fbx",SearchOption.AllDirectories).Where(p=>p!=Source).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
 [MenuItem("Tools/Animation Lab/Import Mixamo Page One")]
 public static void Run(){
  var reference=(ModelImporter)AssetImporter.GetAtPath(Source);Common(reference);reference.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;reference.SaveAndReimport();Configure(Source);
  var avatar=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Avatar>().First();if(!avatar.isValid||!avatar.isHuman)throw new Exception("Invalid Mixamo reference Avatar");
  var rows=new List<string>{"File\tClip\tSeconds\tFPS"};
  foreach(var path in Paths()){
   var imp=(ModelImporter)AssetImporter.GetAtPath(path);Common(imp);imp.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;imp.sourceAvatar=avatar;imp.SaveAndReimport();
   var takes=imp.defaultClipAnimations.Where(t=>t.takeName!="Take 001").ToArray();if(takes.Length!=1)throw new Exception("Expected one motion take: "+path+" got "+takes.Length);
   foreach(var t in takes){t.name=System.IO.Path.GetFileNameWithoutExtension(path);t.loopTime=false;t.loopPose=false;t.lockRootRotation=true;t.lockRootHeightY=true;t.lockRootPositionXZ=true;t.keepOriginalOrientation=true;t.keepOriginalPositionY=true;t.keepOriginalPositionXZ=true;}imp.clipAnimations=takes;imp.SaveAndReimport();
   foreach(var c in Clips(path)){if(!c.humanMotion||c.length<=0)throw new Exception("Invalid clip "+path);rows.Add($"{path}\t{c.name}\t{c.length}\t{c.frameRate}");}
   File.WriteAllText(Out+"/progress.txt",path);
  }
  var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");var entries=catalog.entries.Where(e=>e.category!="Mixamo").ToList();Append(entries);var imported=entries.Where(e=>e.category=="Mixamo").ToArray();if(imported.Length!=117)throw new Exception("Expected 117 motions; got "+imported.Length);
  Validate(catalog,imported.Select(e=>e.clip).ToArray());
  catalog.entries=entries.OrderBy(e=>e.category).ThenBy(e=>e.subcategory).ThenBy(e=>e.title,StringComparer.Ordinal).ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();File.WriteAllLines(Out+"/imported.tsv",rows);
  File.WriteAllLines("Assets/StaffAnimationLab/Inventory.tsv",new[]{"Title\tCategory\tSubcategory\tSource\tClip"}.Concat(catalog.entries.Select(e=>$"{e.title}\t{e.category}\t{e.subcategory}\t{e.source}\t{e.clip.name}")));
  foreach(var name in new[]{"Running","Body Block","Hip Hop Dancing"}){var c=imported.First(e=>e.subcategory=="Page 1"&&e.clip.name==name).clip;var target=catalog.characters.First(p=>p.name=="TheHerta");var label="TargetMixamo-"+name.Replace(' ','_');MeshyRetargetDiagnostic.Capture(label,target,c);File.Copy("/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/RetargetFix/"+label+".png",Out+"/"+label+".png",true);}
  File.WriteAllText(Out+"/result.txt","PASS: Mixamo 117 motions = Page 1 95 + Action Adventure Pack 22. All source clips are Humanoid; 2 characters x 117 clips x 3 phases finite; total catalog="+entries.Count);Debug.Log("MIXAMO PAGE ONE IMPORT COMPLETE");
 }
 static void Common(ModelImporter imp){imp.animationType=ModelImporterAnimationType.Human;imp.preserveHierarchy=false;imp.bakeAxisConversion=true;imp.importAnimation=true;imp.importCameras=false;imp.importLights=false;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;}
 static AnimationClip[] Clips(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
 public static void Append(List<AnimationLabCatalog.Entry> entries){if(File.Exists(MixamoLibraryImport.ManifestPath)){MixamoLibraryImport.Append(entries);return;}if(!Directory.Exists(Folder))return;foreach(var path in Paths())foreach(var c in Clips(path)){if(!c.humanMotion)continue;bool pack=path.Contains("/ActionAdventurePack/");entries.Add(new AnimationLabCatalog.Entry{title=c.name,category="Mixamo",subcategory=pack?"Action Adventure Pack":"Page 1",source="Adobe Mixamo / Ch22_nonPBR / "+System.IO.Path.GetFileName(path),context="",clip=c});}}
 static void Validate(AnimationLabCatalog catalog,AnimationClip[] clips){
  foreach(var prefab in catalog.characters.Where(c=>c.name=="TheHerta"||c.name=="Kafka_NoCoat")){
   var scene=EditorSceneManager.NewPreviewScene();var go=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(go,scene);
   try{foreach(var m in go.GetComponentsInChildren<MonoBehaviour>())m.enabled=false;var a=go.GetComponentInChildren<Animator>();a.runtimeAnimatorController=null;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    foreach(var c in clips){var graph=PlayableGraph.Create("Mixamo verification");try{a.Rebind();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,c);var o=AnimationPlayableOutput.Create(graph,"Motion",a);o.SetSourcePlayable(p);graph.Play();foreach(var f in new[]{.13f,.47f,.79f}){p.SetTime(c.length*f);graph.Evaluate(0);foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftHand,HumanBodyBones.RightFoot}){var pos=a.GetBoneTransform(bone).position;if(!float.IsFinite(pos.x)||!float.IsFinite(pos.y)||!float.IsFinite(pos.z)||pos.magnitude>30)throw new Exception("Invalid pose "+prefab.name+" / "+c.name);}}}finally{graph.Destroy();}}
   }finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
  }
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
   foreach(var kv in map){var t=transforms.FirstOrDefault(t=>t.name=="mixamorig2:"+kv.Value);if(t)actual[kv.Key]=t.name;}
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
   File.WriteAllText(Out+"/avatar-calibration.txt",before+" -> "+after+"; explicit mapped bones="+actual.Count);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
}
