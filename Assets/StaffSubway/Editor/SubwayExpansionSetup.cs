using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Staff.Subway.Editor {
 public static class SubwayExpansionSetup {
 [MenuItem("STAFF/Subway/Expand Station And Install Doors")]
 static void Install(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before scene changes.");
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/STAFF_Subway_Reference.unity");bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity",OpenSceneMode.Additive);
  try{
   var roots=scene.GetRootGameObjects();var env=roots.Single(g=>g.name=="Environment");
   if(env.GetComponent<SubwayDoors>())throw new Exception("Doors already installed; refusing to scale the station twice.");
   EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-expansion.unity",true);
   foreach(var root in roots.Where(g=>g.name=="Environment"||g.name=="Lighting")){
    root.transform.position*=1.5f;root.transform.localScale*=1.5f;
   }
   foreach(var light in roots.SelectMany(g=>g.GetComponentsInChildren<Light>(true)))if(light.type!=LightType.Directional)light.range*=1.5f;
   // Keep human scale and the player camera rig; move reference people with the platform.
   var references=roots.SingleOrDefault(g=>g.name=="Scale References - 1.70m");
   if(references)foreach(Transform person in references.transform)person.position*=1.5f;
   foreach(var camera in roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true))){
    if(!camera.GetComponent<Staff.Characters.AdventureCamera>())camera.transform.position*=1.5f;
    camera.farClipPlane*=1.5f;
   }
   foreach(var mat in env.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.HasProperty("_TileSize")&&m.HasProperty("_Grid")&&m.GetFloat("_Grid")>0).Distinct()){
    mat.SetFloat("_TileSize",mat.GetFloat("_TileSize")*1.5f);EditorUtility.SetDirty(mat);
   }
   BindAuthoredDoors(env);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText("Library/StaffSubway/expansion-install.txt","PASS: station and lighting scaled 1.5; humans unchanged; 85 authored GLB door tracks installed. O toggles doors.");
  }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 [Serializable] class DoorData {public string source,sha256,clip;public float openTime;public Vector3 sourceSize;public Track[] tracks;}
 [Serializable] class Track {public string node;public float[] times;public Vector3[] deltas;}
 [MenuItem("STAFF/Subway/Bind Authored GLB Doors")]
 static void Bind(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/STAFF_Subway_Reference.unity");bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity",OpenSceneMode.Additive);
  try{BindAuthoredDoors(scene.GetRootGameObjects().Single(g=>g.name=="Environment"));EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
  finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 static void BindAuthoredDoors(GameObject env){
  var data=JsonUtility.FromJson<DoorData>(File.ReadAllText("Assets/StaffSubway/Models/Train/DoorAnimation.json"));
  var doors=env.GetComponent<SubwayDoors>()??env.AddComponent<SubwayDoors>();var leaves=new System.Collections.Generic.List<SubwayDoors.Leaf>();
  foreach(var car in env.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("PBR Glow car "))){
   var renderers=car.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
   // GLB RH coordinates to the existing FBX LH scene; account for the scene's nonuniform import fit.
   var scale=new Vector3(-bounds.size.x/data.sourceSize.x,bounds.size.y/data.sourceSize.y,bounds.size.z/data.sourceSize.z);
   foreach(var track in data.tracks){
    var t=car.GetComponentsInChildren<Transform>().Single(x=>x.name==track.node);
    var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
    for(int i=0;i<track.times.Length;i++){
     var offset=t.parent.InverseTransformVector(Vector3.Scale(track.deltas[i],scale));
     for(int axis=0;axis<3;axis++)curves[axis].AddKey(new Keyframe(track.times[i],offset[axis]));
    }
    foreach(var curve in curves)for(int k=0;k<curve.length;k++){AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.Linear);}
    leaves.Add(new SubwayDoors.Leaf{transform=t,closedLocalPosition=t.localPosition,x=curves[0],y=curves[1],z=curves[2]});
   }
  }
  if(leaves.Count!=85)throw new Exception("Expected 85 authored door tracks, got "+leaves.Count);
  doors.leaves=leaves.ToArray();doors.duration=data.openTime;EditorUtility.SetDirty(doors);
  File.WriteAllText("Library/StaffSubway/door-animation-source.txt",$"{data.source}\n{data.clip}\nSHA256 {data.sha256}\n85 tracks across five cars. Original LINEAR keyframes; 0–1.5s opens, reverse closes; open hold until O.");
 }
 [MenuItem("STAFF/Subway/Inspect Door Structure")]
 static void Inspect(){
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/STAFF_Subway_Reference.unity");bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity",OpenSceneMode.Additive);
  var lines=scene.GetRootGameObjects().Select(g=>$"ROOT {g.name}: pos={g.transform.position} scale={g.transform.localScale}").ToList();
  var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment");
  foreach(var r in env.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("SideDoor_")).Take(16))lines.Add($"{r.name} parent={r.transform.parent.name} center={r.bounds.center} size={r.bounds.size} localPos={r.transform.localPosition} children={r.transform.childCount}");
  File.WriteAllLines("Library/StaffSubway/door-structure.txt",lines);
  if(opened)EditorSceneManager.CloseScene(scene,true);
 }
 }
}
