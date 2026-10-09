using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Staff.Subway.Editor {
 public static class SubwayAlignmentReview {
  [Serializable] public class Change {public string id,name;public Vector3 before,after,scale,size,newScale,newSize;public bool collider;}
  [Serializable] public class Plan {public Change[] changes;public float[] gaps;}
  static Bounds B(SubwayCar c){var rs=c.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
  [MenuItem("STAFF/Subway/Apply Reviewed Gangway Alignment")]
  static void Apply(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
   var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Library/StaffSubway/alignment-review.json"));
   var targets=plan.changes.Select(c=>{if(!GlobalObjectId.TryParse(c.id,out var id))throw new Exception("Invalid target ID");return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Transform;}).ToArray();
   for(int i=0;i<targets.Length;i++){var t=targets[i];var c=plan.changes[i];if(!t||t.name!=c.name||t.position!=c.before||t.localScale!=c.scale||(c.collider&&t.GetComponent<BoxCollider>().size!=c.size))throw new Exception("Scene changed since read-only review: "+c.name);}
   var scene=targets[0].gameObject.scene;
   if(scene.path!="Assets/Scenes/STAFF_Subway_Reference.unity")throw new Exception("Unexpected scene");
   // Fresh full-scene backup before any mutation. No objects are deleted or recreated.
   if(!EditorSceneManager.SaveScene(scene,"Library/StaffSubway/before-reviewed-alignment.unity",true))throw new Exception("Backup failed");
   var objects=targets.Cast<UnityEngine.Object>().Concat(targets.Select(t=>t.GetComponent<BoxCollider>()).Where(c=>c).Cast<UnityEngine.Object>()).ToArray();Undo.RecordObjects(objects,"Align reviewed modular train gangways");
   try{
    for(int i=0;i<targets.Length;i++){var t=targets[i];var c=plan.changes[i];t.position=c.after;t.localScale=c.newScale;if(c.collider)t.GetComponent<BoxCollider>().size=c.newSize;}
    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
   }catch{
    for(int i=0;i<targets.Length;i++){var t=targets[i];var c=plan.changes[i];t.position=c.before;t.localScale=c.scale;if(c.collider)t.GetComponent<BoxCollider>().size=c.size;}throw;
   }
   File.WriteAllText("Library/StaffSubway/alignment-applied.txt","PASS: applied validated plan to 245 existing generated targets, no deletion; fresh full scene backup saved; original car/door/collider references retained. Four gangway seams now 0.025m.");
  }
  [MenuItem("STAFF/Subway/Stop Play For Scene Edit")]
  static void Stop(){EditorApplication.isPlaying=false;}
  [MenuItem("STAFF/Subway/Review Gangway Alignment")]
  static void Review(){
   if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
   var scene=SceneManager.GetSceneByPath("Assets/Scenes/STAFF_Subway_Reference.unity");if(!scene.isLoaded)throw new Exception("Open subway scene first");
   var env=scene.GetRootGameObjects().Single(g=>g.name=="Environment");var root=env.transform.Find("Boarding and car collision");var train=env.transform.Find("Tripo - PBR Glow Train");
   var cars=env.GetComponentsInChildren<SubwayCar>().OrderBy(c=>B(c).center.z).ToArray();if(cars.Length!=5)throw new Exception("Expected installed modular consist");var bounds=cars.Select(B).ToArray();var offsets=new float[5];var gaps=new float[4];
   for(int i=1;i<5;i++){gaps[i-1]=bounds[i].min.z-bounds[i-1].max.z;offsets[i]=offsets[i-1]+.025f-gaps[i-1];}
   int Nearest(float z){return Enumerable.Range(0,5).OrderBy(i=>Mathf.Abs(z-bounds[i].center.z)).First();}
   var changes=new List<Change>();
   Change Add(Transform t,Vector3 pos){var bc=t.GetComponent<BoxCollider>();var e=new Change{id=GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(),name=t.name,before=t.position,after=pos,scale=t.localScale,newScale=t.localScale,collider=bc,size=bc?bc.size:Vector3.zero,newSize=bc?bc.size:Vector3.zero};changes.Add(e);return e;}
   for(int i=0;i<5;i++)Add(cars[i].transform,cars[i].transform.position+Vector3.forward*offsets[i]);
   foreach(Transform t in train)if(!t.GetComponent<SubwayCar>()){
    if(t.name!="Greybox cabin floor"&&t.name!="Greybox cabin ceiling"&&t.name!="Cabin linear light")throw new Exception("Unexpected interior: "+t.name);
    Add(t,t.position+Vector3.forward*offsets[Nearest(t.position.z)]);
   }
   var allowed=new[]{"Car side wall","Cabin seat between doors","Level boarding sill","Door gate ","Boardable cabin floor ","Car end wall ","Connection door gate ","Cab boundary "};
   foreach(Transform t in root){if(t.name=="Platform edge barrier"||t.name=="Gangway floor bridge")continue;if(!allowed.Any(s=>t.name.StartsWith(s)))throw new Exception("Unexpected collision object: "+t.name);Add(t,t.position+Vector3.forward*offsets[Nearest(t.position.z)]);}
   var gates=env.GetComponent<SubwayDoors>().doorwayGates.Where(g=>g.name.EndsWith(" R")).OrderBy(g=>g.transform.position.z).ToArray();var barriers=root.Cast<Transform>().Where(t=>t.name=="Platform edge barrier").OrderBy(t=>t.position.z).ToArray();if(barriers.Length!=21||gates.Length!=20)throw new Exception("Unexpected edge segment count");
   float edge=-24;
   for(int i=0;i<21;i++){float end=i==20?147:gates[i].transform.position.z+offsets[Nearest(gates[i].transform.position.z)]-gates[i].size.z*gates[i].transform.lossyScale.z/2;var e=Add(barriers[i],new Vector3(5.25f,2.25f,(edge+end)/2));e.newSize=new Vector3(.3f,4.5f,end-edge);if(end<=edge)throw new Exception("Negative edge segment");if(i<20)edge=end+gates[i].size.z*gates[i].transform.lossyScale.z;}
   var bridges=root.Cast<Transform>().Where(t=>t.name=="Gangway floor bridge").OrderBy(t=>t.position.z).ToArray();if(bridges.Length!=4)throw new Exception("Unexpected bridge count");
   for(int i=0;i<4;i++){float a=bounds[i].max.z+offsets[i]-.7f,b=bounds[i+1].min.z+offsets[i+1]+.7f;var e=Add(bridges[i],new Vector3(7.62f,.065f,(a+b)/2));e.newScale=Vector3.Scale(new Vector3(1.25f,.17f,b-a),new Vector3(1/root.lossyScale.x,1/root.lossyScale.y,1/root.lossyScale.z));}
   if(changes.Select(c=>c.id).Distinct().Count()!=changes.Count)throw new Exception("Duplicate targets");
   File.WriteAllText("Library/StaffSubway/alignment-review.json",JsonUtility.ToJson(new Plan{changes=changes.ToArray(),gaps=gaps},true));
   File.WriteAllText("Library/StaffSubway/alignment-review.txt",$"READ ONLY: {changes.Count} existing generated targets. No deletion or creation. Four gaps: {string.Join(", ",gaps)}. Target gap 0.025m. Character/camera/light roots excluded. All interior/collision target names and edge/bridge counts validated.");
  }
 }
}
