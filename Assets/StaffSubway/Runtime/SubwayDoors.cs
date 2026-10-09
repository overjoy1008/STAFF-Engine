using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Staff.Subway {
 [DisallowMultipleComponent]
 public sealed class SubwayDoors : MonoBehaviour {
  [Serializable] public struct Leaf { public Transform transform; public Vector3 closedLocalPosition; public AnimationCurve x, y, z; }
  public Leaf[] leaves=Array.Empty<Leaf>();
  [Min(.1f)] public float duration=1.5f;
  public BoxCollider[] doorwayGates=Array.Empty<BoxCollider>();
  public bool DoorwayOccupied { get { var player=FindFirstObjectByType<Staff.Characters.AdventurePlayer>();if(!player)return false;var body=player.GetComponent<CharacterController>();if(!body)return false;foreach(var gate in doorwayGates){if(!gate||gate.name.EndsWith(" L"))continue;var bounds=new Bounds(gate.transform.TransformPoint(gate.center),Vector3.Scale(gate.size,gate.transform.lossyScale));bounds.Expand(.15f);if(bounds.Intersects(body.bounds))return true;}return false;} }
  public bool IsOpen {get;private set;}
  public float Progress {get;private set;}
  public bool InputLocked {get;set;}
  public void Close(){if(!DoorwayOccupied)IsOpen=false;}
  public void Toggle(){if(InputLocked||IsOpen&&DoorwayOccupied)return;IsOpen=!IsOpen;}
  public void HandleInput(bool accept){if(accept&&Keyboard.current!=null&&Keyboard.current.oKey.wasPressedThisFrame)Toggle();}
  void Start(){Advance(0);}
  void Update(){
   var player=FindFirstObjectByType<Staff.Characters.AdventurePlayer>();
   HandleInput(Application.isFocused&&player&&player.InputCaptured&&!player.InputBlocked);
   Advance(Time.deltaTime);
  }
  public void Advance(float deltaTime){
   if(!IsOpen&&Progress>0&&DoorwayOccupied)IsOpen=true;
   Progress=Mathf.MoveTowards(Progress,IsOpen?1:0,Mathf.Max(0,deltaTime)/Mathf.Max(.1f,duration));
   float time=Progress*duration;
   foreach(var leaf in leaves)if(leaf.transform){
    // The platform is on model side R; opposite side L stays closed.
    // Connection doors follow the same open/close cycle as platform doors.
    float sample=leaf.transform.name.StartsWith("SideDoor_R_")?time:leaf.transform.name.StartsWith("SideDoor_L_")?0:time;
    leaf.transform.localPosition=leaf.closedLocalPosition+new Vector3(leaf.x.Evaluate(sample),leaf.y.Evaluate(sample),leaf.z.Evaluate(sample));
   }
   foreach(var gate in doorwayGates)if(gate)gate.enabled=gate.name.EndsWith(" L")||Progress<.98f;
  }
  void OnDisable(){IsOpen=false;Progress=0;Advance(0);}
 }
}
