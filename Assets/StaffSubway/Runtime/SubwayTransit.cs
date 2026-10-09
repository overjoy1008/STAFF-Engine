using UnityEngine;
using UnityEngine.InputSystem;
using Staff.Characters;
namespace Staff.Subway {
 [DefaultExecutionOrder(100)]
 public sealed class SubwayTransit : MonoBehaviour {
  public enum Phase { Docked, Closing, Departing, Away, Arriving }
  public Transform[] movingParts;
  public SubwayDoors doors;
  public Vector3[] dockPositions;
  public float travelDistance=420, travelSeconds=16;
  public Phase State {get;private set;}
  public float Offset {get;private set;}
  Vector3[] origins; float elapsed;
  void Awake(){Initialize();}
  public void Initialize(){
   if(origins!=null)return;
   origins=new Vector3[movingParts.Length];
   for(int i=0;i<origins.Length;i++)origins[i]=dockPositions!=null&&dockPositions.Length==movingParts.Length?dockPositions[i]:movingParts[i].position;
  }
  public void ResetToDock(){Initialize();SetVisible(true);SetOffset(0,false);State=Phase.Docked;doors.InputLocked=false;}
  bool OnBoard(AdventurePlayer p){
   if(!p)return false;var v=p.transform.position;
   return v.x>5.8f&&v.x<9.5f&&v.z>Offset-18&&v.z<Offset+134&&v.y<5&&v.y>-.5f;
  }
  public void Arrive(){
   Initialize();if(State!=Phase.Away&&State!=Phase.Docked)return;
   if(doors.DoorwayOccupied)return;
   var p=FindFirstObjectByType<AdventurePlayer>();if(OnBoard(p))p.Respawn();
   doors.Close();doors.Advance(10);doors.InputLocked=true;
   SetVisible(true);SetOffset(-travelDistance,false);elapsed=0;State=Phase.Arriving;
  }
  public void Depart(){if(State!=Phase.Docked)return;doors.InputLocked=true;State=Phase.Closing;}
  void SetVisible(bool active){foreach(var t in movingParts)t.gameObject.SetActive(active);}
  void SetOffset(float value,bool carry){
   var p=FindFirstObjectByType<AdventurePlayer>();bool rider=carry&&OnBoard(p);
   float delta=value-Offset;Offset=value;
   for(int i=0;i<movingParts.Length;i++)movingParts[i].position=origins[i]+Vector3.forward*value;
   if(rider){var cc=p.GetComponent<CharacterController>();bool was=cc.enabled;cc.enabled=false;p.transform.position+=Vector3.forward*delta;cc.enabled=was;}
   Physics.SyncTransforms();
  }
  void Update(){
   var p=FindFirstObjectByType<AdventurePlayer>();var k=Keyboard.current;
   if(Application.isFocused&&p&&p.InputCaptured&&!p.InputBlocked&&k!=null){
    if(k.leftBracketKey.wasPressedThisFrame)Arrive();
    if(k.rightBracketKey.wasPressedThisFrame)Depart();
   }
   Advance(Time.deltaTime);
  }
  public void Advance(float dt){
   Initialize();
   if(State==Phase.Closing){doors.Close();if(!doors.IsOpen&&doors.Progress<=0){State=Phase.Departing;elapsed=0;}return;}
   if(State!=Phase.Arriving&&State!=Phase.Departing)return;
   elapsed+=Mathf.Max(0,dt);float t=Mathf.Clamp01(elapsed/Mathf.Max(1,travelSeconds));
   // Arrival brakes to a stop; departure accelerates out of the station.
   SetOffset(State==Phase.Arriving?-travelDistance*Mathf.Pow(1-t,2):travelDistance*t*t,true);
   if(t<1)return;
   if(State==Phase.Arriving){State=Phase.Docked;doors.InputLocked=false;}
   else{var p=FindFirstObjectByType<AdventurePlayer>();if(OnBoard(p))p.Respawn();SetVisible(false);State=Phase.Away;}
  }
 }
}
