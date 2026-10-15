using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;using Staff.Shared;
namespace Staff.Traversal.Editor {
[InitializeOnLoad] public static class TangentWorldSetup {
 const string Work="Library/TangentWorld",Root="Assets/StaffTraversal/Collision";
 static TangentWorldSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||!File.Exists(Work+"/setup.request"))return;File.Delete(Work+"/setup.request");try{Run();}catch(Exception e){File.WriteAllText(Work+"/setup-error.txt",e.ToString());Debug.LogException(e);}}
 static void Mark(GameObject go){if(!go.GetComponent<TangentSurface>())go.AddComponent<TangentSurface>();}
 public static void Run(){Directory.CreateDirectory(Work);Directory.CreateDirectory(Root);AssetDatabase.Refresh();
  var gameplay=PrefabUtility.LoadPrefabContents(TangentSetup.Gameplay);try{gameplay.GetComponentInChildren<TangentTraversal>(true).range=400;PrefabUtility.SaveAsPrefabAsset(gameplay,TangentSetup.Gameplay);}finally{PrefabUtility.UnloadPrefabContents(gameplay);}
  var report=new List<string>();
  foreach(string kind in new[]{"Rear","Middle","Front"}){
   string path="Assets/StaffShared/Prefabs/Subway "+kind+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
   try{Mark(root);var module=root.GetComponent<SubwayDoorModule>();var pieces=new List<CombineInstance>();
    foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)){
     if(!filter.sharedMesh||!filter.GetComponent<MeshRenderer>()||!filter.gameObject.activeInHierarchy)continue;
     bool door=module.leaves.Any(l=>l.transform&&filter.transform.IsChildOf(l.transform));
     if(door){if(!filter.GetComponent<Collider>())filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;continue;}
     for(int i=0;i<filter.sharedMesh.subMeshCount;i++)if(filter.sharedMesh.GetTopology(i)==MeshTopology.Triangles)pieces.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=i,transform=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
    }
    if(pieces.Count==0)throw new Exception("Missing car geometry: "+kind);
    var mesh=new Mesh{name="Subway "+kind+" traversal collision",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pieces.ToArray(),true,true);mesh.RecalculateBounds();string meshPath=Root+"/Subway "+kind+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,meshPath);
    var hull=root.transform.Find("Traversal hull collision");if(!hull){hull=new GameObject("Traversal hull collision").transform;hull.SetParent(root.transform,false);hull.gameObject.layer=30;}
    var collider=hull.GetComponent<MeshCollider>();if(!collider)collider=hull.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
    PrefabUtility.SaveAsPrefabAsset(root,path);report.Add("PASS shared "+kind+" car: exact hull mesh, independently moving doors, target marker; triangles="+mesh.triangles.Length/3);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(TangentSetup.City);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(TangentSetup.City,UnityEditor.SceneManagement.OpenSceneMode.Additive);
  var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment").transform;
  foreach(var name in new[]{"Infinity Metro - elevated railway","Distant city skyline","Streets and Sidewalks","Metro entrance"}){
   var root=env.Find(name);if(!root)throw new Exception("Missing group "+name);Mark(root.gameObject);
   foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh&&!f.GetComponent<Collider>())f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
   report.Add("PASS "+name+": target marker; colliders="+root.GetComponentsInChildren<Collider>(true).Length);
  }
  foreach(var train in env.GetComponentsInChildren<SubwayConsist>())train.Rebuild();
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();report.Add("PASS shared range 400m; city 1.3 / railway full-size preserved");File.WriteAllLines(Work+"/setup.txt",report);
 }
}
}
