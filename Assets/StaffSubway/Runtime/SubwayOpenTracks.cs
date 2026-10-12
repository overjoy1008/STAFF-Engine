using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Staff.Subway {
 [ExecuteAlways,DisallowMultipleComponent]
 public sealed class SubwayOpenTracks:MonoBehaviour {
  [Min(5)] public float extensionMetres=60;
  GameObject generated;
  readonly Dictionary<Collider,bool> saved=new Dictionary<Collider,bool>();
  void OnEnable(){Build();}
  void OnDisable(){Clear();}
  void Clear(){foreach(var entry in saved)if(entry.Key)entry.Key.enabled=entry.Value;saved.Clear();if(generated){generated.SetActive(false);if(Application.isPlaying)Destroy(generated);else DestroyImmediate(generated);}generated=null;}
  [ContextMenu("Rebuild open tracks")] public void Build(){
   Clear();var all=GetComponentsInChildren<Transform>(true);var bed=all.FirstOrDefault(t=>t.name=="Track trench");var rails=all.Where(t=>t.name=="Rail").ToArray();var slab=all.FirstOrDefault(t=>t.name.StartsWith("Platform slab"));var sleeper=all.FirstOrDefault(t=>t.name.StartsWith("Track sleeper"));
   if(!bed||!slab||!sleeper||rails.Length!=2)return;
   generated=new GameObject("Open track extensions (generated)");generated.hideFlags=HideFlags.DontSave;generated.transform.SetParent(bed.parent,false);
   float length=Mathf.Max(5,extensionMetres)/Mathf.Abs(bed.parent.lossyScale.z);float lo=bed.localPosition.z-bed.localScale.z*.5f,hi=bed.localPosition.z+bed.localScale.z*.5f;
   foreach(var t in all)if(t.name=="Platform track edge"||t.name=="Near platform limit"||t.name=="Far platform limit")foreach(var c in t.GetComponents<Collider>()){saved[c]=c.enabled;c.enabled=false;}
   foreach(int side in new[]{-1,1}){
    float edge=side<0?lo:hi;float z=edge+side*length*.5f;
    Add(bed,"Extended track bed "+side,new Vector3(bed.localPosition.x,bed.localPosition.y,z),new Vector3(bed.localScale.x,bed.localScale.y,length));
    foreach(var rail in rails)Add(rail,"Extended rail "+side,new Vector3(rail.localPosition.x,rail.localPosition.y,z),new Vector3(rail.localScale.x,rail.localScale.y,length));
    for(float d=.6f;d<length;d+=1.2f)Add(sleeper,"Outdoor sleeper",new Vector3(sleeper.localPosition.x,sleeper.localPosition.y,edge+side*d),sleeper.localScale);
    // Open-air landing beyond platform doors, then a walkable slope down to the bed.
    float landingZ=edge+side*2;
    Add(slab,"Track access landing "+side,new Vector3(slab.localPosition.x,slab.localPosition.y,landingZ),new Vector3(slab.localScale.x,slab.localScale.y,4));
    float start=slab.localPosition.x+slab.localScale.x*.5f,end=bed.localPosition.x+.25f;
    float top=slab.localPosition.y+slab.localScale.y*.5f,bottom=bed.localPosition.y+bed.localScale.y*.5f;
    float dx=end-start,dy=bottom-top;
    var ramp=Add(bed,"Track access ramp "+side,new Vector3((start+end)*.5f,(top+bottom)*.5f-.06f,landingZ),new Vector3(Mathf.Sqrt(dx*dx+dy*dy)+.15f,.12f,2.4f));ramp.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(dy,dx)*Mathf.Rad2Deg);
   }
  }
  Transform Add(Transform source,string label,Vector3 position,Vector3 size){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=label;g.hideFlags=HideFlags.DontSave;g.layer=source.gameObject.layer;g.transform.SetParent(generated.transform,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;return g.transform;}
 }
}
