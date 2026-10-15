using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
namespace Staff.AnimationLab.Editor {
[Serializable] public sealed class MixamoLibraryManifest {
 public MixamoLibraryMotion[] motions;
 public MixamoLibraryPack[] packs;
}
[Serializable] public sealed class MixamoLibraryMotion { public string id, title, path, subcategory, sha256; public string[] packSubcategories; }
[Serializable] public sealed class MixamoLibraryPack { public string id, title; public string[] motionIds; }
public static class MixamoLibraryImport {
 public const string Root="Assets/ThirdParty/MixamoLibrary";
 public const string ManifestPath=Root+"/Catalog.json";
 public const string Reference="Assets/ThirdParty/MixamoAnimations/ActionAdventurePack/Ch22_nonPBR.fbx";
 public const string ReportRoot="/Users/gradient/Documents/GitHub/STAFF/3D Assets/Animation Library/Mixamo/Reports/Unity Import";
 static MixamoLibraryManifest manifest;
 static Dictionary<string,MixamoLibraryMotion> paths;
 static Avatar referenceAvatar;
 static AnimationLabCatalog buildingCatalog;
 static List<AnimationLabCatalog.Entry> buildingEntries;
 public static MixamoLibraryManifest Manifest => manifest??=JsonUtility.FromJson<MixamoLibraryManifest>(File.ReadAllText(ManifestPath));
 public static MixamoLibraryMotion Find(string path) { if(!File.Exists(ManifestPath))return null;paths??=Manifest.motions.ToDictionary(m=>m.path,StringComparer.Ordinal);return paths.TryGetValue(path,out var m)?m:null; }
 public static Avatar ReferenceAvatar(){if(!referenceAvatar)referenceAvatar=AssetDatabase.LoadAssetAtPath<Avatar>(Reference);return referenceAvatar&&referenceAvatar.isHuman&&referenceAvatar.isValid?referenceAvatar:null;}
 public static void ConfigureModel(ModelImporter importer) {
  importer.globalScale=1f;importer.useFileUnits=true;importer.useFileScale=true;
  importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;importer.sourceAvatar=ReferenceAvatar();
  importer.preserveHierarchy=false;importer.bakeAxisConversion=true;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationCompression=ModelImporterAnimationCompression.Off;
 }
 public static void ConfigureTakes(ModelImporter importer,MixamoLibraryMotion motion) {
  var takes=importer.defaultClipAnimations.Where(t=>t.takeName!="Take 001").ToArray();
  if(takes.Length!=1)throw new Exception("Expected one animation take for "+motion.id+", found "+takes.Length);
  foreach(var t in takes){t.name=motion.title;t.loopTime=false;t.loopPose=false;t.lockRootRotation=true;t.lockRootHeightY=true;t.lockRootPositionXZ=true;t.keepOriginalOrientation=true;t.keepOriginalPositionY=true;t.keepOriginalPositionXZ=true;if(t.lastFrame<=t.firstFrame)t.lastFrame=t.firstFrame+1;}
  importer.clipAnimations=takes;
 }
 static AnimationClip Clip(string path)=>AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal));
 public static void Append(List<AnimationLabCatalog.Entry> entries) {
  int count=0;foreach(var m in Manifest.motions){var clip=Clip(m.path);if(!clip.humanMotion||clip.length<=0)throw new Exception("Invalid Mixamo clip "+m.id);
   var primary=Manifest.packs.FirstOrDefault(p=>Normalize(p.title)==Normalize(m.subcategory))?.title??m.subcategory;
   entries.Add(new AnimationLabCatalog.Entry{title=m.title,category="Mixamo",subcategory=primary,additionalSubcategories=m.packSubcategories,catalogId=m.id,source="Adobe Mixamo / "+m.id,context="",clip=clip});if(++count%100==0)EditorUtility.UnloadUnusedAssetsImmediate(true);}
 }
 static string Normalize(string value)=>new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
 [Serializable] sealed class Report {public string status;public int expectedMotions,validMotions,packGroups,catalogEntries,retargetedCombinations,poseSamples,changedOtherEntries;public string[] errors;}
 [MenuItem("Tools/Animation Lab/Import Complete Mixamo Library")]
 public static void Run() {
  Directory.CreateDirectory(ReportRoot);var report=new Report{status="running",expectedMotions=2446,packGroups=38};var errors=new List<string>();File.WriteAllText(ReportRoot+"/progress.json",JsonUtility.ToJson(report,true));
  try {
   if(Manifest.motions.Length!=2446||Manifest.packs.Length!=38||Manifest.motions.Select(m=>m.id).Distinct().Count()!=2446)throw new Exception("Manifest ID count mismatch");
   if(!ReferenceAvatar())MixamoMotionSetup.Configure(Reference);
   if(!ReferenceAvatar())throw new Exception("Missing valid Mixamo reference Avatar");
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   foreach(var m in Manifest.motions){
    try {
     var importer=(ModelImporter)AssetImporter.GetAtPath(m.path);if(!importer)throw new Exception("Missing importer");
     var clips=AssetDatabase.LoadAllAssetRepresentationsAtPath(m.path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
     if(!importer.useFileScale||importer.animationType!=ModelImporterAnimationType.Human||importer.sourceAvatar!=ReferenceAvatar()||clips.Length!=1||!clips[0].humanMotion||clips[0].name!=m.title){ConfigureModel(importer);ConfigureTakes(importer,m);importer.SaveAndReimport();}
     var c=Clip(m.path);if(!c.humanMotion||c.length<=0||Math.Abs(c.frameRate-60)>0.01f)throw new Exception($"Invalid Humanoid/length/FPS: human={c.humanMotion} length={c.length} fps={c.frameRate}");report.validMotions++;
    }catch(Exception e){errors.Add(m.id+" / "+m.path+" / "+e.Message);}
    if((report.validMotions+errors.Count)%100==0){File.WriteAllText(ReportRoot+"/progress.json",JsonUtility.ToJson(report,true));Debug.Log("MIXAMO LIBRARY PROGRESS "+report.validMotions+" valid / "+errors.Count+" errors");EditorUtility.UnloadUnusedAssetsImmediate(true);}
   }
   if(errors.Count>0)throw new Exception("Mixamo import failed for "+errors.Count+" files");
   EditorUtility.UnloadUnusedAssetsImmediate(true);buildingCatalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");var catalog=buildingCatalog;var other=catalog.entries.Where(e=>e.category!="Mixamo").ToArray();buildingEntries=other.ToList();var entries=buildingEntries;Append(entries);EditorUtility.UnloadUnusedAssetsImmediate(true);
   var mixamo=entries.Where(e=>e.category=="Mixamo").ToArray();if(mixamo.Length!=2446||mixamo.Select(e=>e.catalogId).Distinct().Count()!=2446)throw new Exception("Duplicate/missing catalog IDs");
   foreach(var pack in Manifest.packs){var actual=mixamo.Where(e=>e.subcategory==pack.title||(e.additionalSubcategories?.Contains(pack.title)??false)).Select(e=>e.catalogId).ToHashSet();if(!actual.SetEquals(pack.motionIds))throw new Exception("Pack membership mismatch "+pack.id);}
   Validate(catalog,mixamo,report);
   catalog.entries=entries.OrderBy(e=>e.category).ThenBy(e=>e.subcategory).ThenBy(e=>e.title,StringComparer.Ordinal).ToArray();report.catalogEntries=catalog.entries.Length;report.changedOtherEntries=other.Count(e=>!catalog.entries.Contains(e));if(report.changedOtherEntries!=0)throw new Exception("Other catalog entries changed");
   EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
   File.WriteAllLines("Assets/StaffAnimationLab/Inventory.tsv",new[]{"Title\tCategory\tSubcategory\tCatalog ID\tPack subcategories\tSource\tClip"}.Concat(catalog.entries.Select(e=>$"{e.title}\t{e.category}\t{e.subcategory}\t{e.catalogId}\t{string.Join("; ",e.additionalSubcategories??Array.Empty<string>())}\t{e.source}\t{e.clip.name}")));
   report.status="complete";report.errors=errors.ToArray();File.WriteAllText(ReportRoot+"/result.json",JsonUtility.ToJson(report,true));Debug.Log("MIXAMO COMPLETE LIBRARY IMPORT PASS: "+JsonUtility.ToJson(report));buildingEntries=null;buildingCatalog=null;
   if(Application.isBatchMode)EditorApplication.Exit(0);
  }catch(Exception e){errors.Add(e.ToString());report.status="failed";report.errors=errors.ToArray();File.WriteAllText(ReportRoot+"/result.json",JsonUtility.ToJson(report,true));Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
 }
 static void Validate(AnimationLabCatalog catalog,AnimationLabCatalog.Entry[] motions,Report report) {
  var targets=catalog.characters.Where(c=>c&&(c.name=="Nicole"||c.name=="TheHerta"||c.name=="Kafka_NoCoat")).ToArray();if(targets.Length<2)throw new Exception("Expected validation characters");
  foreach(var prefab in targets){var scene=EditorSceneManager.NewPreviewScene();var go=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(go,scene);
   try{foreach(var b in go.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;var a=go.GetComponentInChildren<Animator>();if(!a||!a.isHuman||!a.avatar.isValid)throw new Exception("Invalid character "+prefab.name);a.runtimeAnimatorController=null;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    foreach(var e in motions){var graph=PlayableGraph.Create("Full Mixamo validation");try{a.Rebind();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,e.clip);p.SetApplyFootIK(false);var o=AnimationPlayableOutput.Create(graph,"Motion",a);o.SetSourcePlayable(p);graph.Play();foreach(var f in new[]{0f,.47f,.91f}){p.SetTime(e.clip.length*f);graph.Evaluate(0);foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftHand,HumanBodyBones.RightFoot}){var t=a.GetBoneTransform(bone);if(!t)throw new Exception("Missing bone "+bone);var pos=t.position;if(!float.IsFinite(pos.x)||!float.IsFinite(pos.y)||!float.IsFinite(pos.z)||pos.magnitude>30)throw new Exception("Invalid pose "+prefab.name+" / "+e.catalogId+" / "+bone);}report.poseSamples++;}report.retargetedCombinations++;}finally{graph.Destroy();}}
    Debug.Log("MIXAMO FULL RETARGET PASS "+prefab.name);File.WriteAllText(ReportRoot+"/progress.json",JsonUtility.ToJson(report,true));
   }finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}
public sealed class MixamoLibraryPostprocessor : AssetPostprocessor {
 void OnPreprocessModel(){if(!assetPath.StartsWith(MixamoLibraryImport.Root+"/",StringComparison.Ordinal)||!assetPath.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase))return;MixamoLibraryImport.ConfigureModel((ModelImporter)assetImporter);}
 void OnPreprocessAnimation(){var motion=MixamoLibraryImport.Find(assetPath);if(motion!=null)MixamoLibraryImport.ConfigureTakes((ModelImporter)assetImporter,motion);}
}
}
