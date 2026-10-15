using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Animations;using UnityEngine.Playables;using UnityEngine.SceneManagement;
namespace Staff.AnimationLab.Editor {
public static class MeshyTemplateComparison {
 const string Out="/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/TemplateComparison";
 static string PathFor(string kind)=>"Assets/StaffAnimationLab/TemplateTests/"+kind+".fbx";
 [MenuItem("Tools/Animation Lab/Compare Mixamo and MeshyRig")]
 public static void Run(){
  var target=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset").characters.First(c=>c.name=="Kafka_NoCoat");var report=new List<string>();
  foreach(var kind in new[]{"Mixamo60","MeshyRig60"}){
   var p=PathFor(kind);var imp=(ModelImporter)AssetImporter.GetAtPath(p);imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.bakeAxisConversion=true;imp.preserveHierarchy=true;imp.importAnimation=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;var reset=imp.humanDescription;reset.human=new HumanBone[0];reset.skeleton=new SkeletonBone[0];imp.humanDescription=reset;imp.SaveAndReimport();
   var takes=imp.defaultClipAnimations.Where(t=>kind=="MeshyRig60"||t.name.Contains("Flirty")).ToArray();foreach(var t in takes){t.name=kind=="MeshyRig60"?"Flirty_Strut":t.name.Split('|').Last();t.loopTime=false;t.loopPose=false;t.lockRootRotation=true;t.lockRootHeightY=true;t.lockRootPositionXZ=true;t.keepOriginalOrientation=true;t.keepOriginalPositionY=true;t.keepOriginalPositionXZ=true;}imp.clipAnimations=takes;imp.SaveAndReimport();
   report.Add(kind+" mappings: "+string.Join(";",imp.humanDescription.human.Select(h=>h.humanName+"="+h.boneName)));
   foreach(var c in Clips(kind))report.Add(kind+" clip "+c.name+" duration="+c.length+" fps="+c.frameRate);
  }
  foreach(var mode in new[]{"Auto","Explicit"}){
   if(mode=="Explicit")foreach(var kind in new[]{"Mixamo60","MeshyRig60"})Configure(PathFor(kind));
   var b=Clips("MeshyRig60").Single();
   foreach(var a in Clips("Mixamo60")){
    float sum=0,max=0,relativeSum=0,relativeMax=0;int count=0;
    foreach(var phase in new[]{.13f,.47f,.79f}){
     var pa=Pose(target,a,phase);var pb=Pose(target,b,phase);
     foreach(var bone in pa.Keys){float angle=Quaternion.Angle(pa[bone],pb[bone]);sum+=angle;max=Mathf.Max(max,angle);float relative=Quaternion.Angle(Quaternion.Inverse(pa[HumanBodyBones.Hips])*pa[bone],Quaternion.Inverse(pb[HumanBodyBones.Hips])*pb[bone]);relativeSum+=relative;relativeMax=Mathf.Max(relativeMax,relative);count++;}
    }
    report.Add(mode+" "+a.name+" vs "+b.name+": mean world joint angle="+(sum/count)+" max="+max+" n="+count+"; hips-relative mean="+(relativeSum/count)+" max="+relativeMax);
    MeshyRetargetDiagnostic.Capture("TargetTemplate-"+mode+"-Mixamo-"+a.name,target,a,.47f);
   }
   MeshyRetargetDiagnostic.Capture("TargetTemplate-"+mode+"-MeshyRig",target,b,.47f);
  }
  foreach(var kind in new[]{"Mixamo60","MeshyRig60"}){var imp=(ModelImporter)AssetImporter.GetAtPath(PathFor(kind));imp.animationType=ModelImporterAnimationType.Generic;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.SaveAndReimport();}
  foreach(var a in Clips("Mixamo60")){float sum=0,max=0;int n=0;foreach(var phase in new[]{.13f,.47f,.79f}){var pa=RawPose("Mixamo60",a,phase);var pb=RawPose("MeshyRig60",Clips("MeshyRig60").Single(),phase);for(int i=0;i<pa.Length;i++){float angle=Quaternion.Angle(pa[i],pb[i]);sum+=angle;max=Mathf.Max(max,angle);n++;}}report.Add("RAW Generic "+a.name+" hips-relative mean="+sum/n+" max="+max);}
  foreach(var kind in new[]{"Mixamo60","MeshyRig60"})Configure(PathFor(kind));
  File.WriteAllLines(Out+"/unity_results.txt",report);
 }
 static AnimationClip[] Clips(string kind)=>AssetDatabase.LoadAllAssetsAtPath(PathFor(kind)).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
 static Dictionary<HumanBodyBones,Quaternion> Pose(GameObject prefab,AnimationClip clip,float fraction){
  var scene=EditorSceneManager.NewPreviewScene();var go=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(go,scene);PlayableGraph graph=default;
  try{foreach(var m in go.GetComponentsInChildren<MonoBehaviour>())m.enabled=false;var a=go.GetComponentInChildren<Animator>();a.runtimeAnimatorController=null;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();graph=PlayableGraph.Create("Template test");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,clip);var o=AnimationPlayableOutput.Create(graph,"Test",a);o.SetSourcePlayable(p);graph.Play();p.SetTime(clip.length*fraction);graph.Evaluate(0);
   return new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot}.ToDictionary(b=>b,b=>a.GetBoneTransform(b).rotation);
  }finally{if(graph.IsValid())graph.Destroy();UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
 }
 static Quaternion[] RawPose(string kind,AnimationClip clip,float phase){var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(kind)));try{clip.SampleAnimation(go,clip.length*phase);var ts=go.GetComponentsInChildren<Transform>();string prefix=kind=="Mixamo60"?"mixamorig:":"";var hips=ts.First(t=>t.name==prefix+"Hips").rotation;return new[]{"Hips","LeftArm","LeftForeArm","RightArm","RightForeArm","LeftUpLeg","LeftLeg","RightUpLeg","RightLeg"}.Select(b=>Quaternion.Inverse(hips)*ts.First(t=>t.name==prefix+b).rotation).ToArray();}finally{UnityEngine.Object.DestroyImmediate(go);}}
 public static void Configure(string path){
  var importer=(ModelImporter)AssetImporter.GetAtPath(path);var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=AssetDatabase.LoadAssetAtPath<GameObject>(path).name;go.hideFlags=HideFlags.HideAndDontSave;
  try{
   var map=new Dictionary<string,string>{{"Hips","Hips"},{"Spine","Spine"},{"Chest","Spine1"},{"UpperChest","Spine2"},{"Neck","Neck"},{"Head","Head"}};
   foreach(var side in new[]{"Left","Right"}){
    foreach(var pair in new[]{("Shoulder","Shoulder"),("UpperArm","Arm"),("LowerArm","ForeArm"),("Hand","Hand"),("UpperLeg","UpLeg"),("LowerLeg","Leg"),("Foot","Foot"),("Toes","ToeBase")})map[side+pair.Item1]=side+pair.Item2;
    foreach(var finger in new[]{("Thumb","Thumb"),("Index","Index"),("Middle","Middle"),("Ring","Ring"),("Little","Pinky")})for(int i=0;i<3;i++)map[side+" "+finger.Item1+" "+new[]{"Proximal","Intermediate","Distal"}[i]]=side+"Hand"+finger.Item2+(i+1);
   }
   var transforms=go.GetComponentsInChildren<Transform>();var actual=new Dictionary<string,string>();
   foreach(var kv in map){var alias=kv.Value;
    if(!transforms.Any(t=>t.name=="mixamorig:Hips"))alias=alias=="Spine"?"Spine02":alias=="Spine1"?"Spine01":alias=="Spine2"?"Spine":alias=="Neck"?"neck":alias;
    var t=transforms.FirstOrDefault(t=>t.name=="mixamorig:"+alias||t.name==alias);if(t)actual[kv.Key]=t.name;}
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
   File.WriteAllText(Out+"/"+System.IO.Path.GetFileNameWithoutExtension(path)+"-pose_error.txt",before+" -> "+after+"; explicit mapped bones="+actual.Count);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
}
