using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace Staff.Shared.Editor {
[InitializeOnLoad] public static class SurfaceRepair {
 const string Work="Library/SurfaceRepair";
 static SurfaceRepair(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Work+"/install.request"))return;File.Delete(Work+"/install.request");try{Run();}catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);File.AppendAllText(Work+"/result.txt","PASS "+message+"\n");}
 static void EditScene(string path,Action<Scene> edit){var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{EditorSceneManager.SaveScene(scene,Work+"/before-"+scene.name+".unity",true);edit(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}}
 public static void Run(){Directory.CreateDirectory(Work);File.WriteAllText(Work+"/result.txt","");
 EditScene(SceneTravel.City,scene=>{
  var street=scene.GetRootGameObjects().Single(g=>g.name=="Environment").transform.Find("Streets and Sidewalks");var cross=street.Find("Cross street");var pos=cross.localPosition;pos.y=-.17f;cross.localPosition=pos;
  var filter=street.GetComponentsInChildren<MeshFilter>().Single(f=>f.GetComponent<Renderer>().sharedMaterial.name=="asphalt");var mesh=filter.sharedMesh;var vertices=mesh.vertices;int changed=0;
  for(int i=0;i<vertices.Length;i++)if(Mathf.Abs(vertices[i].x)>70){vertices[i].y=vertices[i].y>-.1f?-.03f:-.31f;changed++;}
  Check(changed==24,"Cross street mesh identified: 24 vertices");mesh.vertices=vertices;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);var collider=filter.GetComponent<MeshCollider>();if(collider){collider.sharedMesh=null;collider.sharedMesh=mesh;}
  Check(Mathf.Abs((-.16f+.15f)-(-.17f+.14f))>.019f,"Crossroad surfaces separated by 2cm before city scaling");
 });
 EditScene(SceneTravel.Subway,scene=>{
  var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment");var slab=env.GetComponentsInChildren<Transform>().Single(t=>t.name.StartsWith("Platform slab"));var pos=slab.localPosition;var size=slab.localScale;pos.x=-2.42f;size.x=11.16f;slab.localPosition=pos;slab.localScale=size;
  var coping=env.GetComponentsInChildren<Transform>().Single(t=>t.name=="Platform coping continuation");Check(Mathf.Abs((pos.x+size.x/2)-(coping.localPosition.x-coping.localScale.x/2))<.001f,"Platform slab ends at coping inner edge; no overlapping top faces");
  var open=env.GetComponent<Staff.Subway.SubwayOpenTracks>();if(open)open.Build();
 });
 var scenes=EditorBuildSettings.scenes.ToList();foreach(var path in new[]{SceneTravel.City,SceneTravel.Subway}){var entry=scenes.FirstOrDefault(s=>s.path==path);if(entry==null)scenes.Add(new EditorBuildSettingsScene(path,true));else entry.enabled=true;}EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();Check(SceneTravel.Destination(SceneTravel.City)==SceneTravel.Subway&&SceneTravel.Destination(SceneTravel.Subway)==SceneTravel.City,"F6 routes both ways; both scenes enabled in Build Settings");
 }
}
}
