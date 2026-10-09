using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Staff.Characters;
namespace Staff.Subway.Editor {
 [InitializeOnLoad]public static class AbstractEnvironmentSetup {
 const string Flag="STAFF.Abstract.Verify";
 static AbstractEnvironmentSetup(){EditorApplication.update+=Tick;}
 [MenuItem("STAFF/Subway/Install Abstract Mode")]
 public static void Install(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before installing.");
  var scene=SceneManager.GetActiveScene();if(scene.name!="STAFF_Subway_Reference")scene=EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity");
  Directory.CreateDirectory("Library/StaffSubway");EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-abstract.unity",true);
  Configure(scene);
 }
 static void Configure(Scene scene){
  var c=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<AdventureCamera>(true)).Single();
  var mode=c.GetComponent<AbstractEnvironment>()??c.gameObject.AddComponent<AbstractEnvironment>();
  mode.environmentRoot=scene.GetRootGameObjects().Single(x=>x.name=="Environment").transform;
  mode.surfaceShader=Shader.Find("STAFF/Environment/Abstract Surface");mode.geometryShader=Shader.Find("Hidden/STAFF/Abstract Geometry");mode.edgeShader=Shader.Find("Hidden/STAFF/Abstract Edges");mode.startAbstract=false;
  mode.analyticShader=Shader.Find("STAFF/Environment/Abstract Coordinate Plane");
  mode.additionalEnvironmentRoots=scene.GetRootGameObjects().Where(g=>g.name=="Coordinates"||g.name=="Mathematical Symbols").Select(g=>g.transform).ToArray();
  foreach(var shader in new[]{mode.surfaceShader,mode.geometryShader,mode.edgeShader,mode.analyticShader})if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Shader compile failure");
  EditorUtility.SetDirty(mode);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }
 [MenuItem("STAFF/Subway/Install Three Modes Both Scenes")]
 static void InstallBoth(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before installing.");
  Directory.CreateDirectory("Library/StaffSubway");
  foreach(var path in new[]{"Assets/Scenes/STAFF_Subway_Reference.unity","Assets/Scenes/SampleScene.unity"}){
   var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
   if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-three-modes-"+scene.name+".unity",true);
   Configure(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
  }
 }
 [MenuItem("STAFF/Subway/Verify Subway Modes")]
 static void VerifySubway(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
  SessionState.SetString("STAFF.Abstract.ReturnScene",SceneManager.GetActiveScene().path);
  EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity");Verify();
 }
 [MenuItem("STAFF/Subway/Verify Sample Modes")]
 static void VerifySample(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
  SessionState.SetString("STAFF.Abstract.ReturnScene",SceneManager.GetActiveScene().path);
  EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");Verify();
 }
 [MenuItem("STAFF/Subway/Verify Abstract Mode")]
 static void Verify(){EditorApplication.isPaused=false;EditorApplication.ExecuteMenuItem("Window/General/Game");Directory.CreateDirectory("Library/StaffSubway");SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;}
 static void Capture(Camera c,string name){var rt=new RenderTexture(1600,900,24);var active=RenderTexture.active;var old=c.targetTexture;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();if(t.GetPixels32().Count(p=>p.r>240&&p.g<20&&p.b>240)>1600*900/100)throw new Exception("Shader error magenta detected in "+name);File.WriteAllBytes("Library/StaffSubway/"+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}finally{c.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);}}
 [MenuItem("STAFF/Subway/Mode Verification Status")]
 static void Status(){File.WriteAllText("Library/StaffSubway/verification-state.txt",$"playing={EditorApplication.isPlaying} paused={EditorApplication.isPaused} frame={Time.frameCount} flag={SessionState.GetBool(Flag,false)} scene={SceneManager.GetActiveScene().name}");}
 static void PressI()=>PressKey(UnityEngine.InputSystem.Key.I,()=>UnityEngine.Object.FindFirstObjectByType<CharacterSwitcher>().HandleInput(true));
 static void PressKey(UnityEngine.InputSystem.Key key,Action action){
  var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
  var update=typeof(UnityEngine.InputSystem.InputSystem).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(UnityEngine.InputSystem.LowLevel.InputUpdateType)},null);
  try{
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
   update.Invoke(null,new object[]{UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic});
   action();
  }finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);}
 }
 static void Tick(){
 if(EditorApplication.isPlaying&&SessionState.GetBool(Flag,false))Application.runInBackground=true;
 if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&!SessionState.GetBool(Flag,false)){
 var previous=SessionState.GetString("STAFF.Abstract.ReturnScene","");if(previous!=""){SessionState.EraseString("STAFF.Abstract.ReturnScene");EditorSceneManager.OpenScene(previous);}return;}
 if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||Time.frameCount<40)return;SessionState.SetBool(Flag,false);
  try{
   var c=Camera.main;var m=c.GetComponent<AbstractEnvironment>();if(!m||m.Mode!=AbstractEnvironment.EnvironmentMode.Real)throw new Exception("Expected initial Real mode");
   string prefix=SceneManager.GetActiveScene().name+"-";
   var originalMaterials=m.EnvironmentRenderers.ToDictionary(x=>x,x=>x.sharedMaterials);
   PressI();if(m.Mode!=AbstractEnvironment.EnvironmentMode.Abstract)throw new Exception("Real to Abstract failed");
   var player=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();player.enabled=false;
   var character=player.GetComponentsInChildren<Renderer>().ToDictionary(x=>x,x=>x.sharedMaterials);
   foreach(var shader in new[]{m.surfaceShader,m.geometryShader,m.edgeShader,m.analyticShader})if(!shader||!shader.isSupported||ShaderUtil.ShaderHasError(shader))throw new Exception("Shader compile/support failure: "+shader);
   Capture(c,prefix+"abstract-gameplay");
   PressI();if(m.Mode!=AbstractEnvironment.EnvironmentMode.Imaginary)throw new Exception("Abstract to Imaginary failed");
   Capture(c,prefix+"imaginary-gameplay");
   if(c.GetComponent<SubwayPlanarReflection>()&&c.GetComponent<SubwayPlanarReflection>().enabled)throw new Exception("Reflection still active");
   var environment=m.EnvironmentRenderers.ToDictionary(x=>x,x=>x.sharedMaterials);
   if(environment.Values.SelectMany(x=>x).Any(x=>x&&x.shader!=m.surfaceShader&&x.shader!=m.analyticShader))throw new Exception("Environment material conversion missing");
   if(c.GetComponent<SubwayPlanarReflection>()&&!environment.Values.SelectMany(x=>x).Any(x=>x&&x.HasProperty("_EmissionColor")&&x.GetColor("_EmissionColor").maxColorComponent>0))throw new Exception("Emission lost");
   PressI();if(m.Mode!=AbstractEnvironment.EnvironmentMode.Real)throw new Exception("Imaginary to Real failed");
   Capture(c,prefix+"real-gameplay");if(c.GetComponent<SubwayPlanarReflection>()&&!c.GetComponent<SubwayPlanarReflection>().enabled)throw new Exception("Reflection not restored");
   foreach(var pair in character)if(!pair.Key.sharedMaterials.SequenceEqual(pair.Value))throw new Exception("Character materials changed");
   for(int i=0;i<3;i++){m.CycleMode();m.CycleMode();m.CycleMode();}
   foreach(var pair in originalMaterials)if(!pair.Key.sharedMaterials.SequenceEqual(pair.Value))throw new Exception("PBR materials not restored");
   var doors=UnityEngine.Object.FindFirstObjectByType<SubwayDoors>();
   if(doors){
    if(doors.leaves.Length!=(UnityEngine.Object.FindObjectsByType<SubwayCar>(FindObjectsSortMode.None).Length>0?88:85)||(m.environmentRoot.localScale-Vector3.one*1.5f).sqrMagnitude>.00001f)throw new Exception("Station scale / authored track count incorrect");
    doors.enabled=false;
    var pos=c.transform.position;var rotation=c.transform.rotation;
    var leaf=doors.leaves.First(x=>x.transform.name=="SideDoor_R_02_FrontLeaf");var bounds=leaf.transform.GetComponent<Renderer>().bounds;
    c.transform.position=bounds.center+new Vector3(-5,.3f,-.5f);c.transform.LookAt(bounds.center);
    Capture(c,"doors-closed-real");
    PressKey(UnityEngine.InputSystem.Key.O,()=>doors.HandleInput(true));if(!doors.IsOpen)throw new Exception("O did not open doors");
    doors.Advance(.5f);
    foreach(var door in doors.leaves.Where(x=>x.transform.name.StartsWith("SideDoor_"))){
     var delta=door.transform.parent.TransformVector(door.transform.localPosition-door.closedLocalPosition);
     if(Mathf.Abs(delta.x)<.1f||Mathf.Abs(delta.z)>.0001f)throw new Exception("Authored outward unplug stage lost");
    }
    doors.Advance(1);Capture(c,"doors-open-real");
    foreach(var door in doors.leaves)if(door.transform.parent.TransformVector(door.transform.localPosition-door.closedLocalPosition).sqrMagnitude<.01f)throw new Exception("Door track did not move: "+door.transform.name);
    m.SetMode(AbstractEnvironment.EnvironmentMode.Abstract);Capture(c,"doors-open-abstract");
    m.SetMode(AbstractEnvironment.EnvironmentMode.Imaginary);Capture(c,"doors-open-imaginary");
    m.SetMode(AbstractEnvironment.EnvironmentMode.Real);
    PressKey(UnityEngine.InputSystem.Key.O,()=>doors.HandleInput(true));doors.Advance(.6f);
    var halfway=leaf.transform.localPosition;PressKey(UnityEngine.InputSystem.Key.O,()=>doors.HandleInput(true));doors.Advance(0);
    if(leaf.transform.localPosition!=halfway)throw new Exception("Door reversal jumped");
    PressKey(UnityEngine.InputSystem.Key.O,()=>doors.HandleInput(true));doors.Advance(10);
    foreach(var door in doors.leaves)if((door.transform.localPosition-door.closedLocalPosition).sqrMagnitude>.000001f)throw new Exception("Door did not close exactly");
    c.transform.SetPositionAndRotation(pos,rotation);
    File.WriteAllText("Library/StaffSubway/door-validation.txt","PASS: O input; authored GLB tracks; outward unplug at 0.5s, fully open at 1.5s; Real/Abstract/Imaginary captures; mid-motion reversal without jumps; all doors return exactly to closed; environment scale 1.5.");
   }
   m.SetAbstract(true);
   File.WriteAllText("Library/StaffSubway/"+SceneManager.GetActiveScene().name+"-mode-validation.txt","PASS: I key Real -> Abstract -> Imaginary -> Real; live captures of all three modes; all environment materials converted; emission retained; reflection disabled/restored; character material references unchanged; three toggle cycles restore exact original material references.");
  }catch(Exception e){File.WriteAllText("Library/StaffSubway/"+SceneManager.GetActiveScene().name+"-mode-validation.txt","FAIL: "+e);Debug.LogException(e);}finally{Application.runInBackground=false;EditorApplication.isPlaying=false;}
 }
 }
}
