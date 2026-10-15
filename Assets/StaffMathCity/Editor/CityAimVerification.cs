using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using Staff.Characters;using Staff.Shared;using Staff.Traversal;
namespace Staff.City.Editor {
[InitializeOnLoad] public static class CityAimVerification {
 const string Work="Library/CityScaleMatching",Key="STAFF.CityAimVerification";static double began;static bool checking;
 static CityAimVerification(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(File.Exists(Work+"/aim-test.request")&&!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete(Work+"/aim-test.request");File.WriteAllText(Work+"/aim-test.txt","");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;}
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||checking)return;
  if(began==0){began=EditorApplication.timeSinceStartup;return;}if(EditorApplication.timeSinceStartup-began<4)return;
  checking=true;try{Check();}catch(Exception e){File.AppendAllText(Work+"/aim-test.txt","FAIL "+e);Debug.LogException(e);}finally{SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
 }
 static void Assert(bool condition,string text){if(!condition)throw new Exception(text);File.AppendAllText(Work+"/aim-test.txt","PASS "+text+"\n");}
 static void Check(){
  var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();var a=p.GetComponent<TangentTraversal>();var cam=Camera.main;var view=cam.GetComponent<AdventureCamera>();var scene=p.gameObject.scene;var env=scene.GetRootGameObjects().Single(x=>x.name=="Environment").transform;
  Assert(Mathf.Abs(env.lossyScale.x-1.3f)<.0001f,"City environment scale 1.3");var train=env.GetComponentInChildren<SubwayConsist>();Assert(Vector3.Distance(train.transform.lossyScale,Vector3.one)<.0001f,"Train matches full subway size");Assert(Mathf.Abs(env.Find("Infinity Metro - elevated railway").lossyScale.x-1/.55f)<.0001f,"Elevated lane scale 1.81818");Assert(Vector3.Distance(p.transform.lossyScale,Vector3.one)<.0001f,"Shared character scale unchanged");
  a.Cancel();var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=new Vector3(0,.4f,23.4f);cc.enabled=true;Physics.SyncTransforms();
  var west=env.GetComponentsInChildren<TangentSurface>().Single(x=>x.name=="West Apartments").GetComponentsInChildren<Collider>().First(c=>c.name=="Main plaster mass").bounds;
  var target=new Vector3(west.max.x,9.1f,23.4f);cam.transform.position=p.transform.position+new Vector3(0,2,-3);cam.transform.LookAt(target);
  var frozenPos=cam.transform.position;var frozenRot=cam.transform.rotation;float frozenFov=cam.fieldOfView;
  Assert(a.BeginScreenAim(cam),"Q aim can begin");var initial=a.AimScreenPosition;a.MoveScreenAim(new Vector2(100,50));Assert(Vector2.Distance(a.AimScreenPosition,initial)>100,"Mouse moves reticle independently");view.AddLook(new Vector2(400,300),Vector2.one,120,true,.016f);view.Simulate(.016f);Assert(Vector3.Distance(cam.transform.position,frozenPos)<.0001f&&Quaternion.Angle(cam.transform.rotation,frozenRot)<.001f&&Mathf.Abs(cam.fieldOfView-frozenFov)<.001f,"Camera position rotation FOV frozen during mouse aim");
  a.MoveScreenAim(new Vector2(100000,100000));Assert(a.AimScreenPosition.x<=cam.pixelRect.xMax&&a.AimScreenPosition.y<=cam.pixelRect.yMax,"Reticle clamped inside viewport");a.MoveScreenAim((Vector2)cam.WorldToScreenPoint(target)-a.AimScreenPosition);Assert(a.HasTarget,"Screen-space reticle ray hits building");Assert(a.EndScreenAim()&&!a.IsScreenAiming,"Release Q launches and releases camera lock");
  for(int i=0;i<150;i++){p.Simulate(default,.016f);a.SendMessage("LateUpdate");}Assert(a.State==TangentTraversal.Phase.Wall,"Wall arrival with split city scale; state="+a.State+" position="+p.transform.position+" target="+a.TargetPoint);
  var east=env.GetComponentsInChildren<TangentSurface>().Single(x=>x.name=="East Apartments").GetComponentsInChildren<Collider>().First(c=>c.name=="Main plaster mass").bounds;target=new Vector3(east.min.x,10.4f,23.4f);cam.transform.position=p.transform.position+new Vector3(1,1,0);cam.transform.LookAt(target);Assert(a.BeginScreenAim(cam),"Can aim again while on wall");a.MoveScreenAim(Vector2.zero);Assert(a.EndScreenAim(),"Wall reticle launches second hop");for(int i=0;i<100;i++){p.Simulate(default,.016f);a.SendMessage("LateUpdate");}Assert(a.CompletedHops==2,"Continuous wall-to-wall jump remains functional");
  a.Cancel();Assert(a.BeginScreenAim(cam),"Aim can restart");a.Cancel();Assert(!a.IsScreenAiming,"Cancel releases aim lock");p.Respawn();view.SetFirstPerson(true);Assert(a.BeginScreenAim(cam),"First-person Q aim begins");frozenPos=cam.transform.position;frozenRot=cam.transform.rotation;a.MoveScreenAim(new Vector2(-80,30));view.AddLook(new Vector2(50,100),Vector2.zero,0,false,.016f);view.Simulate(.016f);Assert(Vector3.Distance(frozenPos,cam.transform.position)<.0001f&&Quaternion.Angle(frozenRot,cam.transform.rotation)<.001f,"First-person camera also stays fixed");a.Cancel();view.SetFirstPerson(false);p.Respawn();
 }
}
}
