using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Staff.Characters;
namespace Staff.Subway.Editor {
 [InitializeOnLoad] public static class SubwayBoardingVerification {
  const string Flag="STAFF.Boarding.Verify";static SubwayBoardingVerification(){EditorApplication.update+=Tick;}
  [MenuItem("STAFF/Subway/Verify Modular Boarding")]
  static void Run(){if(EditorApplication.isPlaying){SessionState.SetBool(Flag,true);EditorApplication.isPaused=false;return;}SessionState.SetString(Flag+"Scene",SceneManager.GetActiveScene().path);EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity");SessionState.SetBool(Flag,true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static void Capture(Camera c,string name){var rt=new RenderTexture(1600,900,24);var old=c.targetTexture;var active=RenderTexture.active;Texture2D t=null;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes("Library/StaffSubway/"+name+".png",t.EncodeToPNG());}finally{c.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);if(t)UnityEngine.Object.DestroyImmediate(t);}}
  static void Tick(){
   if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&!SessionState.GetBool(Flag,false)){var path=SessionState.GetString(Flag+"Scene","");if(path!=""){SessionState.EraseString(Flag+"Scene");EditorSceneManager.OpenScene(path);}return;}
   if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying)return;Application.runInBackground=true;var runningTransit=UnityEngine.Object.FindFirstObjectByType<SubwayTransit>();if(runningTransit)runningTransit.enabled=false;if(Time.frameCount<40)return;SessionState.SetBool(Flag,false);
   try{
    var resetTransit=UnityEngine.Object.FindFirstObjectByType<SubwayTransit>();if(resetTransit){resetTransit.enabled=false;resetTransit.ResetToDock();}
    var doors=UnityEngine.Object.FindFirstObjectByType<SubwayDoors>();doors.enabled=false;
    var p=UnityEngine.Object.FindFirstObjectByType<AdventurePlayer>();p.enabled=false;var cc=p.GetComponent<CharacterController>();var original=p.transform.position;
    var cars=UnityEngine.Object.FindObjectsByType<SubwayCar>(FindObjectsSortMode.None);Check(cars.Count(c=>c.module=="Front")==1&&cars.Count(c=>c.module=="Middle")==3&&cars.Count(c=>c.module=="Rear")==1,"Consist module counts");Check(doors.leaves.Length==88,"Authored animation track count");
    void Place(float x,float z){cc.enabled=false;p.transform.position=new Vector3(x,.22f,z);cc.enabled=true;Physics.SyncTransforms();for(int i=0;i<12;i++)cc.Move(Vector3.down*.04f);}
    void Walk(float dx,int steps){for(int i=0;i<steps;i++){cc.Move(new Vector3(dx,-.035f,0));Physics.SyncTransforms();}}
    var gates=doors.doorwayGates.Where(g=>g.name.EndsWith(" R")).OrderBy(g=>g.transform.position.z).ToArray();Check(gates.Length==20,"20 platform-side boarding portals");
    foreach(var gate in gates){
     float z=gate.transform.position.z;
     Place(4.35f,z);if(doors.IsOpen)doors.Toggle();doors.Advance(10);Walk(.10f,35);Check(p.transform.position.x<gate.transform.position.x-.1f,"Closed door was passable: "+gate.name);
     // Move away from the gate before opening, then cross it with actual CharacterController.Move.
     Place(4.35f,z);doors.Toggle();doors.Advance(10);Physics.SyncTransforms();Walk(.10f,35);Check(p.transform.position.x>7.1f,"Boarding blocked: "+gate.name+" position="+p.transform.position+" gate="+gate.transform.position+" open="+doors.IsOpen+" progress="+doors.Progress);Check(p.transform.position.y>-.05f,"Cabin floor missing");
     doors.Toggle();doors.Advance(10);Walk(-.10f,35);Check(p.transform.position.x>gate.transform.position.x+.1f,"Closed door permitted exit");
     Place(7.5f,z);doors.Toggle();doors.Advance(10);Walk(-.10f,35);Check(p.transform.position.x<4.8f,"Exit blocked: "+gate.name);
     Place(gate.transform.position.x,z);Check(doors.DoorwayOccupied,"Doorway occupancy missed");doors.Toggle();Check(doors.IsOpen,"Door closed on player");
     Place(4.35f,z);doors.Toggle();doors.Advance(10);
    }
    Bounds CarBounds(SubwayCar car){var rs=car.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    var ordered=cars.OrderBy(c=>CarBounds(c).center.z).ToArray();
    for(int i=0;i<4;i++){
     float seam=CarBounds(ordered[i]).max.z;Check(Mathf.Abs(CarBounds(ordered[i+1]).min.z-seam-.025f)<.002f,"Gangway seam alignment");
     Place(7.62f,seam-2);if(!doors.IsOpen)doors.Toggle();doors.Advance(10);Physics.SyncTransforms();
     for(int step=0;step<50;step++){cc.Move(new Vector3(0,-.035f,.10f));Physics.SyncTransforms();}
     Check(p.transform.position.z>seam+2&&p.transform.position.y>-.05f,"Gangway crossing blocked / floor gap at "+i+": "+p.transform.position);
    }
    foreach(var leaf in doors.leaves.Where(l=>l.transform.name.StartsWith("SideDoor_L_")))Check(leaf.transform.localPosition==leaf.closedLocalPosition,"Opposite-side door opened");
    foreach(var gate in doors.doorwayGates.Where(g=>g.name.EndsWith(" L")))Check(gate.enabled,"Opposite-side collision disabled");
    foreach(var car in cars)foreach(var mat in car.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct())Check(mat.shader.name==(mat.name.Contains("Glass")?"STAFF/Subway/Double Sided Glass":"STAFF/Subway/Double Sided PBR"),"One-sided car material: "+mat.name);
    for(int i=0;i<4;i++){
     float z=(CarBounds(ordered[i]).max.z+CarBounds(ordered[i+1]).min.z)/2;
     Place(7.62f,z);Walk(.10f,30);Check(p.transform.position.x<8.5f&&p.transform.position.y>-.05f,"Escaped gangway right side "+i);
     Place(7.62f,z);Walk(-.10f,30);Check(p.transform.position.x>6.7f&&p.transform.position.y>-.05f,"Escaped gangway left side "+i);
    }
    var spawn=(Vector3)typeof(AdventurePlayer).GetField("spawn",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(p);
    var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
    try{
     UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.R));
     typeof(UnityEngine.InputSystem.InputSystem).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(UnityEngine.InputSystem.LowLevel.InputUpdateType)},null).Invoke(null,new object[]{UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic});
     Check(p.HandleResetInput(true)&&Vector3.Distance(p.transform.position,spawn)<.001f&&p.VerticalVelocity==0&&!p.IsDashing,"R reset failed");
    }finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);}
    var camera=Camera.main;var mode=camera.GetComponent<AbstractEnvironment>();
    Place(7.6f,gates[8].transform.position.z);if(!doors.IsOpen)doors.Toggle();doors.Advance(10);
    camera.transform.position=p.transform.position+new Vector3(.1f,1.55f,-4);camera.transform.rotation=Quaternion.Euler(3,0,0);
    Capture(camera,"modular-cabin-real");mode.SetMode(AbstractEnvironment.EnvironmentMode.Abstract);Capture(camera,"modular-cabin-abstract");mode.SetMode(AbstractEnvironment.EnvironmentMode.Imaginary);Capture(camera,"modular-cabin-imaginary");mode.SetMode(AbstractEnvironment.EnvironmentMode.Real);
    camera.transform.position=new Vector3(1.5f,2.4f,-23);camera.transform.LookAt(new Vector3(7.6f,1.8f,-7));Capture(camera,"modular-consist-overview");
    camera.transform.position=new Vector3(2,2.4f,142);camera.transform.LookAt(new Vector3(7.6f,1.8f,120));Capture(camera,"modular-front-overview");
    float joint=CarBounds(ordered[1]).max.z;camera.transform.position=new Vector3(2.6f,2.3f,joint-3);camera.transform.LookAt(new Vector3(7.6f,1.8f,joint));Capture(camera,"modular-gangway-joint");
    Place(4.35f,gates[8].transform.position.z);
    if(doors.IsOpen)doors.Toggle();doors.Advance(10);
    foreach(var leaf in doors.leaves)Check(Vector3.Distance(leaf.transform.localPosition,leaf.closedLocalPosition)<.001f,"Door failed to close: "+leaf.transform.name);
    foreach(var gate in doors.doorwayGates)Check(gate.enabled,"Closed gate disabled");
    var transit=UnityEngine.Object.FindFirstObjectByType<SubwayTransit>();transit.enabled=false;transit.Initialize();
    var fixedEdge=GameObject.Find("Platform edge barrier").transform;var edgePosition=fixedEdge.position;
    doors.Toggle();doors.Advance(10);transit.Depart();transit.Advance(.1f);
    Check(transit.State==SubwayTransit.Phase.Closing,"Departure must wait for doors");
    doors.Advance(10);transit.Advance(.1f);transit.Advance(transit.travelSeconds);
    Check(transit.State==SubwayTransit.Phase.Away&&transit.movingParts.All(t=>!t.gameObject.activeSelf),"Departed train not hidden");
    Check(fixedEdge.position==edgePosition,"Platform moved with train");
    transit.Arrive();Check(transit.Offset==-transit.travelDistance,"Arrival starting offset");
    transit.Advance(transit.travelSeconds/2);Check(transit.Offset<0&&transit.Offset>-transit.travelDistance,"Arrival motion missing");
    transit.Advance(transit.travelSeconds/2);
    Check(transit.State==SubwayTransit.Phase.Docked&&Mathf.Abs(transit.Offset)<.001f&&!doors.InputLocked,"Arrival not docked");
    Place(7.62f,gates[8].transform.position.z);var riderPosition=p.transform.position;
    transit.Depart();transit.Advance(.1f);transit.Advance(1);
    Check(Mathf.Abs(p.transform.position.z-riderPosition.z-transit.Offset)<.001f,"Passenger not carried");
    transit.Advance(transit.travelSeconds);Check(Vector3.Distance(p.transform.position,spawn)<.001f,"Departed rider not returned safely");
    transit.Arrive();transit.Advance(transit.travelSeconds);
    var probe=UnityEngine.Object.FindFirstObjectByType<ReflectionProbe>();
    Check(probe.customBakedTexture!=null,"Baked reflection missing");
    foreach(var car in cars)Check(probe.bounds.Intersects(CarBounds(car)),"Car outside reflection bounds");
    camera.transform.position=new Vector3(1.5f,2.4f,30);camera.transform.LookAt(new Vector3(7.6f,2,90));Capture(camera,"rebaked-train-overview");
    File.WriteAllText("Library/StaffSubway/transit-validation.txt","PASS: all 88 leaves close; all gates close; departure waits for doors; train hides; platform stays fixed; arrival returns to dock; passenger follows and safely respawns on despawn; baked reflection covers all cars.");
    p.transform.position=original;
    File.WriteAllText("Library/StaffSubway/boarding-validation.txt","PASS: front 1 / middle 3 / rear 1; 88 authored door tracks; all 20 platform doorways tested with actual player CharacterController: closed blocks entry/exit, open permits boarding and exit, cabin floor supports player; occupied doorways prevent closing; four aligned 0.025m gangway seams crossed with actual controller without falling; R input resets position/velocity; all four gangways block lateral escape; opposite-side doors remain closed with collision; opaque and glass materials use double-sided shaders; cabin renders in Real/Abstract/Imaginary.");
   }catch(Exception e){File.WriteAllText("Library/StaffSubway/boarding-validation.txt","FAIL: "+e);Debug.LogException(e);}finally{Application.runInBackground=false;EditorApplication.isPlaying=false;}
  }
 }
}
