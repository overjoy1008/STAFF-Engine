using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Staff.Subway.Editor {
 public static class SubwayTransitSetup {
 static void InstallAfterStop(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode)return;EditorApplication.playModeStateChanged-=InstallAfterStop;EditorApplication.delayCall+=Install;}
 [MenuItem("STAFF/Subway/Install Transit And Bake Reflections")]
 static void Install(){
  if(EditorApplication.isPlaying){EditorApplication.playModeStateChanged+=InstallAfterStop;EditorApplication.isPlaying=false;return;}
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/STAFF_Subway_Reference.unity");bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity",OpenSceneMode.Additive);
  try{
   if(!EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-transit.unity",true))throw new Exception("Backup failed");
   var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment");
   var train=env.transform.Find("Tripo - PBR Glow Train");
   var transit=env.GetComponent<SubwayTransit>();if(!transit)transit=env.AddComponent<SubwayTransit>();
   transit.doors=env.GetComponent<SubwayDoors>();
   transit.movingParts=new[]{train}.Concat(env.transform.Find("Boarding and car collision").Cast<Transform>().Where(t=>t.name!="Platform edge barrier")).ToArray();
   transit.dockPositions=transit.movingParts.Select(t=>t.position).ToArray();
   var probe=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ReflectionProbe>()).Single();
   // Reflection probe dimensions do not follow Transform scale.
   probe.center=probe.transform.InverseTransformDirection(new Vector3(0,3,60)-probe.transform.position);
   probe.size=new Vector3(32,16,210);probe.resolution=256;probe.mode=ReflectionProbeMode.Custom;
   const string path="Assets/StaffSubway/Textures/StationReflection.exr";
   // Capture the station without freezing a train image into its own reflections.
   var parts=transit.movingParts;var active=parts.Select(t=>t.gameObject.activeSelf).ToArray();
   try{foreach(var t in parts)t.gameObject.SetActive(false);
    if(!Lightmapping.BakeReflectionProbe(probe,path))throw new Exception("Reflection bake failed");
   }finally{for(int i=0;i<parts.Length;i++)parts[i].gameObject.SetActive(active[i]);}
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
   probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
   if(!probe.customBakedTexture)throw new Exception("Baked cubemap missing");
   foreach(var car in env.GetComponentsInChildren<SubwayCar>())foreach(var r in car.GetComponentsInChildren<Renderer>()){
    if(!probe.bounds.Intersects(r.bounds))throw new Exception("Probe misses "+r.name);
   }
   EditorUtility.SetDirty(probe);EditorUtility.SetDirty(transit);EditorSceneManager.MarkSceneDirty(scene);
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText("Library/StaffSubway/transit-install.txt","PASS: station reflection rebaked and explicitly assigned; full train coverage; transit roots installed.");
  }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 }
}
