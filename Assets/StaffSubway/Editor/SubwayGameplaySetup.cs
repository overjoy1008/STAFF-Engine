using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Staff.Characters;
namespace Staff.Subway.Editor {
 public static class SubwayGameplaySetup {
  [MenuItem("STAFF/Subway/Import SampleScene Gameplay")]
  public static void Install() {
   var scene=SceneManager.GetActiveScene();
   if(scene.name!="STAFF_Subway_Reference" || EditorApplication.isPlaying) throw new Exception("Open the subway scene in Edit mode.");
   if(scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<AdventurePlayer>(true)).Any()) throw new Exception("Gameplay already exists; refusing to duplicate it.");
   Directory.CreateDirectory("Library/StaffSubway");
   EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-gameplay.unity",true);
   var source=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Additive);
   try {
    SceneManager.SetActiveScene(scene);
    var original=source.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<AdventurePlayer>(true)).Single();
    var originalCamera=source.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<AdventureCamera>(true)).Single();
    var characters=UnityEngine.Object.Instantiate(original.transform.root.gameObject);
    characters.name="Characters";SceneManager.MoveGameObjectToScene(characters,scene);
    var player=characters.GetComponentInChildren<AdventurePlayer>(true);
    player.transform.position=new Vector3(0,.03f,-4);player.transform.rotation=Quaternion.identity;
    var cameras=scene.GetRootGameObjects().Single(x=>x.name=="Cameras");
    var reference=cameras.GetComponentInChildren<Camera>();
    var oldReflection=reference.GetComponent<SubwayPlanarReflection>();
    reference.enabled=false;reference.tag="Untagged";
    foreach(var b in reference.GetComponents<Behaviour>()) b.enabled=false;
    var cameraObject=UnityEngine.Object.Instantiate(originalCamera.gameObject,cameras.transform);
    cameraObject.name="Gameplay Camera (SampleScene)";
    var camera=cameraObject.GetComponent<Camera>();camera.cullingMask=-1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=reference.backgroundColor;
    var follow=camera.GetComponent<AdventureCamera>();
    var so=new SerializedObject(player);so.FindProperty("followCamera").objectReferenceValue=follow;so.ApplyModifiedPropertiesWithoutUndo();
    follow.Configure(player.transform,player.Visual);
    if(oldReflection){var reflection=cameraObject.AddComponent<SubwayPlanarReflection>();EditorUtility.CopySerialized(oldReflection,reflection);reflection.enabled=true;}
    foreach(var light in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Light>(true))) light.cullingMask|=1;
    var env=scene.GetRootGameObjects().Single(x=>x.name=="Environment");
    var collision=new GameObject("Gameplay Collision");collision.transform.SetParent(env.transform,false);collision.layer=30;
    Action<string,Vector3,Vector3> box=(name,center,size)=>{var o=new GameObject(name);o.layer=30;o.transform.SetParent(collision.transform,false);o.transform.position=center;o.AddComponent<BoxCollider>().size=size;};
    foreach(var t in env.GetComponentsInChildren<Transform>()) {
     if(!(t.name.StartsWith("Column ")||t.name=="Black bench"||t.name=="Secondary concourse bench"))continue;
     var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;
     var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);
     box(t.name+" collider",bounds.center,bounds.size);
    }
    // Closed train cars are solid until interactive doors/interiors are introduced.
    for(int i=0;i<5;i++) box("Closed train car "+(i+1),new Vector3(5.08f,.865f,-2+i*20.55f),new Vector3(3.1f,3.85f,20.2f));
    box("Platform track edge",new Vector3(3.5f,1.5f,41),new Vector3(.2f,3,114));
    box("Near platform limit",new Vector3(-2.3f,1.5f,-16),new Vector3(11.4f,3,.15f));
    box("Far platform limit",new Vector3(-2.3f,1.5f,98),new Vector3(11.4f,3,.15f));
   } finally {EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene);}
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   File.WriteAllText("Library/StaffSubway/gameplay-install.txt","PASS: SampleScene Characters and AdventureCamera copied with input, animator, roster and character shaders intact; player-camera references remapped; collision volumes added. Source scene not saved.");
   Debug.Log("Subway gameplay installed.");
  }
 }
}
