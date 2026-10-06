using UnityEngine;
using UnityEngine.InputSystem;
namespace Staff.Subway {
 [DisallowMultipleComponent] public sealed class SubwayFlyCamera : MonoBehaviour {
  public float speed=4f;public Vector3 referencePosition;public Vector3 referenceEuler;
  float yaw,pitch;
  void OnEnable(){yaw=transform.eulerAngles.y;pitch=transform.eulerAngles.x;}
  void Update(){
   var k=Keyboard.current;var m=Mouse.current;if(k==null||m==null)return;
   if(k.rKey.wasPressedThisFrame){transform.SetPositionAndRotation(referencePosition,Quaternion.Euler(referenceEuler));yaw=referenceEuler.y;pitch=referenceEuler.x;}
   if(!m.rightButton.isPressed){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;return;}
   Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
   var d=m.delta.ReadValue();yaw+=d.x*.12f;pitch=Mathf.Clamp(pitch-d.y*.12f,-85,85);transform.rotation=Quaternion.Euler(pitch,yaw,0);
   Vector3 v=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.eKey.isPressed?1:0)-(k.qKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
   transform.position+=transform.TransformDirection(v.normalized)*speed*(k.leftShiftKey.isPressed?3:1)*Time.deltaTime;
  }
  void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 }
}
