using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Staff.Characters;using Staff.Shared;
namespace Staff.Traversal.Editor {
public static class TangentSetup {
 static void RoofBox(Transform parent,string name,Vector3 position,Vector3 size){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.AddComponent<BoxCollider>().size=size;}
 public const string Gameplay="Assets/StaffShared/Prefabs/STAFF Gameplay.prefab";
 public const string City="Assets/Scenes/STAFF_City_Reference.unity";
 [MenuItem("Tools/STAFF/Install Tangent Traversal")]
 public static void Run(){
  var root=PrefabUtility.LoadPrefabContents(Gameplay);try{var p=root.GetComponentInChildren<AdventurePlayer>(true);var a=p.GetComponent<TangentTraversal>()??p.gameObject.AddComponent<TangentTraversal>();a.castClip=TangentAudition.Clip("128670910");a.launchClip=TangentAudition.Clip("119840901");a.flightClip=TangentAudition.Clip("119830901");a.wallClip=TangentAudition.Clip("120480901");a.landClip=TangentAudition.Clip("120470901");a.equationTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/StaffTraversal/Mathematics/Tangent/symbol.png");if(!a.equationTexture)throw new Exception("Missing LaTeX texture");PrefabUtility.SaveAsPrefabAsset(root,Gameplay);}finally{PrefabUtility.UnloadPrefabContents(root);}
  var scene=EditorSceneManager.OpenScene(City);int count=0;foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Main plaster mass")){var building=t.parent;if(!building.GetComponent<TangentSurface>())building.gameObject.AddComponent<TangentSurface>();foreach(var child in building.GetComponentsInChildren<Transform>(true).Where(c=>c.name=="Roof slab"||c.name=="Roof parapet")){if(!child.GetComponent<Collider>())child.gameObject.AddComponent<BoxCollider>();}
   if(!building.Find("Tangent Roof Collision")){
    var dims=t.localScale;float h=t.localPosition.y+dims.y*.5f;
    RoofBox(building,"Tangent Roof Collision",new Vector3(0,h+.13f,0),new Vector3(dims.x+.38f,.22f,dims.z+.36f));
    foreach(float side in new[]{-1f,1f}){RoofBox(building,"Tangent Parapet Collision",new Vector3(side*(dims.x/2-.1f),h+.46f,0),new Vector3(.2f,.7f,dims.z));RoofBox(building,"Tangent Parapet Collision",new Vector3(0,h+.46f,side*(dims.z/2-.1f)),new Vector3(dims.x,.7f,.2f));}
   }
   foreach(var mesh in building.GetComponentsInChildren<MeshFilter>(true))if(mesh.sharedMesh&&!mesh.GetComponent<Collider>())mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
   count++;}EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Directory.CreateDirectory("Library/TangentReview");File.WriteAllText("Library/TangentReview/setup.txt","Buildings marked: "+count+"\nShared gameplay prefab configured\n");
 }
}
}
