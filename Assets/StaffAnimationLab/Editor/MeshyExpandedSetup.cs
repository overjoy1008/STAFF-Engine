using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Animations;using UnityEngine.Playables;using UnityEngine.SceneManagement;
namespace Staff.AnimationLab.Editor {
public static class MeshyExpandedSetup {
 const string Folder="Assets/ThirdParty/MeshyAnimations/WalkingRunning";
 const string Out="/Users/gradient/Documents/GitHub/STAFF/3D Studio/Animation Lab/Meshy/LocomotionExpanded";
 static string[] Paths()=>Directory.GetFiles(Folder,"*.fbx").OrderBy(p=>p,StringComparer.Ordinal).ToArray();
 static AnimationClip[] Clips(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
 public static void Append(List<AnimationLabCatalog.Entry> entries){
  if(!AssetDatabase.IsValidFolder(Folder))return;
  foreach(var path in Paths())foreach(var c in Clips(path)){
   if(c.name=="Running"||c.name=="Walking")continue;
   entries.Add(new AnimationLabCatalog.Entry{title=c.name.Replace('_',' ')+" ["+System.IO.Path.GetFileNameWithoutExtension(path)+"]",category="Meshy",subcategory="Walking & Running",source="Meshy / Kafka / Walking & Running / "+System.IO.Path.GetFileName(path),context=c.name.Contains("Swim")?"Swim":"Move",clip=c});
  }
 }
}
}
