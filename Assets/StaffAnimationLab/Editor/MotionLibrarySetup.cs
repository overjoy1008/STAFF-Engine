using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace Staff.AnimationLab.Editor {
public static class MotionLibrarySetup {
 public const string TripoPath="Assets/ThirdParty/TripoAnimations/Tripo97.fbx";
 [MenuItem("Tools/Animation Lab/Refresh Imported Motions")]
 public static void RefreshCatalog() {
  ConfigureTripo();
  var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");
  var entries=catalog.entries.Where(e=>!e.category.StartsWith("Tripo",StringComparison.Ordinal) && e.category!="Meshy" && e.category!="Mixamo").ToList();AppendImported(entries);
  catalog.entries=entries.OrderBy(e=>e.category).ThenBy(e=>e.title,StringComparer.Ordinal).ToArray();
  EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
  Debug.Log("MOTION LIBRARY: Tripo="+entries.Count(e=>e.category=="Tripo")+", total="+entries.Count);
 }
 public static void ConfigureTripo() {
  var imp=AssetImporter.GetAtPath(TripoPath) as ModelImporter;
  if(!imp)throw new Exception("Missing Tripo animation FBX");
  imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  imp.bakeAxisConversion=true;imp.importAnimation=true;imp.importCameras=false;imp.importLights=false;
  imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;
  imp.SaveAndReimport();
  var takes=imp.defaultClipAnimations;
  if(takes.Length!=97)throw new Exception("Expected 97 Tripo takes, found "+takes.Length);
  foreach(var take in takes){
   take.name=take.name.Split('|').Last();take.loopTime=IsLoop(take.name);take.loopPose=take.loopTime;
   take.lockRootRotation=true;take.lockRootHeightY=true;take.lockRootPositionXZ=false;
   take.keepOriginalOrientation=true;take.keepOriginalPositionY=true;take.keepOriginalPositionXZ=false;
  }
  imp.clipAnimations=takes;imp.SaveAndReimport();
  var avatar=AssetDatabase.LoadAllAssetsAtPath(TripoPath).OfType<Avatar>().FirstOrDefault();
  if(!avatar||!avatar.isHuman||!avatar.isValid)throw new Exception("Tripo humanoid mapping failed");
 }
 public static void AppendImported(List<AnimationLabCatalog.Entry> entries) {
  MeshyMotionSetup.Append(entries);
  MixamoMotionSetup.Append(entries);
  var clips=AssetDatabase.LoadAllAssetsAtPath(TripoPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
  if(clips.Length>0 && clips.Length!=97)throw new Exception("Tripo clip count mismatch");
  foreach(var clip in clips){
   if(!clip.humanMotion)throw new Exception("Non-humanoid Tripo clip "+clip.name);
   var title=Regex.Replace(clip.name,@"_(\d+)$",m=>" "+(char)('A'+int.Parse(m.Groups[1].Value)-1)).Replace('_',' ').Replace('-',' ');
   title=System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(title);
   entries.Add(new AnimationLabCatalog.Entry{title=title,category="Tripo",source="Tripo / user-provided theherta.glb",context=clip.name=="sit"?"Sit":clip.name=="swim"?"Swim":clip.name=="climb"?"Climb":"",clip=clip});
  }

 }
 [MenuItem("Tools/Animation Lab/Keep Tripo GLB Only")]
 public static void KeepTripoGlbOnly(){
  var catalog=AssetDatabase.LoadAssetAtPath<AnimationLabCatalog>("Assets/StaffAnimationLab/Catalog.asset");
  var entries=catalog.entries.Where(e=>e.category!="Tripo FBX").ToList();
  foreach(var e in entries)if(e.category=="Tripo GLB")e.category="Tripo";
  catalog.entries=entries.OrderBy(e=>e.category).ThenBy(e=>e.title,StringComparer.Ordinal).ToArray();
  EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
  System.IO.File.WriteAllLines("Assets/StaffAnimationLab/Inventory.tsv",new[]{"Title\tCategory\tSource\tClip"}.Concat(catalog.entries.Select(e=>$"{e.title}\t{e.category}\t{e.source}\t{e.clip.name}")));
  AssetDatabase.DeleteAsset("Assets/StaffAnimationLab/TemplateTests/TripoMixamo.fbx");
  AssetDatabase.DeleteAsset("Assets/StaffAnimationLab/Editor/TripoFormatComparison.cs");
  Debug.Log("TRIPO GLB RESTORED: "+entries.Count(e=>e.category=="Tripo"));
 }
 static bool IsLoop(string n)=>new[]{"idle","standing_relax","run","walk","swagger","swim","climb","sit","wait","surf","press-up","shovel","dig","dribble","jump_rope_01","jump_rope_02","warm_up"}.Contains(n)||n.StartsWith("dance_");
}
}
