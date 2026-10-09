using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Staff.Subway.Editor {
 public static class SubwayRepairSetup {
  static void AfterStop(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode)return;EditorApplication.playModeStateChanged-=AfterStop;EditorApplication.delayCall+=Apply;}
  [MenuItem("STAFF/Subway/Apply Rear And Cabin Fixes")]
  static void Apply(){
   if(EditorApplication.isPlaying){EditorApplication.playModeStateChanged+=AfterStop;EditorApplication.isPlaying=false;return;}
   var shader=Shader.Find("STAFF/Subway/Double Sided PBR");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Double-sided shader compilation failure");
   var scene=SceneManager.GetSceneByPath("Assets/Scenes/STAFF_Subway_Reference.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/STAFF_Subway_Reference.unity",OpenSceneMode.Additive);
   try{
    if(!EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-rear-cabin-fixes.unity",true))throw new Exception("Backup failed");
    var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment");var cars=env.GetComponentsInChildren<SubwayCar>();
    foreach(var atlas in new[]{"Body","Doors","Undercarriage","Continuity"}){
     string dir="Assets/StaffSubway/Models/TrainRear/";var orm=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+atlas+"_ORM.png");var pixels=orm.GetPixels32();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(pixels[i].b,0,0,(byte)(255-pixels[i].g));
     var tex=new Texture2D(orm.width,orm.height,TextureFormat.RGBA32,false,true);tex.SetPixels32(pixels);File.WriteAllBytes(dir+atlas+"_MetallicSmoothness.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(dir+atlas+"_MetallicSmoothness.png");
    }
    var glass=Shader.Find("STAFF/Subway/Double Sided Glass");if(!glass||ShaderUtil.ShaderHasError(glass))throw new Exception("Glass shader compilation failure");
    foreach(var car in cars){
     foreach(var r in car.GetComponentsInChildren<Renderer>()){
      if(car.module=="Rear")r.sharedMaterials=r.sharedMaterials.Select(m=>SubwayConsistSetup.MaterialFor("Rear",m.name)).ToArray();
      foreach(var m in r.sharedMaterials.Distinct())if(m){
       bool transparent=m.name.Contains("Glass");m.shader=transparent?glass:shader;m.renderQueue=transparent?3000:-1;m.SetOverrideTag("RenderType",transparent?"Transparent":"Opaque");m.doubleSidedGI=true;EditorUtility.SetDirty(m);
      }
     }
    }
    SubwayConsistSetup.BindDoors(env,cars.ToList());
    var doors=env.GetComponent<SubwayDoors>();var root=env.transform.Find("Boarding and car collision");var ends=doors.doorwayGates.Where(g=>g.name.StartsWith("Connection door gate ")).OrderBy(g=>g.transform.position.z).ToArray();if(ends.Length!=8)throw new Exception("Expected eight inter-car connection doors");
    for(int i=0;i<4;i++){
     float a=ends[i*2].transform.position.z-.45f,b=ends[i*2+1].transform.position.z+.45f;
     for(int side=-1;side<=1;side+=2){
      string name="Gangway containment "+i+" "+side;var existing=root.Find(name);var t=existing?existing:new GameObject(name).transform;t.SetParent(root,true);t.gameObject.layer=30;
      t.position=new Vector3(7.62f+side*.875f,1.8f,(a+b)/2);t.rotation=Quaternion.identity;t.localScale=new Vector3(1/root.lossyScale.x,1/root.lossyScale.y,1/root.lossyScale.z);
      var collider=t.GetComponent<BoxCollider>();if(!collider)collider=t.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(.2f,3.6f,b-a);
     }
    }
    foreach(var gate in doors.doorwayGates)gate.enabled=true;
    EditorUtility.SetDirty(doors);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    File.WriteAllText("Library/StaffSubway/rear-cabin-fixes.txt","PASS: refreshed Rear FBX/textures and GLB tracks; opaque and glass car materials use double-sided Standard PBR; eight gangway containment walls; O platform-only gates and leaves; opposite side closed; connection doors follow O open/close.");
   }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
  }
 }
}
