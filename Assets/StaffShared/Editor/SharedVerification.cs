using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;using Staff.Characters;using Staff.Subway;using Object=UnityEngine.Object;
namespace Staff.Shared.Editor {
[InitializeOnLoad] public static class SharedVerification {
 const string Work="Library/StaffShared",Key="StaffShared.Check";static double deadline;
 static SharedVerification(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;}
 static void State(PlayModeStateChange s){if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode)deadline=EditorApplication.timeSinceStartup+4;if(s==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key+"Next",true);}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(SessionState.GetBool(Key,false)&&!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool(Key+"Next",false)){SessionState.SetBool(Key+"Next",false);Next();return;}
  if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Work+"/cityplay.request")){File.Delete(Work+"/cityplay.request");SessionState.SetBool(Key+"Next",false);SessionState.SetInt(Key+"Index",1);SessionState.SetBool(Key,true);Next();return;}
  if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Work+"/play.request")){File.Delete(Work+"/play.request");File.WriteAllText(Work+"/play-validation.txt","");try{Propagation();SessionState.SetInt(Key+"Index",-1);SessionState.SetBool(Key,true);Next();}catch(Exception e){Fail(e);}return;}
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||deadline==0||EditorApplication.timeSinceStartup<deadline)return;deadline=0;
  try{CheckPlay();}catch(Exception e){Fail(e);}finally{EditorApplication.isPlaying=false;}
 }
 static void Fail(Exception e){SessionState.SetBool(Key,false);File.AppendAllText(Work+"/play-validation.txt","FAIL "+e+"\n");Debug.LogException(e);}
 static void Next(){if(!SessionState.GetBool(Key,false))return;if(EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Key+"Next",true);return;}int index=SessionState.GetInt(Key+"Index",-1)+1;SessionState.SetInt(Key+"Index",index);if(index>=SharedMigration.Scenes.Length){SessionState.SetBool(Key,false);File.WriteAllText(Work+"/play-complete.txt","PASS: returned to Edit mode after all three scenes.");return;}EditorSceneManager.OpenScene(SharedMigration.Scenes[index],OpenSceneMode.Single);EditorApplication.isPlaying=true;}
 static void Propagation(){
  var root=PrefabUtility.LoadPrefabContents(SharedMigration.Gameplay);
  try{if(root.GetComponent<BoxCollider>())throw new Exception("Unexpected probe collider");root.AddComponent<BoxCollider>().enabled=false;PrefabUtility.SaveAsPrefabAsset(root,SharedMigration.Gameplay);}finally{PrefabUtility.UnloadPrefabContents(root);}
  try{foreach(var path in SharedMigration.Scenes){var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var g=SharedMigration.All<SharedGameplay>(s).Single();if(!g.GetComponent<BoxCollider>())throw new Exception("Prefab component did not propagate "+path);}}
  finally{root=PrefabUtility.LoadPrefabContents(SharedMigration.Gameplay);try{Object.DestroyImmediate(root.GetComponent<BoxCollider>());PrefabUtility.SaveAsPrefabAsset(root,SharedMigration.Gameplay);}finally{PrefabUtility.UnloadPrefabContents(root);}}
  File.AppendAllText(Work+"/play-validation.txt","PASS: adding a component to the shared gameplay prefab propagated to all three scenes; temporary probe removed from source.\n");
 }
 static void CheckPlay(){
  var s=SceneManager.GetActiveScene();var g=SharedMigration.All<SharedGameplay>(s).Single();var sky=SharedMigration.All<SharedSkyRig>(s).Single();
  if(!Camera.main||Camera.main!=g.cameraController.GetComponent<Camera>()||g.cameraController.Target!=g.player.transform||!g.player.Animator)throw new Exception("Runtime player-camera binding failed");
  if(!g.player.GetComponent<CharacterSwitcher>()||!g.player.GetComponent<AdventureDance>())throw new Exception("Character features missing");
  if(!sky.sky.atmosphere.Ready||SubwayTimeOfDay.ForCamera(Camera.main)!=sky.sky||sky.sky.cameras.Length!=0)throw new Exception("Automatic sky binding failed");
  if(!Camera.main.GetComponent<Skybox>().material)throw new Exception("Camera sky not bound");
  var train=SharedMigration.All<SubwayConsist>(s).FirstOrDefault();
  if(train&&s.name=="STAFF_City_Reference"&&train.CarCount!=3)throw new Exception("City must use the shared three-car consist");
  if(train&&s.name=="STAFF_Subway_Reference"){
   var cc=g.player.GetComponent<CharacterController>();var original=g.player.transform.position;var trainPosition=train.transform.position;var trainRotation=train.transform.rotation;var tr=train.GetComponent<SubwayTransit>();
   for(int n=2;n<=8;n++){train.CarCount=n;if(train.Cars.Length!=n)throw new Exception("Runtime car count failed");var d=train.GetComponent<SubwayDoors>();if(d.leaves.Any(l=>!l.transform)||d.doorwayGates.Any(x=>!x))throw new Exception("Runtime door binding failed");}
   train.transform.SetPositionAndRotation(new Vector3(31,5,-25),Quaternion.Euler(0,37,0));train.CarCount=8;train.Rebuild();
   var position=train.transform.TransformPoint(new Vector3(0,.3f,train.carSpacing*3));cc.enabled=false;g.player.transform.position=position;cc.enabled=true;Physics.SyncTransforms();
   tr.Depart();train.GetComponent<SubwayDoors>().Advance(2);tr.Advance(0);tr.Advance(8);
   var expected=position+train.transform.TransformVector(Vector3.forward*105);
   if(Vector3.Distance(g.player.transform.position,expected)>.02f)throw new Exception("Rotated 8-car rider carry failed "+g.player.transform.position+" expected "+expected);
   tr.Advance(8);if(tr.State!=SubwayTransit.Phase.Away)throw new Exception("Departure failed");tr.Arrive();tr.Advance(16);if(tr.State!=SubwayTransit.Phase.Docked||Mathf.Abs(tr.Offset)>.001f)throw new Exception("Arrival failed");
   train.transform.SetPositionAndRotation(trainPosition,trainRotation);train.CarCount=5;train.Rebuild();cc.enabled=false;g.player.transform.position=original;cc.enabled=true;
   File.AppendAllText(Work+"/play-validation.txt","PASS: runtime 2–8 cars; translated/rotated 8-car train carries rider in local forward direction; departure and arrival finish correctly.\n");
  }
  sky.sky.SetPreset(1);sky.sky.atmosphere.Tick(true);sky.sky.ApplySky();sky.ApplyLighting();Capture(Camera.main,Work+"/"+s.name+"-day.png");
  sky.sky.SetPreset(3);sky.sky.atmosphere.Tick(true);sky.sky.ApplySky();sky.ApplyLighting();Capture(Camera.main,Work+"/"+s.name+"-night.png");
  if(s.name=="STAFF_City_Reference"){
   var camera=Camera.main;var cp=camera.transform.position;var cr=camera.transform.rotation;
   camera.transform.position=new Vector3(0,45,0);camera.transform.rotation=Quaternion.LookRotation(CelestialMoon.Direction(sky.sky.moon,sky.sky.atmosphere));Capture(camera,Work+"/Sky-Moon-Stars-Clouds.png");
   sky.sky.SetPreset(0);sky.sky.ApplySky();sky.ApplyLighting();camera.transform.rotation=Quaternion.LookRotation(sky.sky.atmosphere.SunDirection);Capture(camera,Work+"/Sky-Sun-Flare-Rays.png");camera.transform.SetPositionAndRotation(cp,cr);
  }
  sky.sky.SetMode(sky.sky.ContinuousModeIndex);if(!sky.sky.atmosphere.animate||!sky.sky.atmosphere.useDayCycle)throw new Exception("Solar cycle failed");
  File.AppendAllText(Work+"/play-validation.txt","PASS "+s.name+": actual Play; shared character features/camera; live Nishita; day/night captures; continuous solar cycle.\n");
 }
 static void Capture(Camera c,string path){var rt=new RenderTexture(1280,720,24);var prev=RenderTexture.active;var old=c.targetTexture;rt.Create();try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);}finally{c.targetTexture=old;RenderTexture.active=prev;rt.Release();Object.DestroyImmediate(rt);}}
}
}
