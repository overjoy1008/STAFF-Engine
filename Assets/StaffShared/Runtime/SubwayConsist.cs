using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Staff.Subway;
namespace Staff.Shared {
[ExecuteAlways,DefaultExecutionOrder(-500),DisallowMultipleComponent,RequireComponent(typeof(SubwayDoors),typeof(SubwayTransit))]
public sealed class SubwayConsist:MonoBehaviour {
 [SerializeField,Range(2,8)] int carCount=5;
 public GameObject rearCar,middleCar,frontCar;
 [Min(1)] public float carSpacing=30.325f;
 public Material gangwayMaterial;
 [SerializeField,HideInInspector] Transform generated;
 int builtCount;GameObject lastRear,lastMiddle,lastFront;
 float lastSpacing;
 public int CarCount {get=>Mathf.Clamp(carCount,2,8);set{carCount=Mathf.Clamp(value,2,8);Rebuild();}}
 public SubwayDoorModule[] Cars=>generated?generated.GetComponentsInChildren<SubwayDoorModule>(true):Array.Empty<SubwayDoorModule>();
 void OnEnable(){Rebuild();}
 void OnValidate(){carCount=Mathf.Clamp(carCount,2,8);}
 void Update(){if(CarCount!=builtCount||lastRear!=rearCar||lastMiddle!=middleCar||lastFront!=frontCar||lastSpacing!=carSpacing)Rebuild();}
 void OnDisable(){Clear();}
 void Clear(){if(!generated)return;generated.gameObject.SetActive(false);if(Application.isPlaying)Destroy(generated.gameObject);else DestroyImmediate(generated.gameObject);generated=null;}
 [ContextMenu("Rebuild car count")]
 public void Rebuild(){
  if(!isActiveAndEnabled||!rearCar||!middleCar||!frontCar)return;
  var transit=GetComponent<SubwayTransit>();transit.enabled=false;Clear();
  var root=new GameObject("Cars (generated from shared modules)");root.hideFlags=HideFlags.DontSave;root.transform.SetParent(transform,false);generated=root.transform;
  var leaves=new List<SubwayDoors.Leaf>();var gates=new List<BoxCollider>();var cars=new List<SubwayDoorModule>();
  for(int i=0;i<CarCount;i++){
   var prefab=i==0?rearCar:i==CarCount-1?frontCar:middleCar;
   var car=Instantiate(prefab,generated);car.name=$"Car {i+1} - "+(i==0?"Rear":i==CarCount-1?"Front":"Middle");car.transform.localPosition=Vector3.forward*(i*carSpacing);car.transform.localRotation=Quaternion.identity;car.SetActive(true);
   foreach(var t in car.GetComponentsInChildren<Transform>(true))t.gameObject.hideFlags=HideFlags.DontSave;
   var module=car.GetComponent<SubwayDoorModule>();cars.Add(module);leaves.AddRange(module.leaves);gates.AddRange(module.gates);
  }
  var body=cars[0].localBodyBounds;
  for(int i=0;i<cars.Count;i++){
   var b=cars[i].localBodyBounds;b.center+=Vector3.forward*(i*carSpacing);body.Encapsulate(b);
   if(i==0)continue;
   float a=(i-1)*carSpacing+cars[i-1].localBodyBounds.max.z-.7f,z=i*carSpacing+cars[i].localBodyBounds.min.z+.7f;
   var bridge=GameObject.CreatePrimitive(PrimitiveType.Cube);bridge.name="Gangway floor bridge";bridge.hideFlags=HideFlags.DontSave;bridge.layer=30;bridge.transform.SetParent(generated,false);bridge.transform.localPosition=new Vector3(0,.065f,(a+z)/2);bridge.transform.localScale=new Vector3(1.25f,.17f,Mathf.Max(.1f,z-a));bridge.GetComponent<Renderer>().sharedMaterial=gangwayMaterial;
  }
  var doors=GetComponent<SubwayDoors>();doors.leaves=leaves.ToArray();doors.doorwayGates=gates.ToArray();doors.ResetClosed();
  transit.doors=doors;transit.motionSpace=transform;transit.movingParts=new[]{generated};transit.dockPositions=new[]{generated.position};body.center=new Vector3(body.center.x,1.7f,body.center.z);body.size=new Vector3(body.size.x,3.4f,body.size.z);transit.boardingBounds=body;transit.Reinitialize();transit.enabled=true;
  builtCount=CarCount;lastRear=rearCar;lastMiddle=middleCar;lastFront=frontCar;lastSpacing=carSpacing;
 }
}
}
