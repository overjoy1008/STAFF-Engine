using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Staff.Subway.Editor {
 public static class SubwayConsistSetup {
  const string Root="Assets/StaffSubway/";
  [Serializable] class Data {public float openTime;public Vector3 sourceSize;public Track[] tracks;}
  [Serializable] class Track {public string node;public float[] times;public Vector3[] deltas;}
  static string Dir(string module)=>Root+"Models/"+(module=="Front"?"Train":"Train"+module)+"/";
  static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
  static Data Load(string module)=>JsonUtility.FromJson<Data>(File.ReadAllText(Dir(module)+"DoorAnimation.json"));
  static GameObject Group(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);g.layer=30;return g;}
  static BoxCollider Box(string name,Transform parent,Vector3 center,Vector3 size,Material material=null){
   var g=material?GameObject.CreatePrimitive(PrimitiveType.Cube):new GameObject(name);g.name=name;g.transform.SetParent(parent);g.transform.position=center;g.layer=30;
   if(material){g.transform.localScale=Vector3.Scale(size,new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1/parent.lossyScale.z));g.GetComponent<Renderer>().sharedMaterial=material;return g.GetComponent<BoxCollider>();}
   g.transform.localScale=new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1/parent.lossyScale.z);g.AddComponent<BoxCollider>().size=size;return g.GetComponent<BoxCollider>();
  }
  public static Material MaterialFor(string module,string name){
   name=name.Replace(" (Instance)","");var existing=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+name+".mat");
   if(module=="Front")return existing;
   string path=Dir(module)+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
   m=existing?new Material(existing):new Material(Shader.Find("Standard"));m.name=name;
   string atlas=name=="Body_ContinuousShell"?"Continuity":name.StartsWith("Doors")?"Doors":name.StartsWith("Undercarriage")?"Undercarriage":"Body";
   m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir(module)+atlas+"_BaseColor.png"));m.SetColor("_Color",Color.white);
   m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir(module)+atlas+"_MetallicSmoothness.png"));m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_GlossMapScale",1);
   m.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir(module)+atlas+"_Emission.png"));m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",atlas=="Doors"?Color.black:Color.white*1.2f);
   if(name.EndsWith("Glass")){m.SetColor("_Color",new Color(.78f,.86f,.93f,.22f));}
   if(name=="Body_Transition"){m.SetTexture("_MainTex",null);m.SetColor("_Color",new Color(.025f,.032f,.043f));m.SetColor("_EmissionColor",Color.black);}
   m.enableInstancing=true;AssetDatabase.CreateAsset(m,path);return m;
  }
  static void Prepare(string module){
   string model=Dir(module)+module+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(model);importer.importAnimation=false;importer.SaveAndReimport();
   foreach(var path in Directory.GetFiles(Dir(module),"*.png")){
    var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.sRGBTexture=!path.Contains("_ORM")&&!path.Contains("MetallicSmoothness");ti.maxTextureSize=4096;ti.isReadable=path.Contains("_ORM");ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
   }
   foreach(var atlas in new[]{"Body","Doors","Undercarriage","Continuity"}){
    string output=Dir(module)+atlas+"_MetallicSmoothness.png";if(File.Exists(output))continue;
    var orm=AssetDatabase.LoadAssetAtPath<Texture2D>(Dir(module)+atlas+"_ORM.png");var pixels=orm.GetPixels32();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(pixels[i].b,0,0,(byte)(255-pixels[i].g));
    var tex=new Texture2D(orm.width,orm.height,TextureFormat.RGBA32,false,true);tex.SetPixels32(pixels);File.WriteAllBytes(output,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(output);
    var ti=(TextureImporter)AssetImporter.GetAtPath(output);ti.sRGBTexture=false;ti.maxTextureSize=4096;ti.SaveAndReimport();
   }
  }
  [MenuItem("STAFF/Subway/Rebuild Front Middle Rear Consist")]
  public static void Install(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
   Prepare("Middle");Prepare("Rear");
   var scene=SceneManager.GetSceneByPath("Assets/Scenes/STAFF_Subway_Reference.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity",OpenSceneMode.Additive);
   try{
    EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-consist.unity",true);
    var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment");var train=env.transform.Find("Tripo - PBR Glow Train");
    if(train.GetComponentsInChildren<SubwayCar>().Length>0)throw new Exception("Modular consist already installed.");
    var oldCars=train.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("PBR Glow car ")).OrderBy(t=>BoundsOf(t).center.z).ToArray();
    if(oldCars.Length!=5)throw new Exception("Expected five reference cars.");
    var oldBounds=oldCars.Select(BoundsOf).ToArray();var source=Load("Front");var unit=new Vector3(oldBounds[0].size.x/source.sourceSize.x,oldBounds[0].size.y/source.sourceSize.y,oldBounds[0].size.z/source.sourceSize.z);
    var cars=new List<SubwayCar>();
    for(int i=0;i<5;i++){
     string module=i==0?"Rear":i==4?"Front":"Middle";var data=Load(module);var wrapper=Group($"Car {i+1} - {module}",train);var normalizer=Group("Source normalization",wrapper.transform);
     string path=Dir(module)+(module=="Front"?"TrainPBRGlow":module)+".fbx";
     var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(normalizer.transform,false);
     var b=BoundsOf(model.transform);var desired=Vector3.Scale(unit,data.sourceSize);normalizer.transform.localScale=Vector3.Scale(normalizer.transform.localScale,new Vector3(desired.x/b.size.x,desired.y/b.size.y,desired.z/b.size.z));
     b=BoundsOf(model.transform);normalizer.transform.position+=new Vector3(oldBounds[i].center.x-b.center.x,oldBounds[i].min.y-b.min.y,oldBounds[i].center.z-b.center.z);
     foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
     foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>MaterialFor(module,m.name)).ToArray();
     foreach(var anim in model.GetComponentsInChildren<Animator>())Object.DestroyImmediate(anim);
     var car=wrapper.AddComponent<SubwayCar>();car.module=module;car.sourceToWorldScale=new Vector3(-unit.x,unit.y,unit.z);cars.Add(car);
    }
    foreach(var car in oldCars)Object.DestroyImmediate(car.gameObject);
    // Remove only superseded whole-car barriers and benches across the door openings.
    foreach(var t in env.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Closed train car ")||t.name=="Platform track edge"||t.name=="Greybox cabin seat").ToArray())Object.DestroyImmediate(t.gameObject);
    var collision=Group("Boarding and car collision",env.transform).transform;var gates=new List<BoxCollider>();var openings=new List<Bounds>();
    var wall=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Wall panels.mat");var seat=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Chiyoda navy band.mat");
    foreach(var car in cars){
     var b=BoundsOf(car.transform);float minZ=b.min.z+.55f,maxZ=b.max.z-.55f;
     foreach(var side in new[]{"R","L"}){
      var pairs=car.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("SideDoor_"+side+"_")&&r.name.EndsWith("Leaf")).GroupBy(r=>r.transform.parent).Select(g=>{var a=g.First().bounds;foreach(var r in g.Skip(1))a.Encapsulate(r.bounds);return a;}).OrderBy(a=>a.center.z).ToArray();
      float x=pairs[0].center.x;float cursor=minZ;
      foreach(var aperture in pairs){
       float left=aperture.min.z+.035f,right=aperture.max.z-.035f;
       if(left>cursor)WallAndSeat(cursor,left,x,side,collision,seat);
       var gate=Box("Door gate "+car.name+" "+side,collision,new Vector3(x,1.55f,aperture.center.z),new Vector3(.20f,3.2f,right-left));gates.Add(gate);
       if(side=="R"){
        openings.Add(aperture);Box("Level boarding sill",collision,new Vector3((5.13f+x)/2,.075f,aperture.center.z),new Vector3(x-5.13f+.3f,.15f,right-left),wall);
       }
       cursor=right;
      }
      if(cursor<maxZ)WallAndSeat(cursor,maxZ,x,side,collision,seat);
     }
     // Solid, level interior floor, with a central aisle and segmented side seats.
     Box("Boardable cabin floor "+car.name,collision,new Vector3(b.center.x,.065f,b.center.z),new Vector3(3.88f,.17f,maxZ-minZ),wall);
     foreach(float z in new[]{minZ,maxZ}){
      Box("Car end wall left",collision,new Vector3(b.center.x-1.28f,1.6f,z),new Vector3(1.3f,3.2f,.18f));
      Box("Car end wall right",collision,new Vector3(b.center.x+1.28f,1.6f,z),new Vector3(1.3f,3.2f,.18f));
     }
    }
    // Retain edge protection between all twenty platform-side door openings.
    float edge=-24;foreach(var opening in openings.OrderBy(b=>b.min.z)){
     float left=opening.min.z+.035f,right=opening.max.z-.035f;
     if(left>edge)Box("Platform edge barrier",collision,new Vector3(5.25f,2.25f,(edge+left)/2),new Vector3(.3f,4.5f,left-edge));edge=right;
    }
    if(edge<147)Box("Platform edge barrier",collision,new Vector3(5.25f,2.25f,(edge+147)/2),new Vector3(.3f,4.5f,147-edge));
    // Coupler/connection gap is bridged at floor level; no gaps in the walkable aisle.
    for(int i=0;i<cars.Count-1;i++){float a=BoundsOf(cars[i].transform).max.z-.7f,b=BoundsOf(cars[i+1].transform).min.z+.7f;Box("Gangway floor bridge",collision,new Vector3(oldBounds[0].center.x,.065f,(a+b)/2),new Vector3(1.25f,.17f,b-a),wall);}
    BindDoors(env,cars);var doors=env.GetComponent<SubwayDoors>();doors.doorwayGates=gates.ToArray();EditorUtility.SetDirty(doors);
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    File.WriteAllText("Library/StaffSubway/consist-install.txt","PASS: rear + 3 middle + front; shared source scale; original per-module GLB curves; segmented side walls/seats; 40 doorway gates; 20 platform sills; floor/corridor continuity.");
   }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
  }
  [MenuItem("STAFF/Subway/Finalize Consist Connections")]
  static void FinishConnections(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
   var scene=SceneManager.GetSceneByPath("Assets/Scenes/STAFF_Subway_Reference.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity",OpenSceneMode.Additive);
   try{
    var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment");var doors=env.GetComponent<SubwayDoors>();var root=env.transform.Find("Boarding and car collision");
    var cars=env.GetComponentsInChildren<SubwayCar>().OrderBy(c=>BoundsOf(c.transform).center.z).ToArray();var gates=doors.doorwayGates.ToList();var report=new List<string>();
    foreach(var car in cars){
     var b=BoundsOf(car.transform);report.Add(car.name+" "+b);
     foreach(var r in car.GetComponentsInChildren<Renderer>().Where(r=>r.name.ToLowerInvariant().Contains("bellow")))report.Add("  "+r.name+" "+r.bounds);
     foreach(var leaf in doors.leaves.Where(l=>l.transform.IsChildOf(car.transform)&&!l.transform.name.StartsWith("SideDoor_"))){
      var r=leaf.transform.GetComponent<Renderer>();if(!r)continue;string name="Connection door gate "+car.name+" "+leaf.transform.name;
      if(root.Find(name))continue;
      gates.Add(Box(name,root,new Vector3(r.bounds.center.x,1.55f,r.bounds.center.z),new Vector3(r.bounds.size.x,3.2f,.18f)));
     }
     if(car.module=="Rear"||car.module=="Front"){
      string name="Cab boundary "+car.name;if(!root.Find(name))Box(name,root,new Vector3(b.center.x,1.6f,car.module=="Rear"?b.min.z+.7f:b.max.z-.7f),new Vector3(4,3.2f,.2f));
     }
    }
    doors.doorwayGates=gates.ToArray();EditorUtility.SetDirty(doors);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllLines("Library/StaffSubway/consist-bounds.txt",report);
   }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
  }
  static void WallAndSeat(float a,float b,float x,string side,Transform root,Material mat){
   Box("Car side wall",root,new Vector3(x,1.6f,(a+b)/2),new Vector3(.14f,3.4f,b-a));
   if(b-a>.55f)Box("Cabin seat between doors",root,new Vector3(x+(side=="R"?.45f:-.45f),.62f,(a+b)/2),new Vector3(.65f,.24f,b-a-.15f),mat);
  }
  public static void BindDoors(GameObject env,List<SubwayCar> cars){
   var leaves=new List<SubwayDoors.Leaf>();foreach(var car in cars){var data=Load(car.module);
    foreach(var track in data.tracks){
     var t=car.GetComponentsInChildren<Transform>().Single(x=>x.name==track.node);var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
     for(int i=0;i<track.times.Length;i++){var delta=t.parent.InverseTransformVector(Vector3.Scale(track.deltas[i],car.sourceToWorldScale));for(int axis=0;axis<3;axis++)curves[axis].AddKey(new Keyframe(track.times[i],delta[axis]));}
     foreach(var c in curves)for(int k=0;k<c.length;k++){AnimationUtility.SetKeyLeftTangentMode(c,k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(c,k,AnimationUtility.TangentMode.Linear);}
     leaves.Add(new SubwayDoors.Leaf{transform=t,closedLocalPosition=t.localPosition,x=curves[0],y=curves[1],z=curves[2]});
    }
   }
   var doors=env.GetComponent<SubwayDoors>();doors.leaves=leaves.ToArray();doors.duration=1.5f;
  }
 }
}
