using System.Collections.Generic;
using UnityEngine;
namespace Staff.Subway {
 // Volumetric proxies only: never creates Lights or modifies authored emission.
 public sealed class LocalGodraysSources {
  public struct Source { public Vector3 position; public float range; public Color color; public Object owner; public bool train,trainEnd; }
  struct Emitter { public Renderer renderer; public Material material; public Transform car; public Vector3 carCenter; public Bounds localBounds; }
  readonly List<Emitter> emitters=new List<Emitter>(); readonly List<Source> candidates=new List<Source>();
  public Renderer[] Renderers {get;private set;} double nextScan;
  public void Refresh(){
   if(Renderers!=null&&Time.realtimeSinceStartupAsDouble<nextScan)return;
   nextScan=Time.realtimeSinceStartupAsDouble+1;Renderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);emitters.Clear();
   var centers=new Dictionary<SubwayCar,Vector3>();
   foreach(var r in Renderers){
    var car=r.GetComponentInParent<SubwayCar>();if(!car)continue;
    var filter=r.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
    var materials=r.sharedMaterials;var mesh=filter.sharedMesh;
    for(int i=0;i<materials.Length&&i<mesh.subMeshCount;i++){
     var m=materials[i];if(!m)continue;string name=m.name.Replace(" (Instance)","");
     if(name!="Body_Lamp")continue;
    if(!centers.TryGetValue(car,out var center)){
     var bounds=r.bounds;foreach(var part in car.GetComponentsInChildren<Renderer>())bounds.Encapsulate(part.bounds);
     center=car.transform.InverseTransformPoint(bounds.center);centers.Add(car,center);
    }

     emitters.Add(new Emitter{renderer=r,material=m,car=car.transform,carCenter=center,localBounds=mesh.GetSubMesh(i).bounds});
    }
   }
  }
  public void Select(Camera camera,SolarEffects.Settings settings,List<Source> result){
   Refresh();candidates.Clear();result.Clear();var planes=GeometryUtility.CalculateFrustumPlanes(camera);var eye=camera.transform.position;
   if(settings.trainEmissionRays)foreach(var e in emitters){
    var r=e.renderer;if(!r||!e.car||!r.enabled||!r.gameObject.activeInHierarchy||(camera.cullingMask&(1<<r.gameObject.layer))==0||!e.material)continue;
    // Respect disabled/black emission. Atlas colors are art-directed independently below.
    if(!e.material.IsKeywordEnabled("_EMISSION")||(e.material.HasProperty("_EmissionColor")&&e.material.GetColor("_EmissionColor").maxColorComponent<=0))continue;
    var p=r.transform.TransformPoint(e.localBounds.center);var center=e.car.TransformPoint(e.carCenter);
    Vector3 direction=e.car.forward*Mathf.Sign(Vector3.Dot(p-center,e.car.forward));
    p+=direction*.08f;
    float emission=e.material.HasProperty("_EmissionColor")?Mathf.Clamp(e.material.GetColor("_EmissionColor").maxColorComponent/1.2f,0,4):1;
    var color=settings.trainLampColor;
    Add(new Source{position=p,range=settings.trainLampRange,color=color.linear*settings.trainRayIntensity*emission*settings.trainEndRayStrength,owner=r,train=true,trainEnd=true},planes,eye);
   }
   candidates.Sort((a,b)=>Score(a,eye).CompareTo(Score(b,eye)));
   // Only front/rear lamp submeshes may enter the local shadow budget.
   int budget=Mathf.Clamp(settings.localLightBudget,1,4);
   foreach(var c in candidates){if(result.Count>=budget)break;if(!result.Exists(x=>x.owner==c.owner))result.Add(c);}
  }
  void Add(Source s,Plane[] planes,Vector3 eye){
   if(s.color.maxColorComponent<=0)return;
   float distance=Vector3.Distance(eye,s.position);if(distance>s.range+24||!GeometryUtility.TestPlanesAABB(planes,new Bounds(s.position,Vector3.one*s.range*2)))return;
   s.color*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(s.range+16,s.range+24,distance));candidates.Add(s);
  }
  static float Score(Source s,Vector3 eye)=>Vector3.Distance(s.position,eye)/Mathf.Sqrt(s.range);
 }
}
